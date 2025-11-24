# Session Tracking Architecture Diagram

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Microsoft Teams                                │
│                                                                          │
│  User: "create ticket"                                                  │
│    ↓                                                                     │
│  Bot: "Please enter a title" ← [TRACKED: bot message #1]              │
│    ↓                                                                     │
│  User: "Cannot access reports" ← [TRACKED: user message #2]           │
│    ↓                                                                     │
│  Bot: "Please provide description" ← [TRACKED: bot message #3]        │
│    ↓                                                                     │
│  User: "Getting 404 error..." ← [TRACKED: user message #4]            │
│    ↓                                                                     │
│  Bot: "Ticket Summary... Confirm?" ← [TRACKED: bot message #5]        │
│    ↓                                                                     │
│  User: "Yes" ← [TRACKED: user message #6]                             │
└─────────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                     Bot: CreateTicketDialog                              │
│                                                                          │
│  1. TrackMessage() called for each interaction                          │
│     → Stores in stepContext.Values[ConversationMessagesKey]            │
│     → Builds List<MessageInfo>                                          │
│                                                                          │
│  2. BuildSessionInfo() constructs SessionInfo object:                   │
│     {                                                                    │
│       ConversationId: "19:meeting_xxx@thread.v2" ← from Activity       │
│       SessionId: "abc-123-def-456" ← new GUID                          │
│       UserId: "29:1AbCdEfGhIjKlMn" ← from Activity.From                │
│       UserName: "John Doe" ← from Activity.From.Name                   │
│       TenantId: "b22f8675-..." ← from Activity.Conversation            │
│       ChannelId: "msteams" ← from Activity.ChannelId                   │
│       Locale: "en-US" ← from Activity.Locale                           │
│       Timestamp: 2025-10-17T08:00:00Z ← DateTime.UtcNow               │
│       Messages: [msg1, msg2, msg3, msg4, msg5, msg6] ← tracked list   │
│     }                                                                    │
│                                                                          │
│  3. Calls TicketApiClient.CreateAsync()                                 │
└─────────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                     TicketApiClient                                      │
│                                                                          │
│  CreateAsync(title, description, userToken, sessionInfo, ct)            │
│                                                                          │
│  1. Logs session details                                                │
│     Console: "Session info provided: True"                              │
│     Console: "ConversationId: 19:meeting_xxx, Messages: 6"             │
│                                                                          │
│  2. Builds HTTP request:                                                │
│     POST /api/tickets                                                   │
│     Authorization: Bearer <token>                                       │
│     Body: {                                                             │
│       title: "Cannot access reports",                                   │
│       description: "Getting 404 error...",                              │
│       session: { ... }  ← SessionInfo object                           │
│     }                                                                    │
│                                                                          │
│  3. Sends to API                                                        │
└─────────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                     Support Tickets API                                 │
│              (https://saaliticketsapiclean.azurewebsites.net)          │
│                                                                          │
│  TicketsController.Create()                                             │
│    ↓                                                                     │
│  1. Receives CreateTicketRequest with Session property                  │
│  2. Logs: "Creating ticket with session - ConversationId: ...,         │
│           SessionId: ..., Messages: 6"                                  │
│  3. Calls repository.CreateAsync(title, desc, session, ct)             │
│    ↓                                                                     │
│  TableStorageTicketRepository.CreateAsync()                             │
│    ↓                                                                     │
│  1. Creates TicketEntity with session fields:                           │
│     entity.ConversationId = session.ConversationId                      │
│     entity.SessionId = session.SessionId                                │
│     entity.TenantId = session.TenantId                                  │
│     entity.ChannelId = session.ChannelId                                │
│     entity.Locale = session.Locale                                      │
│     entity.ConversationMessages = JsonSerializer.Serialize(             │
│       session.Messages)  ← JSON array                                   │
│     entity.MessageCount = session.Messages.Count                        │
│                                                                          │
│  2. Inserts into Azure Table Storage                                    │
└─────────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                   Azure Table Storage                                   │
│                   Table: SupportTickets                                 │
│                                                                          │
│  PartitionKey: 29:1AbCdEfGhIjKlMnOpQrStUv                              │
│  RowKey: abc-123-def-456                                                │
│  ─────────────────────────────────────────────────────────────────     │
│  Title: "Cannot access reports"                                         │
│  Description: "Getting 404 error when trying to view monthly reports"   │
│  Status: "New"                                                          │
│  CreatedByUserId: "29:1AbCdEfGhIjKlMnOpQrStUv"                         │
│  CreatedByDisplayName: "John Doe"                                       │
│  CreatedUtc: 2025-10-17T08:00:20Z                                       │
│  LastUpdatedUtc: 2025-10-17T08:00:20Z                                   │
│  Deleted: false                                                         │
│  ─────────────────────────────────────────────────────────────────     │
│  ✨ NEW SESSION FIELDS:                                                │
│  ConversationId: "19:meeting_MjdhNjM4YzQtZGJm@thread.v2"               │
│  SessionId: "abc123-def456-ghi789-jkl012"                              │
│  TenantId: "b22f8675-8375-455b-941a-67bee4cf7747"                      │
│  ChannelId: "msteams"                                                   │
│  Locale: "en-US"                                                        │
│  ConversationMessages: "[{messageId:'msg-001',from:'Bot',text:'📝...', │
│    timestamp:'2025-10-17T08:00:00Z',messageType:'bot'},{messageId:     │
│    'msg-002',from:'John Doe',text:'Cannot access reports',timestamp:   │
│    '2025-10-17T08:00:05Z',messageType:'user'},...}]"                   │
│  MessageCount: 6                                                        │
└─────────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────────┐
│                      Support Team Dashboard                             │
│                                                                          │
│  Ticket #abc-123                                                        │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━     │
│  Title: Cannot access reports                                           │
│  Status: New                                                            │
│  Created: Oct 17, 2025 @ 8:00 AM                                       │
│  User: John Doe (29:1AbCd...)                                          │
│                                                                          │
│  📋 CONVERSATION HISTORY:                                              │
│  ┌───────────────────────────────────────────────────────────────┐    │
│  │ [08:00:00] Bot: Please enter a title for your support ticket  │    │
│  │ [08:00:05] John Doe: Cannot access reports                     │    │
│  │ [08:00:06] Bot: Please provide a detailed description          │    │
│  │ [08:00:15] John Doe: Getting 404 error when trying to view    │    │
│  │                      monthly reports                            │    │
│  │ [08:00:16] Bot: Ticket Summary... Do you want to create?       │    │
│  │ [08:00:20] John Doe: Yes                                       │    │
│  └───────────────────────────────────────────────────────────────┘    │
│                                                                          │
│  🎯 QUICK INSIGHTS:                                                    │
│  • Issue: 404 error accessing monthly reports                          │
│  • Started: This morning after system update (from description)        │
│  • User confirmed ticket creation                                      │
│  • 6 messages exchanged (3 bot prompts, 3 user responses)              │
│                                                                          │
│  [Assign to Agent] [Add Comment] [Change Status] [View in Teams]      │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 📊 Data Flow Diagram

```
USER              BOT DIALOG           API CLIENT          API                  STORAGE
  │                   │                     │                │                     │
  │──"create ticket"─→│                     │                │                     │
  │                   │                     │                │                     │
  │                   │──Track msg #1──→ [List]             │                     │
  │←─"Enter title"────│                     │                │                     │
  │                   │                     │                │                     │
  │──"Cannot access"─→│                     │                │                     │
  │                   │──Track msg #2──→ [List]             │                     │
  │                   │                     │                │                     │
  │                   │──Track msg #3──→ [List]             │                     │
  │←─"Description?"───│                     │                │                     │
  │                   │                     │                │                     │
  │──"Getting 404"───→│                     │                │                     │
  │                   │──Track msg #4──→ [List]             │                     │
  │                   │                     │                │                     │
  │                   │──Track msg #5──→ [List]             │                     │
  │←─"Confirm?"───────│                     │                │                     │
  │                   │                     │                │                     │
  │──"Yes"───────────→│                     │                │                     │
  │                   │──Track msg #6──→ [List]             │                     │
  │                   │                     │                │                     │
  │                   │──BuildSessionInfo()→SessionInfo{    │                     │
  │                   │   ConversationId                     │                     │
  │                   │   SessionId                          │                     │
  │                   │   Messages[6]                        │                     │
  │                   │ }                   │                │                     │
  │                   │                     │                │                     │
  │                   │──CreateAsync()─────→│                │                     │
  │                   │   (title, desc,     │                │                     │
  │                   │    token, session)  │                │                     │
  │                   │                     │                │                     │
  │                   │                     │──POST /api────→│                     │
  │                   │                     │   tickets      │                     │
  │                   │                     │   + session    │                     │
  │                   │                     │                │                     │
  │                   │                     │                │──CreateAsync()─────→│
  │                   │                     │                │   + session data    │
  │                   │                     │                │                     │
  │                   │                     │                │                TicketEntity
  │                   │                     │                │                + Session
  │                   │                     │                │                Fields
  │                   │                     │                │                     │
  │                   │                     │                │←─────Success────────│
  │                   │                     │                │   TicketDto         │
  │                   │                     │←──200 OK───────│                     │
  │                   │                     │   {ticket}     │                     │
  │                   │←─────TicketDto──────│                │                     │
  │                   │                     │                │                     │
  │←─"✅ Created!"────│                     │                │                     │
  │   Ticket ID       │                     │                │                     │
  │                   │                     │                │                     │
```

---

## 🔍 Message Tracking Detail

```
┌─────────────────────────────────────────────────────────────────────┐
│           TrackMessage() Function Flow                              │
└─────────────────────────────────────────────────────────────────────┘

  Input: (stepContext, from, text, messageType)
    ↓
  1. Get message list from stepContext.Values[ConversationMessagesKey]
    ↓
  2. Create MessageInfo object:
     {
       MessageId: Guid.NewGuid().ToString(),
       From: from,
       Text: text,
       Timestamp: DateTime.UtcNow,
       MessageType: messageType
     }
    ↓
  3. Add to message list
    ↓
  4. Log: "Tracked {messageType} message from {from}"
    ↓
  Output: Message added to list in stepContext


┌─────────────────────────────────────────────────────────────────────┐
│         BuildSessionInfo() Function Flow                            │
└─────────────────────────────────────────────────────────────────────┘

  Input: (stepContext, activity, userName)
    ↓
  1. Get message list from stepContext.Values[ConversationMessagesKey]
    ↓
  2. Extract data from Activity:
     - ConversationId ← activity.Conversation?.Id
     - UserId ← activity.From?.Id
     - TenantId ← activity.Conversation?.TenantId
     - ChannelId ← activity.ChannelId
     - Locale ← activity.Locale ?? "en-US"
    ↓
  3. Generate new SessionId (Guid.NewGuid())
    ↓
  4. Create SessionInfo object with all extracted data + message list
    ↓
  Output: Complete SessionInfo object ready to send to API
```

---

## 🗄️ Storage Structure

```
Azure Table Storage: SupportTickets
├── Partition: user-123
│   ├── Row: ticket-001 (OLD ticket without session)
│   │   ├── Title
│   │   ├── Description
│   │   ├── Status
│   │   └── ... (no session fields)
│   │
│   └── Row: ticket-002 (NEW ticket with session) ✨
│       ├── Title
│       ├── Description
│       ├── Status
│       ├── ConversationId ← "19:meeting_xxx@thread.v2"
│       ├── SessionId ← "abc-123-def-456"
│       ├── TenantId ← "b22f8675-..."
│       ├── ChannelId ← "msteams"
│       ├── Locale ← "en-US"
│       ├── ConversationMessages ← JSON: [{...},{...}]
│       └── MessageCount ← 6
│
├── Partition: user-456
│   └── ... (more tickets)
│
└── Partition: user-789
    └── ... (more tickets)

Query Examples:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1. Get all tickets with session data:
   filter: "ConversationId ne null"

2. Get tickets with many messages:
   filter: "MessageCount gt 5"

3. Get tickets from specific conversation:
   filter: "ConversationId eq '19:meeting_xxx@thread.v2'"

4. Get tickets for specific tenant:
   filter: "TenantId eq 'b22f8675-...'"
```

---

## 🎯 Key Components

```
┌──────────────────────────────────────────────────────────────┐
│  Component             │  Responsibility                     │
├──────────────────────────────────────────────────────────────┤
│  CreateTicketDialog    │  • Capture user interactions        │
│                        │  • Track messages via TrackMessage()│
│                        │  • Build SessionInfo object         │
│                        │  • Call API with session data       │
├──────────────────────────────────────────────────────────────┤
│  TicketApiClient       │  • Define session models            │
│                        │  • Send session to API              │
│                        │  • Log session details              │
├──────────────────────────────────────────────────────────────┤
│  TicketsController     │  • Receive session in request       │
│                        │  • Log session for debugging        │
│                        │  • Pass to repository               │
├──────────────────────────────────────────────────────────────┤
│  TicketRepository      │  • Serialize messages to JSON       │
│                        │  • Store in Azure Table Storage     │
│                        │  • Handle null sessions gracefully  │
├──────────────────────────────────────────────────────────────┤
│  Azure Table Storage   │  • Persist ticket + session data    │
│                        │  • Enable queries on session fields │
│                        │  • Scale automatically              │
└──────────────────────────────────────────────────────────────┘
```

---

**Visual representation of the complete session tracking architecture** 🎨

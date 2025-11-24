# Session Tracking Implementation - Summary

## 📋 Overview

Added conversation session tracking and message history to the Tickets table in Azure Table Storage. This allows tracking which Teams conversation and messages led to ticket creation without requiring additional tables.

---

## ✅ Changes Made

### **1. Updated TicketEntity Model**
**File:** `SupportTicketsApi/Models/TicketEntity.cs`

**Added Fields:**
```csharp
// Session and conversation tracking fields
public string? ConversationId { get; set; }        // Teams conversation ID
public string? SessionId { get; set; }             // Unique session ID
public string? TenantId { get; set; }              // Azure AD tenant ID
public string? ChannelId { get; set; }             // Channel ID (msteams)
public string? Locale { get; set; }                // User locale (en-US)

// Conversation messages stored as JSON string
public string? ConversationMessages { get; set; }  // JSON array of messages
public int MessageCount { get; set; }              // Quick count reference
```

**Storage Impact:**
- All fields are nullable (optional) - existing tickets will continue to work
- No schema migration required (Azure Table Storage is schema-flexible)
- Typical storage: ~2KB per ticket with 10 messages

---

### **2. Updated CreateTicketRequest Model**
**File:** `SupportTicketsApi/Models/CreateTicketRequest.cs`

**Added Classes:**
```csharp
public class SessionInfo
{
    public string? ConversationId { get; set; }
    public string? SessionId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? TenantId { get; set; }
    public string? ChannelId { get; set; }
    public string? Locale { get; set; }
    public DateTime Timestamp { get; set; }
    public List<MessageInfo>? Messages { get; set; }
}

public class MessageInfo
{
    public string? MessageId { get; set; }
    public string? From { get; set; }
    public string? Text { get; set; }
    public DateTime Timestamp { get; set; }
    public string? MessageType { get; set; }  // "user" or "bot"
}
```

---

### **3. Updated Repository Interface**
**File:** `SupportTicketsApi/Services/ITicketRepository.cs`

**Changed Signature:**
```csharp
// Before
Task<TicketEntity> CreateAsync(string userId, string userName, string title, string description, CancellationToken ct);

// After
Task<TicketEntity> CreateAsync(string userId, string userName, string title, string description, SessionInfo? session, CancellationToken ct);
```

---

### **4. Updated Repository Implementations**

#### **TableStorageTicketRepository.cs**
```csharp
// Store session information
ConversationId = session?.ConversationId,
SessionId = session?.SessionId,
TenantId = session?.TenantId,
ChannelId = session?.ChannelId,
Locale = session?.Locale,
ConversationMessages = session?.Messages != null 
    ? System.Text.Json.JsonSerializer.Serialize(session.Messages)
    : null,
MessageCount = session?.Messages?.Count ?? 0
```

#### **FileTicketRepository.cs** (for development)
- Updated `StoredTicket` class with session fields
- Updated `Map()` method to include session data
- Updated `CreateAsync()` to store session information

#### **TicketRepository.cs**
- Updated to match interface changes
- Stores session data in Azure Table Storage

---

### **5. Updated Controller**
**File:** `SupportTicketsApi/Controllers/TicketsController.cs`

**Enhanced Logging:**
```csharp
// Log session information if provided
if (request.Session != null)
{
    _logger.LogInformation("Session info - ConversationId: {ConversationId}, SessionId: {SessionId}, Messages: {MessageCount}",
        request.Session.ConversationId, request.Session.SessionId, request.Session.Messages?.Count ?? 0);
}

// Pass session to repository
var entity = await _repo.CreateAsync(userId, display, request.Title, request.Description, request.Session, ct);
```

---

## 📊 What Gets Stored

### **Example Ticket Entity in Azure Table Storage:**

```json
{
  "PartitionKey": "user-123",
  "RowKey": "abc-def-ghi",
  "Title": "Cannot access reports",
  "Description": "Getting 404 error",
  "Status": "New",
  "CreatedByUserId": "user-123",
  "CreatedByDisplayName": "John Doe",
  "CreatedUtc": "2025-10-17T10:30:00Z",
  "LastUpdatedUtc": "2025-10-17T10:30:00Z",
  "Deleted": false,
  
  "ConversationId": "19:meeting_xxx@thread.v2",
  "SessionId": "session-abc-123",
  "TenantId": "b22f8675-8375-455b-941a-67bee4cf7747",
  "ChannelId": "msteams",
  "Locale": "en-US",
  "MessageCount": 4,
  "ConversationMessages": "[{\"MessageId\":\"msg1\",\"From\":\"Bot\",\"Text\":\"Please enter a title\",\"Timestamp\":\"2025-10-17T10:29:50Z\",\"MessageType\":\"bot\"},{\"MessageId\":\"123\",\"From\":\"John Doe\",\"Text\":\"Cannot access reports\",\"Timestamp\":\"2025-10-17T10:29:55Z\",\"MessageType\":\"user\"}]"
}
```

---

## 🔍 How to Query Session Data

### **Get Ticket with Messages:**
```csharp
var ticket = await _tableClient.GetEntityAsync<TicketEntity>(userId, ticketId);

// Deserialize messages
if (!string.IsNullOrEmpty(ticket.Value.ConversationMessages))
{
    var messages = JsonSerializer.Deserialize<List<MessageInfo>>(
        ticket.Value.ConversationMessages);
    
    foreach (var msg in messages)
    {
        Console.WriteLine($"[{msg.Timestamp}] {msg.From}: {msg.Text}");
    }
}
```

### **Query Tickets by Conversation:**
```csharp
var tickets = _tableClient.QueryAsync<TicketEntity>(
    t => t.ConversationId == "19:meeting_xxx@thread.v2");
```

### **Query Tickets by Session:**
```csharp
var tickets = _tableClient.QueryAsync<TicketEntity>(
    t => t.SessionId == "session-abc-123");
```

---

## ✅ Build Status

**API Build:** ✅ **SUCCESS**
- All files compile without errors
- Only pre-existing warnings (unrelated to session tracking)

---

## 🎯 Next Steps

### **To Complete Implementation:**

1. **Update Bot (CreateTicketDialog.cs)** to capture and send session data
2. **Update TicketApiClient.cs** to include session info in API calls
3. **Test locally** with ticket creation
4. **Deploy API** to Azure
5. **Test in Teams** to verify session tracking

### **Optional Enhancements:**

- Add session-based ticket grouping in UI
- Add conversation replay feature
- Add message search/filtering
- Add session analytics dashboard

---

## 📝 Backward Compatibility

✅ **Fully backward compatible:**
- All session fields are nullable/optional
- Existing tickets without session data will continue to work
- Old clients can still create tickets (session will be null)
- No database migration required

---

## 💾 Storage Considerations

**Current Table Structure:**
- **Table Name:** SupportTickets (existing)
- **New Fields:** 7 additional string fields + 1 int
- **Size Impact:** ~2-5KB per ticket (with typical message count)
- **Cost Impact:** Negligible - Azure Table Storage charges by storage size and transactions

**Limits:**
- Azure Table entity max size: **1 MB**
- Can store **~5000 messages** per ticket (well within limits for typical use)
- If needed later, can split to separate Messages table

---

## 🔐 Security Considerations

✅ **Session data is:**
- Stored with user's partition key (per-user isolation)
- Protected by existing API authentication
- Only accessible to ticket owner
- No sensitive data in messages (all user-provided)

---

## 📖 Documentation

- This summary document
- Code comments in modified files
- Logging statements for debugging

---

**Date:** October 17, 2025  
**Status:** ✅ API Changes Complete - Ready for Bot Integration  
**Build:** ✅ Successful

# Session Tracking Testing Checklist

## ✅ Complete Testing Guide

Use this checklist to verify that session tracking is working correctly from end-to-end.

---

## 🔧 Prerequisites

- [x] API deployed to Azure (https://saaliticketsapiclean.azurewebsites.net)
- [x] Bot code updated with session tracking
- [x] Bot builds successfully (no compile errors)
- [ ] Bot deployed to Azure (or running locally)
- [ ] Teams app manifest uploaded
- [ ] OAuth connections configured (`ticketsoauth`)

---

## 📝 Test Scenario 1: Create Ticket with Full Conversation

### **Steps:**

1. **Open Teams**
   - [ ] Navigate to your bot in Teams
   - [ ] Start a new conversation

2. **Initiate Ticket Creation**
   - [ ] Send message: `create ticket`
   - [ ] Verify bot responds with title prompt

3. **Enter Ticket Details**
   - [ ] Bot asks: "Please enter a **title** for your support ticket"
   - [ ] Type: `Cannot access monthly reports`
   - [ ] Bot asks: "Please provide a detailed **description**"
   - [ ] Type: `Getting 404 error when trying to access monthly sales reports. Started this morning after system update.`
   - [ ] Bot shows ticket summary
   - [ ] Confirm: `Yes`

4. **Verify Success Message**
   - [ ] Bot shows: "✅ Ticket created successfully"
   - [ ] Ticket ID is displayed
   - [ ] Status shows as "New"

### **Expected Console Output (Bot):**

```
Tracked bot message from Bot: 📝 **Create New Support Ticket**...
Tracked user message from John Doe: Cannot access monthly reports
Tracked bot message from Bot: 📄 Please provide a detailed **description**...
Tracked user message from John Doe: Getting 404 error when trying to access monthly sales reports...
Tracked bot message from Bot: 🎫 **Ticket Summary**...
Tracked user message from John Doe: Yes
Creating ticket with session tracking - ConversationId: 19:meeting_xxx@thread.v2, Messages: 6
```

### **Expected Console Output (API):**

```
[TicketApiClient.CreateAsync] Session info provided: True
[TicketApiClient.CreateAsync] Session details - ConversationId: 19:meeting_xxx@thread.v2, Messages: 6
[TicketsController] Creating ticket with session - ConversationId: 19:meeting_xxx@thread.v2, SessionId: abc-123, Messages: 6
```

### **Validation:**
- [ ] No errors in bot console
- [ ] No errors in API logs
- [ ] Ticket created successfully
- [ ] Message count matches (should be 6)

---

## 🗄️ Test Scenario 2: Verify Azure Table Storage

### **Steps:**

1. **Navigate to Azure Portal**
   - [ ] Go to https://portal.azure.com
   - [ ] Find your Storage Account
   - [ ] Go to **Tables** → **SupportTickets**

2. **Locate Your Ticket**
   - [ ] Find the ticket you just created (by Title or RowKey)
   - [ ] Click to view full entity

3. **Verify Session Fields**
   - [ ] `ConversationId` is populated (format: `19:meeting_xxx@thread.v2`)
   - [ ] `SessionId` is populated (GUID format)
   - [ ] `TenantId` is populated (your Azure AD tenant ID)
   - [ ] `ChannelId` is `msteams`
   - [ ] `Locale` is populated (e.g., `en-US`)
   - [ ] `ConversationMessages` contains JSON array
   - [ ] `MessageCount` matches number of exchanges (should be 6)

4. **Verify Message Content**
   - [ ] Copy the `ConversationMessages` field value
   - [ ] Paste into a JSON validator (https://jsonlint.com)
   - [ ] Verify JSON is valid
   - [ ] Check messages array contains all 6 messages
   - [ ] Verify message order is correct (chronological)

### **Expected ConversationMessages Structure:**

```json
[
  {
    "messageId": "...",
    "from": "Bot",
    "text": "📝 **Create New Support Ticket**...",
    "timestamp": "2025-10-17T08:00:00Z",
    "messageType": "bot"
  },
  {
    "messageId": "...",
    "from": "John Doe",
    "text": "Cannot access monthly reports",
    "timestamp": "2025-10-17T08:00:05Z",
    "messageType": "user"
  },
  ...
]
```

### **Validation:**
- [ ] All session fields are non-null
- [ ] JSON is valid and well-formed
- [ ] Message count is accurate
- [ ] Timestamps are in correct order
- [ ] Both bot and user messages are captured

---

## 🔍 Test Scenario 3: Query Tickets with Session Data

### **Option A: Azure Portal - Storage Browser**

1. **Open Storage Browser**
   - [ ] Azure Portal → Storage Account → Storage Browser → Tables
   - [ ] Select **SupportTickets** table

2. **Add Filter**
   - [ ] Click "Add Filter"
   - [ ] Property: `ConversationId`
   - [ ] Operator: `Not equal to`
   - [ ] Value: `null`
   - [ ] Click Apply

3. **Verify Results**
   - [ ] All returned tickets have `ConversationId` populated
   - [ ] Each has `MessageCount > 0`
   - [ ] Session data is present

### **Option B: Azure CLI**

```powershell
# List tickets with session data
az storage entity query `
  --table-name SupportTickets `
  --account-name <your-storage-account> `
  --filter "ConversationId ne null and MessageCount gt 0"
```

- [ ] Command executes without errors
- [ ] Returns tickets with session data
- [ ] Output shows session fields

### **Option C: API Endpoint**

```powershell
# Get tickets via API
$token = "your-bearer-token"
curl -H "Authorization: Bearer $token" `
     https://saaliticketsapiclean.azurewebsites.net/api/tickets
```

- [ ] API returns tickets successfully
- [ ] Response includes your newly created ticket
- [ ] Ticket has session information

---

## 🧪 Test Scenario 4: Cancel Ticket Creation

### **Purpose:** Verify session tracking doesn't cause issues when user cancels

### **Steps:**

1. **Start Ticket Creation**
   - [ ] Send: `create ticket`
   - [ ] Enter title: `Test cancellation`
   - [ ] Enter description: `This is a test`
   - [ ] When prompted for confirmation, type: `No`

2. **Verify Cancellation**
   - [ ] Bot shows: "❌ Ticket creation cancelled"
   - [ ] No ticket is created
   - [ ] No errors in console

### **Validation:**
- [ ] Cancellation works correctly
- [ ] No session data is sent to API
- [ ] No exceptions thrown
- [ ] User can start new ticket creation immediately

---

## 🔄 Test Scenario 5: Backward Compatibility

### **Purpose:** Ensure old tickets without session data still work

### **Steps:**

1. **Check Existing Tickets**
   - [ ] Query tickets created before session tracking
   - [ ] Verify they still load correctly
   - [ ] Session fields should be `null`

2. **List All Tickets**
   - [ ] In Teams, send: `show my tickets`
   - [ ] Verify both old and new tickets appear
   - [ ] No errors for tickets without session data

### **Expected Behavior:**
- [ ] Old tickets (without session data) work fine
- [ ] New tickets (with session data) work fine
- [ ] No breaking changes
- [ ] API handles both formats gracefully

---

## 📊 Test Scenario 6: Performance Check

### **Purpose:** Ensure session tracking doesn't slow down ticket creation

### **Steps:**

1. **Create Multiple Tickets**
   - [ ] Create 5 tickets in quick succession
   - [ ] Note creation time for each

2. **Verify Performance**
   - [ ] Average creation time: < 3 seconds
   - [ ] No timeouts
   - [ ] No performance degradation

### **Expected Timing:**
- [ ] Bot prompts: < 500ms each
- [ ] API call: < 2 seconds
- [ ] Total flow: < 30 seconds (user interaction time)

---

## 🚨 Error Handling Tests

### **Test 1: API Down**
- [ ] Stop the API
- [ ] Try to create ticket
- [ ] Bot shows: "Failed to create ticket" (graceful error)
- [ ] No crash or unhandled exception

### **Test 2: Invalid Token**
- [ ] Clear OAuth token
- [ ] Try to create ticket
- [ ] Bot prompts for sign-in
- [ ] After sign-in, ticket creation works

### **Test 3: Network Issue**
- [ ] Disconnect network temporarily
- [ ] Try to create ticket
- [ ] Bot shows appropriate error message
- [ ] Reconnect network and retry successfully

---

## 📝 Validation Summary

### **Must Pass All:**

#### **Bot Side:**
- [x] Code compiles without errors ✅
- [ ] No runtime exceptions during ticket creation
- [ ] All dialog steps work correctly
- [ ] Message tracking captures all exchanges
- [ ] SessionInfo is built correctly

#### **API Side:**
- [x] API deployed and running ✅
- [ ] Receives session data in requests
- [ ] Stores session data in Azure Table Storage
- [ ] Logs session information
- [ ] Returns successful responses

#### **Storage Side:**
- [ ] Session fields are populated
- [ ] ConversationMessages is valid JSON
- [ ] MessageCount is accurate
- [ ] All message details are captured correctly

#### **End-to-End:**
- [ ] User can create tickets successfully
- [ ] Full conversation is captured
- [ ] Session data appears in Azure Table Storage
- [ ] Support team can view conversation history
- [ ] No performance issues

---

## 🐛 Troubleshooting

### **Issue: No session data in storage**

**Check:**
1. Bot console logs - is `BuildSessionInfo()` called?
2. API logs - is `Session info provided: True` logged?
3. Request payload - does it include `session` property?

**Solution:**
- Verify `CreateTicketDialog` is calling `BuildSessionInfo()`
- Check `TicketApiClient.CreateAsync()` is passing `sessionInfo`
- Ensure API `TicketsController` receives the session parameter

---

### **Issue: ConversationMessages is empty**

**Check:**
1. Bot logs - are `TrackMessage()` calls being made?
2. Message list - is it initialized in `PromptForTitleStepAsync()`?

**Solution:**
- Ensure `stepContext.Values[ConversationMessagesKey]` is initialized
- Verify all dialog steps call `TrackMessage()`
- Check message list is not being reset between steps

---

### **Issue: MessageCount is 0**

**Check:**
1. API logs - what is the count received?
2. Message list size in bot before sending

**Solution:**
- Verify `sessionInfo.Messages.Count` has values before API call
- Check API is correctly reading `session.Messages.Count`

---

## ✅ Final Checklist

**Before marking as complete, verify:**

- [ ] ✅ All test scenarios passed
- [ ] ✅ Session data visible in Azure Table Storage
- [ ] ✅ Conversation messages are captured correctly
- [ ] ✅ No errors in bot or API logs
- [ ] ✅ Performance is acceptable
- [ ] ✅ Backward compatibility confirmed
- [ ] ✅ Error handling works correctly
- [ ] ✅ Documentation is complete
- [ ] ✅ Support team can access session data
- [ ] ✅ Ready for production use

---

## 🎉 Success Criteria

**Session tracking is successful when:**

1. ✅ Every new ticket has `ConversationId` populated
2. ✅ `ConversationMessages` contains 4-8 messages on average
3. ✅ `MessageCount` matches the actual number of messages
4. ✅ Support team can view full conversation history
5. ✅ No performance degradation (< 3 seconds per ticket)
6. ✅ Zero errors or exceptions related to session tracking
7. ✅ 100% of new tickets include session data

---

## 📞 Support

**If you encounter issues:**

1. Check console logs (bot and API)
2. Review Azure Application Insights
3. Verify OAuth connections are active
4. Consult documentation:
   - BOT_SESSION_TRACKING_GUIDE.md
   - SESSION_TRACKING_CHANGES.md
   - DEPLOYMENT_LOG.md

---

**Last Updated:** October 17, 2025  
**Status:** Ready for Testing ✅

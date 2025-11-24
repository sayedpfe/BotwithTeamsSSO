# Integration Quickstart Template
## 60–90 Minute Custom API Enablement Path

Use this template as a repeatable playbook to integrate ANY Azure-hosted REST API with the Teams SSO Bot platform. It is organized into timed phases for workshops or internal onboarding.

---
## 🧭 High-Level Flow
```
Phase 0   Validate API + Goals (5m)
Phase 1   Configure OAuth (10m)
Phase 2   Wire API Client (15m)
Phase 3   Build Dialog (20m)
Phase 4   Connect to MainDialog (10m)
Phase 5   Local Test & Iterations (15m)
Phase 6   Pre-Prod Checklist (5m)
```
Total: ~80 minutes (first integration). Subsequent integrations average 30–40 minutes.

---
## ✅ Prerequisites Checklist
| Item | Status | Notes |
|------|--------|-------|
| Azure AD Tenant Access | ☐ | Needed for app registration & consent |
| Bot App Registration (existing) | ☑ | Provided in solution (`MicrosoftAppId`) |
| Custom API Hosted (Dev URL) | ☐ | e.g., `https://your-api-dev.azurewebsites.net` |
| API Secured by Azure AD | ☐ | JWT validation configured |
| Defined Endpoint(s) | ☐ | Example: `POST /api/orders` |
| Scope Created (`access_as_user`) | ☐ | In API App Registration |
| Teams App Manifest Ready | ☑ | Already in repo (update later if new commands) |

Mark all as complete before moving to execution.

---
## 🗂 Folder Touchpoints
| Component | Location | Action |
|-----------|----------|--------|
| Bot Config | `BotConversationSsoQuickstart/appsettings.json` | Add connection + API base URL |
| API Client | `BotConversationSsoQuickstart/Services/` | New `{YourApi}Client.cs` |
| Dialog | `BotConversationSsoQuickstart/Dialogs/` | New dialog class |
| Enum Extension | `GraphAction.cs` | Add new action value |
| MainDialog | `MainDialog.cs` | Wire OAuth prompt + action routing |
| Teams Manifest | `M365Agent/appPackage/manifest.json` | (Optional) Add command/description |

---
## 🕐 Phase 0: Validate API (5m)
1. Confirm HTTPS endpoint(s)
2. Confirm Azure AD protection or plan to add
3. Identify minimum payload for create/list operations
4. Decide initial dialog flow (prompt(s) + confirmation)

Outcome: ONE primary action (e.g., Create Order) + ONE optional list/read action.

---
## 🔐 Phase 1: Configure OAuth (10m)
1. Azure Portal → App Registrations → Your API
2. Expose an API → Application ID URI: `api://botid-{BotAppId}` OR `api://{ApiAppId}`
3. Add scope: `access_as_user`
4. Grant admin consent
5. In Bot resource → OAuth Connection Settings → Add:
   - Name: `yourcustomapi`
   - Service Provider: Azure AD v2
   - Client ID: (Bot App ID)
   - Client Secret: (Bot secret)
   - Tenant ID: (Tenant GUID)
   - Scopes: `api://botid-{BotAppId}/access_as_user`

Update `appsettings.json`:
```json
"ConnectionNameYourAPI": "yourcustomapi",
"YourCustomApi": {
  "BaseUrl": "https://your-api-dev.azurewebsites.net/",
  "AuthType": "AzureAD"
}
```

---
## 🌐 Phase 2: Wire API Client (15m)
Create file `Services/YourApiClient.cs` based on template in `API_INTEGRATION_PLATFORM_GUIDE.md`:
Core Requirements:
- Constructor reads `YourCustomApi:BaseUrl`
- Methods: `CreateAsync(...)`, `ListAsync(...)` (adjust names)
- Accept `userToken` + `SessionInfo`
- Serialize request using `JsonContent.Create`
- Handle non-success (log + return null)

Register in `Program.cs`:
```csharp
builder.Services.AddHttpClient<YourApiClient>();
```

Smoke Test (optional):
```csharp
// Temporary console call inside Program after build.Services...
// var client = builder.Services.BuildServiceProvider().GetService<YourApiClient>();
```

---
## 💬 Phase 3: Build Dialog (20m)
Create `Dialogs/YourCustomDialog.cs`:
- Derive from `ComponentDialog`
- Waterfall steps: prompt → detail → confirm → execute
- Track messages (reuse pattern from `CreateTicketDialog`) using:
  - `ConversationMessagesKey`
  - `TrackMessage()`
  - `BuildSessionInfo()`
- Invoke API client in final step

Minimal Example Snippet:
```csharp
var result = await _apiClient.CreateAsync(name, details, token, sessionInfo, ct);
if (result == null) await stepContext.Context.SendActivityAsync("Failed.");
else await stepContext.Context.SendActivityAsync($"Created: {result.Id}");
```

Success Criteria:
- Dialog compiles
- Uses injected `YourApiClient`
- Returns after EndDialog

---
## 🔗 Phase 4: Connect to MainDialog (10m)
1. Add new enum value in `GraphAction.cs` (e.g., `CreateOrder`)
2. Inject `YourApiClient` into `MainDialog` constructor
3. Add OAuthPrompt:
```csharp
AddDialog(new OAuthPrompt("YourApiOAuthPrompt", new OAuthPromptSettings {
  ConnectionName = _yourApiConnection,
  Title = "Sign in (Your API)",
  Text = "Please sign in to continue",
  Timeout = 300000
}));
```
4. Update routing in token acquisition step:
```csharp
if (action == GraphAction.CreateOrder) { connection = _yourApiConnection; promptId = "YourApiOAuthPrompt"; }
```
5. Add switch case to launch your dialog.

---
## 🧪 Phase 5: Local Test & Iterations (15m)
Commands:
```powershell
# Run bot
pwsh
cd BotConversationSsoQuickstart
dotnet run
```
Test Flow:
1. Message bot with trigger phrase (e.g., "create order")
2. Complete OAuth sign-in
3. Provide prompted inputs
4. Confirm action
5. Observe API call + response

Validation Points:
- Token retrieved (check console logs)
- API returns 200 OK
- SessionInfo transmitted (inspect API logs)
- Dialog ends cleanly

---
## 🧾 Phase 6: Pre-Prod Checklist (5m)
| Item | Status |
|------|--------|
| Remove temporary logging | ☐ |
| Add null/empty validation | ☐ |
| Add Adaptive Card (optional) | ☐ |
| Confirm scope works in production tenant | ☐ |
| Update `BaseUrl` for prod | ☐ |
| Update Teams manifest (command list) | ☐ |
| Add telemetry enrichment (correlation IDs) | ☐ |
| Add retry for transient HTTP failures | ☐ |

---
## 🔍 Validation Script (Optional)
For quick smoke testing with Bot Framework Emulator (if configured).

Expected Console Log Events:
```
[YourApiClient] Creating resource - Name: SampleName
[YourApiClient] Authorization header added
[YourApiClient] Response: OK
```

---
## 🛠 Common Adjustments
| Need | Change |
|------|--------|
| Add list command | Implement `ListAsync()` → Add new dialog or branch |
| Add update | Add `UpdateAsync()` and a new dialog with ID capture |
| Rich UI | Replace confirmation text with Adaptive Card | 
| Multi-step validation | Insert extra Waterfall steps with `TextPrompt` validators |
| Role-based gating | Check `tokenValue` claims in dialog before calling API |

---
## 🧱 Reusable Code Blocks
Prompt:
```csharp
return await stepContext.PromptAsync("TextPrompt", new PromptOptions { Prompt = MessageFactory.Text(promptText) }, ct);
```
Tracking:
```csharp
messages.Add(new YourApiClient.MessageInfo { MessageId = Guid.NewGuid().ToString(), Text = text, From = from, Timestamp = DateTime.UtcNow, MessageType = type });
```
Session:
```csharp
new YourApiClient.SessionInfo { ConversationId = act.Conversation?.Id, SessionId = Guid.NewGuid().ToString(), UserId = act.From?.Id, UserName = userName, TenantId = act.Conversation?.TenantId, ChannelId = act.ChannelId, Locale = act.Locale ?? "en-US", Timestamp = DateTime.UtcNow, Messages = messages };
```

---
## 📦 Packaging for Production
1. Swap API base URL to production
2. Rotate bot & API secrets (if using new tenant/app)
3. Rebuild & publish:
```powershell
cd BotConversationSsoQuickstart
dotnet publish -c Release -o ./publish
```
4. Zip and deploy to Azure Web App (bot hosting)
5. Update Teams manifest if new commands added
6. Re-upload app package or app update via Teams Admin Center

---
## 📈 Scaling Additional Integrations
Repeat with naming conventions:
- Client: `HrApiClient`, `FinanceApiClient`
- Dialogs: `CreateLeaveDialog`, `SubmitExpenseDialog`
- Enum Values: `CreateLeave`, `SubmitExpense`
- Connection Names: `hrapi`, `financeapi`

Keep each integration atomic—avoid monolithic dialogs.

---
## 🧪 Test Matrix Template
| Scenario | Input | Expected | Notes |
|----------|-------|----------|-------|
| Create basic | Name + details | 200 + ID returned | |
| Empty name | "" | Validation message | Add TextPrompt validator |
| Token missing | Revoke session | OAuth prompt restart | |
| API 500 | Force server error | Graceful failure message | Retry? |
| Long input | > 500 chars | Truncate or reject | Define rule |
| Concurrent calls | 3 rapid creates | All succeed or queue | Monitor perf |

---
## 🆘 Troubleshooting Quick Reference
| Symptom | Cause | Fix |
|---------|-------|-----|
| 401 Unauthorized | Wrong scope URI | Ensure `api://botid-{BotAppId}/access_as_user` |
| Null result object | Deserialization mismatch | Adjust DTO to match JSON |
| Dialog hangs | Missing `EndDialogAsync` | Return result in last step |
| Token value empty | OAuth prompt ID mismatch | Ensure prompt ID used in BeginDialog |
| Session messages empty | Key not initialized | Initialize list at first step |

---
## 🧩 Optional Enhancements
| Enhancement | Benefit |
|------------|---------|
| Adaptive Cards | Rich structured responses |
| Proactive notifications | Event-driven UX |
| Telemetry correlation IDs | Faster debugging |
| Circuit breaker for API | Resilience under failure |
| Polly retry policies | Handle transient outages |
| Bulk operations | Reduce repeated prompts |

---
## 🚀 Final Hand-Off Summary
Your integration is complete when:
- A user triggers the custom action in Teams
- OAuth flow completes for your API
- Dialog collects data, confirms, executes API call
- Response surfaces ID or result
- SessionInfo logged server-side
- Failure states produce human-friendly messages

Each new integration reuses >80% of existing platform code.

---
## 📝 Copy/Paste Action Summary
1. Add OAuth connection → `yourcustomapi`
2. Update `appsettings.json`
3. Create `YourApiClient.cs`
4. Register client in `Program.cs`
5. Create `YourCustomDialog.cs`
6. Extend `GraphAction` enum
7. Wire routing in `MainDialog`
8. Run + test locally
9. Deploy
10. (Optional) Update Teams manifest

Done. Repeat as needed.

# API Integration Platform Guide
## Transform the Teams SSO Bot into Your Custom API Gateway

---

## 📋 Overview

This guide shows how to **replace the Support Tickets API with your own custom API**, transforming this Teams bot into a powerful integration platform for any Azure-hosted REST API. The bot provides ready-to-use authentication, session tracking, and conversational UI - you just plug in your API.

### **What You Get Out of the Box**

✅ **Teams Integration** - Complete Microsoft Teams bot framework  
✅ **Azure AD Authentication** - Dual OAuth connections (Graph + Custom API)  
✅ **User-Delegated Tokens** - Secure, user-specific API calls  
✅ **Session Tracking** - Automatic conversation context capture  
✅ **Interactive Dialogs** - Professional multi-step conversations  
✅ **Error Handling** - Timeout protection, retry logic, graceful failures  
✅ **Logging & Monitoring** - Built-in diagnostics and telemetry  

### **What You Need to Provide**

🔧 **Your Custom API** - Any REST API hosted on Azure  
🔐 **API Authentication Config** - Azure AD app registration for your API  
📝 **Business Logic** - Your domain-specific operations  

---

## 🏗️ Architecture: Before & After

### **Current Architecture (Support Tickets API)**

```
┌─────────────┐
│ Teams User  │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────────────┐
│          Teams SSO Bot                      │
│  ┌────────────────────────────────────┐    │
│  │   MainDialog                       │    │
│  │   • Graph Actions                  │    │
│  │   • Ticket Actions                 │    │
│  └────────┬───────────────────────────┘    │
│           │                                 │
│  ┌────────▼───────────────────────────┐    │
│  │   CreateTicketDialog               │    │
│  │   • Title prompt                   │    │
│  │   • Description prompt             │    │
│  │   • Confirmation                   │    │
│  │   • Session tracking               │    │
│  └────────┬───────────────────────────┘    │
│           │                                 │
│  ┌────────▼───────────────────────────┐    │
│  │   TicketApiClient                  │    │
│  │   • CreateAsync()                  │    │
│  │   • ListAsync()                    │    │
│  │   • Session info                   │    │
│  └────────┬───────────────────────────┘    │
└───────────┼─────────────────────────────────┘
            │
            ▼
┌───────────────────────────────────────────┐
│    Support Tickets API                    │
│    • Create ticket                        │
│    • List tickets                         │
│    • Azure Table Storage                  │
└───────────────────────────────────────────┘
```

### **Your Custom Architecture (Replace API)**

```
┌─────────────┐
│ Teams User  │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────────────┐
│          Teams SSO Bot                      │
│  ┌────────────────────────────────────┐    │
│  │   MainDialog                       │    │
│  │   • Graph Actions                  │    │
│  │   • YOUR Custom Actions ←─────────┐│    │
│  └────────┬───────────────────────────┘│    │
│           │                            │    │
│  ┌────────▼───────────────────────────┼┐   │
│  │   YOUR Custom Dialog               ││   │
│  │   • YOUR prompts                   ││   │
│  │   • YOUR validation                ││   │
│  │   • YOUR confirmation              ││   │
│  │   • Session tracking (built-in)    ││   │
│  └────────┬───────────────────────────┼┘   │
│           │                            │    │
│  ┌────────▼───────────────────────────┼┐   │
│  │   YOUR API Client                  ││   │
│  │   • YOUR operations                ││   │
│  │   • Session info (built-in)        ││   │
│  └────────┬───────────────────────────┼┘   │
└───────────┼────────────────────────────┼────┘
            │                            │
            ▼                            │
┌───────────────────────────────────────┼────┐
│    YOUR CUSTOM API                    │    │
│    • YOUR endpoints                   │    │
│    • YOUR business logic              │    │
│    • YOUR data storage                │    │
└───────────────────────────────────────┼────┘
                                        │
                              REUSE THIS PATTERN
```

---

## 🚀 Step-by-Step Integration Guide

### **Phase 1: Prepare Your Custom API**

#### **1.1 API Requirements Checklist**

Your API must have:
- ✅ **REST endpoints** (HTTP/HTTPS)
- ✅ **Azure AD authentication** (JWT bearer tokens)
- ✅ **CORS configured** (allow bot origin)
- ✅ **Hosted on Azure** (App Service, Functions, Container Apps, etc.)

#### **1.2 Create Azure AD App Registration for Your API**

```bash
# Create app registration for your API
az ad app create \
  --display-name "Your-Custom-API" \
  --sign-in-audience AzureADMyOrg \
  --enable-id-token-issuance false \
  --enable-access-token-issuance true

# Note the Application (client) ID from output
# Example: 12345678-1234-1234-1234-123456789abc
```

#### **1.3 Expose API and Define Scopes**

In Azure Portal → App Registrations → Your API:

1. **Expose an API**:
   - Application ID URI: `api://botid-{YourBotAppId}` or `api://{YourApiAppId}`
   - Add scope: `access_as_user` (Admin and users can consent)

2. **API Permissions** (if your API calls other services):
   - Add delegated permissions as needed

#### **1.4 Configure Your API to Validate Tokens**

**Example for ASP.NET Core:**

```csharp
// Startup.cs or Program.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Audience = "api://botid-{YourBotAppId}"; // Match your App ID URI
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

services.AddAuthorization();

app.UseAuthentication();
app.UseAuthorization();

// Protect your endpoints
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class YourController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> YourOperation([FromBody] YourRequest request)
    {
        // Get user identity from token
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User.Identity?.Name;
        
        // Your business logic here
        return Ok(result);
    }
}
```

---

### **Phase 2: Configure Bot OAuth Connection for Your API**

#### **2.1 Create OAuth Connection in Azure Bot Service**

1. **Navigate to Azure Portal** → Your Bot → Configuration → OAuth Connection Settings

2. **Add New Connection**:
   - **Name**: `yourcustomapi` (choose a meaningful name)
   - **Service Provider**: Azure Active Directory v2
   - **Client ID**: Your bot's App ID (89155d3a-359d-4603-b821-0504395e331f)
   - **Client Secret**: Your bot's client secret
   - **Tenant ID**: Your Azure AD tenant ID
   - **Token Exchange URL**: Leave empty
   - **Scopes**: `api://botid-{YourBotAppId}/access_as_user`

3. **Save** and test the connection

#### **2.2 Update Bot Configuration**

**File**: `BotConversationSsoQuickstart/appsettings.json`

```json
{
  "MicrosoftAppId": "89155d3a-359d-4603-b821-0504395e331f",
  "MicrosoftAppPassword": "your-bot-secret",
  "ConnectionName": "oauthbotsetting",
  "ConnectionNameGraph": "oauthbotsetting",
  "ConnectionNameTickets": "ticketsoauth",          // ← Keep for reference
  "ConnectionNameYourAPI": "yourcustomapi",         // ← ADD THIS
  "MicrosoftAppType": "SingleTenant",
  "MicrosoftAppTenantId": "your-tenant-id",
  "TicketApi": {
    "BaseUrl": "https://saaliticketsapiclean.azurewebsites.net/",
    "AuthType": "AzureAD"
  },
  "YourCustomApi": {                                // ← ADD THIS SECTION
    "BaseUrl": "https://your-api.azurewebsites.net/",
    "AuthType": "AzureAD"
  }
}
```

---

### **Phase 3: Create Your Custom API Client**

#### **3.1 Create Your API Client Class**

**File**: `BotConversationSsoQuickstart/Services/YourApiClient.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotBuilderSamples.Services
{
    public class YourApiClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _authType;

        // Session tracking models (reuse from TicketApiClient)
        public class MessageInfo
        {
            public string MessageId { get; set; }
            public string From { get; set; }
            public string Text { get; set; }
            public DateTime Timestamp { get; set; }
            public string MessageType { get; set; }
        }

        public class SessionInfo
        {
            public string ConversationId { get; set; }
            public string SessionId { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string TenantId { get; set; }
            public string ChannelId { get; set; }
            public string Locale { get; set; }
            public DateTime Timestamp { get; set; }
            public List<MessageInfo> Messages { get; set; } = new List<MessageInfo>();
        }

        // YOUR custom DTOs
        public record YourDto(string Id, string Name, string Status);
        public record YourCreateRequest(string Name, string Details, SessionInfo Session);

        public YourApiClient(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _baseUrl = cfg["YourCustomApi:BaseUrl"]?.TrimEnd('/') 
                ?? throw new InvalidOperationException("YourCustomApi:BaseUrl missing");
            _authType = cfg["YourCustomApi:AuthType"] ?? "None";
        }

        public async Task<YourDto> CreateAsync(
            string name, 
            string details, 
            string userToken, 
            SessionInfo sessionInfo, 
            CancellationToken ct)
        {
            try
            {
                Console.WriteLine($"[YourApiClient] Creating resource - Name: {name}");
                
                using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/your-endpoint");
                
                if (_authType == "AzureAD" && !string.IsNullOrEmpty(userToken))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
                    Console.WriteLine("[YourApiClient] Authorization header added");
                }

                var requestBody = new YourCreateRequest(name, details, sessionInfo);
                req.Content = JsonContent.Create(requestBody);
                
                var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                Console.WriteLine($"[YourApiClient] Response: {resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[YourApiClient] ERROR: {body}");
                    return null;
                }
                
                return JsonSerializer.Deserialize<YourDto>(body, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YourApiClient] Exception: {ex.Message}");
                return null;
            }
        }

        public async Task<YourDto[]> ListAsync(int top, string userToken, CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, 
                    $"{_baseUrl}/api/your-endpoint?top={top}");
                
                if (_authType == "AzureAD" && !string.IsNullOrEmpty(userToken))
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
                }

                var resp = await _http.SendAsync(req, ct);
                var json = await resp.Content.ReadAsStringAsync(ct);
                
                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[YourApiClient] ERROR: {resp.StatusCode}");
                    return null;
                }
                
                return JsonSerializer.Deserialize<YourDto[]>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YourApiClient] Exception: {ex.Message}");
                return null;
            }
        }
    }
}
```

#### **3.2 Register Your API Client in Dependency Injection**

**File**: `BotConversationSsoQuickstart/Program.cs`

```csharp
// Add your API client
builder.Services.AddHttpClient<YourApiClient>();
```

---

### **Phase 4: Create Your Custom Dialog**

#### **4.1 Create Your Dialog Class**

**File**: `BotConversationSsoQuickstart/Dialogs/YourCustomDialog.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.BotBuilderSamples.Services;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class YourCustomOptions
    {
        public string UserToken { get; set; }
    }

    public class YourCustomDialog : ComponentDialog
    {
        private readonly ILogger _logger;
        private readonly YourApiClient _apiClient;

        // Dialog IDs
        private const string NamePromptId = "NamePrompt";
        private const string DetailsPromptId = "DetailsPrompt";
        private const string ConfirmPromptId = "ConfirmPrompt";

        // Step values keys
        private const string NameKey = "name";
        private const string DetailsKey = "details";
        private const string UserTokenKey = "userToken";
        private const string ConversationMessagesKey = "conversationMessages";

        public YourCustomDialog(YourApiClient apiClient, ILogger<YourCustomDialog> logger)
            : base(nameof(YourCustomDialog))
        {
            _apiClient = apiClient;
            _logger = logger;

            AddDialog(new TextPrompt(NamePromptId));
            AddDialog(new TextPrompt(DetailsPromptId));
            AddDialog(new ConfirmPrompt(ConfirmPromptId));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptForNameStepAsync,
                PromptForDetailsStepAsync,
                ConfirmStepAsync,
                CreateResourceStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptForNameStepAsync(
            WaterfallStepContext stepContext, CancellationToken ct)
        {
            var options = stepContext.Options as YourCustomOptions;
            if (options?.UserToken != null)
            {
                stepContext.Values[UserTokenKey] = options.UserToken;
            }

            stepContext.Values[ConversationMessagesKey] = 
                new List<YourApiClient.MessageInfo>();

            var promptText = "Please enter a name:";
            TrackMessage(stepContext, "Bot", promptText, "bot");

            return await stepContext.PromptAsync(NamePromptId, 
                new PromptOptions { Prompt = MessageFactory.Text(promptText) }, ct);
        }

        private async Task<DialogTurnResult> PromptForDetailsStepAsync(
            WaterfallStepContext stepContext, CancellationToken ct)
        {
            var name = (string)stepContext.Result;
            stepContext.Values[NameKey] = name;
            TrackMessage(stepContext, stepContext.Context.Activity.From?.Name ?? "User", 
                name, "user");

            var promptText = "Please provide details:";
            TrackMessage(stepContext, "Bot", promptText, "bot");

            return await stepContext.PromptAsync(DetailsPromptId,
                new PromptOptions { Prompt = MessageFactory.Text(promptText) }, ct);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(
            WaterfallStepContext stepContext, CancellationToken ct)
        {
            var details = (string)stepContext.Result;
            stepContext.Values[DetailsKey] = details;
            TrackMessage(stepContext, stepContext.Context.Activity.From?.Name ?? "User", 
                details, "user");

            var name = (string)stepContext.Values[NameKey];
            var confirmText = $"Create resource '{name}'?";
            TrackMessage(stepContext, "Bot", confirmText, "bot");

            return await stepContext.PromptAsync(ConfirmPromptId,
                new PromptOptions { Prompt = MessageFactory.Text(confirmText) }, ct);
        }

        private async Task<DialogTurnResult> CreateResourceStepAsync(
            WaterfallStepContext stepContext, CancellationToken ct)
        {
            var confirmed = (bool)stepContext.Result;
            TrackMessage(stepContext, stepContext.Context.Activity.From?.Name ?? "User", 
                confirmed ? "Yes" : "No", "user");

            if (!confirmed)
            {
                await stepContext.Context.SendActivityAsync("Cancelled.", cancellationToken: ct);
                return await stepContext.EndDialogAsync(cancellationToken: ct);
            }

            var name = (string)stepContext.Values[NameKey];
            var details = (string)stepContext.Values[DetailsKey];
            var activity = stepContext.Context.Activity;
            var userName = activity.From?.Name ?? "Unknown";

            await stepContext.Context.SendActivityAsync("Creating...", cancellationToken: ct);

            try
            {
                var userToken = stepContext.Values.ContainsKey(UserTokenKey)
                    ? stepContext.Values[UserTokenKey] as string
                    : null;

                var sessionInfo = BuildSessionInfo(stepContext, activity, userName);

                var result = await _apiClient.CreateAsync(name, details, userToken, 
                    sessionInfo, ct);

                if (result != null)
                {
                    await stepContext.Context.SendActivityAsync(
                        $"✅ Created: {result.Name} (ID: {result.Id})", cancellationToken: ct);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(
                        "Failed to create resource.", cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating resource");
                await stepContext.Context.SendActivityAsync(
                    "An error occurred.", cancellationToken: ct);
            }

            return await stepContext.EndDialogAsync(cancellationToken: ct);
        }

        // Reuse tracking methods from CreateTicketDialog
        private void TrackMessage(WaterfallStepContext stepContext, string from, 
            string text, string messageType)
        {
            var messages = (List<YourApiClient.MessageInfo>)
                stepContext.Values[ConversationMessagesKey];
            
            messages.Add(new YourApiClient.MessageInfo
            {
                MessageId = Guid.NewGuid().ToString(),
                From = from,
                Text = text,
                Timestamp = DateTime.UtcNow,
                MessageType = messageType
            });
        }

        private YourApiClient.SessionInfo BuildSessionInfo(
            WaterfallStepContext stepContext, Activity activity, string userName)
        {
            var messages = (List<YourApiClient.MessageInfo>)
                stepContext.Values[ConversationMessagesKey];

            return new YourApiClient.SessionInfo
            {
                ConversationId = activity.Conversation?.Id,
                SessionId = Guid.NewGuid().ToString(),
                UserId = activity.From?.Id,
                UserName = userName,
                TenantId = activity.Conversation?.TenantId,
                ChannelId = activity.ChannelId,
                Locale = activity.Locale ?? "en-US",
                Timestamp = DateTime.UtcNow,
                Messages = messages
            };
        }
    }
}
```

---

### **Phase 5: Integrate with MainDialog**

#### **5.1 Add Your Custom Action**

**File**: `BotConversationSsoQuickstart/Dialogs/GraphAction.cs`

```csharp
public enum GraphAction
{
    None = 0,
    Profile,
    RecentMail,
    SendTestMail,
    CreateTicket,
    ListTickets,
    ShowFeedbackForm,
    YourCustomAction  // ← ADD THIS
}
```

#### **5.2 Update MainDialog**

**File**: `BotConversationSsoQuickstart/Dialogs/MainDialog.cs`

```csharp
public class MainDialog : LogoutDialog
{
    private readonly YourApiClient _yourApiClient;  // ← ADD THIS
    private readonly string _yourApiConnection;     // ← ADD THIS
    
    private const string YourCustomDialogId = "YourCustomDialog";  // ← ADD THIS
    
    public MainDialog(
        IConfiguration config,
        TicketApiClient ticketClient,
        YourApiClient yourApiClient,  // ← ADD THIS
        ILoggerFactory loggerFactory)
        : base(nameof(MainDialog), config["ConnectionNameGraph"])
    {
        _yourApiClient = yourApiClient;  // ← ADD THIS
        _yourApiConnection = config["ConnectionNameYourAPI"] 
            ?? throw new InvalidOperationException("ConnectionNameYourAPI missing");
        
        // Add your custom OAuth prompt
        AddDialog(new OAuthPrompt(
            "YourApiOAuthPrompt",  // ← ADD THIS
            new OAuthPromptSettings
            {
                ConnectionName = _yourApiConnection,
                Title = "Sign in (Your API)",
                Text = "Please sign in to access your custom API.",
                Timeout = 300000
            }));
        
        // Add your custom dialog
        AddDialog(new YourCustomDialog(_yourApiClient, 
            loggerFactory.CreateLogger<YourCustomDialog>()));  // ← ADD THIS
        
        // ... rest of initialization
    }
    
    private bool IsYourApiAction(GraphAction action) =>
        action == GraphAction.YourCustomAction;  // ← ADD THIS
    
    private async Task<DialogTurnResult> EnsureResourceTokenStepAsync(
        WaterfallStepContext step, CancellationToken ct)
    {
        // ... existing code ...
        
        // Add your API to the routing logic
        if (IsYourApiAction(opts.Action))  // ← ADD THIS
        {
            connection = _yourApiConnection;
            promptId = "YourApiOAuthPrompt";
        }
        
        // ... rest of method
    }
    
    private async Task<DialogTurnResult> ExecuteActionStepAsync(
        WaterfallStepContext step, CancellationToken ct)
    {
        // ... existing code ...
        
        switch (action)
        {
            // ... existing cases ...
            
            case GraphAction.YourCustomAction:  // ← ADD THIS
                return await step.BeginDialogAsync(YourCustomDialogId, 
                    new YourCustomOptions { UserToken = tokenValue }, ct);
            
            // ... rest of cases
        }
    }
}
```

---

### **Phase 6: Test Your Integration**

#### **6.1 Local Testing**

```bash
# Run your custom API
cd YourCustomApi
dotnet run

# Run the bot
cd BotConversationSsoQuickstart
dotnet run

# Use Bot Framework Emulator or Teams to test
```

#### **6.2 Test Authentication Flow**

1. Send message to bot: `your custom action`
2. Bot prompts for sign-in to your API
3. Complete OAuth flow
4. Bot calls your custom dialog
5. Follow prompts and verify API calls

#### **6.3 Verify Session Tracking**

Check your API logs for:
```
Received session data:
- ConversationId: 19:meeting_xxx@thread.v2
- SessionId: abc-123-def-456
- Messages: 6
- User: John Doe
```

---

## 📊 Integration Patterns

### **Pattern 1: Simple CRUD Operations**

Use this pattern for basic create/read/update/delete operations:

```
Bot Dialog → API Client → Your API → Database
```

**Example**: HR Leave Requests, Expense Submissions

### **Pattern 2: Multi-Step Workflows**

Use this pattern for complex business processes:

```
Bot Dialog 1 → API Call 1 → Get Options
Bot Dialog 2 → API Call 2 → Submit Selection
Bot Dialog 3 → API Call 3 → Confirm Action
```

**Example**: Approval Workflows, Multi-Stage Forms

### **Pattern 3: Read-Only Dashboards**

Use this pattern for information display:

```
Bot Dialog → API Client → Your API → Read Data → Format Response
```

**Example**: Status Dashboards, Reports, Metrics

### **Pattern 4: Integration Hub**

Use this pattern to integrate multiple APIs:

```
Bot → API Client 1 → External API 1
    → API Client 2 → External API 2
    → API Client 3 → External API 3
```

**Example**: Multi-System Integration, Orchestration

---

## 🔐 Security Best Practices

### **Authentication**

✅ **Use user-delegated tokens** (not app-only)  
✅ **Validate tokens** on your API side  
✅ **Check audience claim** matches your API  
✅ **Verify issuer** is your Azure AD tenant  
✅ **Validate token expiration**  

### **Authorization**

✅ **Check user permissions** in your API  
✅ **Implement role-based access control**  
✅ **Log all user actions** for audit  
✅ **Validate input data** to prevent injection  

### **Data Protection**

✅ **Encrypt sensitive data** at rest and in transit  
✅ **Don't log sensitive information**  
✅ **Implement rate limiting**  
✅ **Use HTTPS everywhere**  

---

## 🧪 Testing Checklist

- [ ] OAuth connection works (bot can get token for your API)
- [ ] Your API validates tokens correctly
- [ ] Dialog prompts display properly
- [ ] User inputs are validated
- [ ] API calls succeed with user token
- [ ] Session tracking data is captured
- [ ] Error handling works (network errors, timeouts)
- [ ] User can complete full workflow
- [ ] Conversation context is sent to your API
- [ ] Your API stores/processes session data correctly

---

## 📦 Deployment

### **Deploy Your API**

```bash
# Publish your API
cd YourCustomApi
dotnet publish -c Release -o ./publish

# Deploy to Azure App Service
az webapp deployment source config-zip \
  --resource-group your-rg \
  --name your-api-app \
  --src ./publish.zip
```

### **Deploy Bot**

```bash
# No changes needed - just update appsettings.json with production URLs
cd BotConversationSsoQuickstart
dotnet publish -c Release -o ./publish

# Deploy to Azure Bot Service
az webapp deployment source config-zip \
  --resource-group your-rg \
  --name your-bot-app \
  --src ./publish.zip
```

---

## 🆘 Troubleshooting

### **Problem: OAuth connection fails**

**Solution**: 
- Verify App ID URI matches in both bot connection and API configuration
- Check scopes are correct: `api://botid-{AppId}/access_as_user`
- Ensure admin consent is granted

### **Problem: API returns 401 Unauthorized**

**Solution**:
- Check token audience claim matches your API
- Verify issuer is correct Azure AD tenant
- Ensure token hasn't expired
- Check API authentication middleware is configured

### **Problem: Session data not reaching API**

**Solution**:
- Verify `BuildSessionInfo()` is called in dialog
- Check API client passes `SessionInfo` parameter
- Ensure your API endpoint accepts session data in request body

---

## 🎓 Learning Resources

- [Bot Framework SDK Documentation](https://docs.microsoft.com/en-us/azure/bot-service/)
- [Azure AD OAuth 2.0](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow)
- [Teams Bot Authentication](https://docs.microsoft.com/en-us/microsoftteams/platform/bots/how-to/authentication/auth-flow-bot)

---

## ✅ Summary

You now have a complete guide to transform this Teams SSO Bot into an integration platform for your custom API. The key benefits:

1. ✅ **Reuse authentication infrastructure** - No need to rebuild OAuth
2. ✅ **Leverage session tracking** - Get conversation context automatically
3. ✅ **Professional dialogs** - Interactive UX out of the box
4. ✅ **Secure by design** - User-delegated tokens, not app-only
5. ✅ **Production-ready** - Error handling, logging, monitoring included

**Your custom API integrates in 6 phases with minimal code changes!** 🚀

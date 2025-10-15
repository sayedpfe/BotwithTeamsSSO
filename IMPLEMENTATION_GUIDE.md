# User-Delegated Token Implementation Guide
## Teams Bot with Support Tickets API - OAuth Configuration

---

## 📋 Overview

This guide documents the implementation of **user-delegated authentication** for a Teams bot that calls a custom Support Tickets API. The solution uses **separate OAuth connections** for Microsoft Graph and the custom API, eliminating the need for complex On-Behalf-Of (OBO) token exchange.

### **Problem Statement**
- The bot initially used **app-only tokens** (client credentials flow) for API calls
- Requirement: Change to **user-delegated tokens** to track which user creates/views tickets
- Challenge: OAuth connection provides Graph tokens, but API expects tokens with a different audience

### **Solution Architecture**
- ✅ Use **two separate OAuth connections** in Azure Bot Service:
  - `oauthbotsetting` → Microsoft Graph API (audience: `https://graph.microsoft.com`)
  - `ticketsoauth` → Support Tickets API (audience: `api://botid-{AppId}`)
- ✅ Bot determines which connection to use based on action type
- ✅ API validates tokens with correct audience
- ✅ No OBO complexity - direct token usage

---

## 🏗️ Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Teams User                              │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ "my tickets"
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Teams Bot                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              MainDialog                                   │  │
│  │  • Determines action type (Graph vs Tickets)             │  │
│  │  • Routes to appropriate OAuth connection                │  │
│  └──────────────────┬───────────────────┬───────────────────┘  │
│                     │                   │                        │
│         ┌───────────▼─────────┐  ┌──────▼──────────────┐       │
│         │ Graph OAuth Prompt  │  │ Tickets OAuth Prompt│       │
│         │ (oauthbotsetting)   │  │  (ticketsoauth)     │       │
│         └───────────┬─────────┘  └──────┬──────────────┘       │
│                     │                   │                        │
└─────────────────────┼───────────────────┼────────────────────────┘
                      │                   │
         ┌────────────▼──────────┐       │
         │  Microsoft Graph      │       │
         │  Token Audience:      │       │
         │  graph.microsoft.com  │       │
         └───────────────────────┘       │
                                          │
                      ┌───────────────────▼─────────────────────┐
                      │    Azure AD Token Endpoint              │
                      │    Issues token with audience:          │
                      │    api://botid-{AppId}                  │
                      └───────────────────┬─────────────────────┘
                                          │
                                          │ User token
                                          ▼
                      ┌───────────────────────────────────────┐
                      │    TicketApiClient                    │
                      │    • Receives user token              │
                      │    • Adds to Authorization header     │
                      │    • Calls API                        │
                      └───────────────────┬───────────────────┘
                                          │
                                          │ HTTP Request + Bearer token
                                          ▼
                      ┌───────────────────────────────────────┐
                      │    Support Tickets API                │
                      │    • Validates JWT token              │
                      │    • Checks audience matches          │
                      │    • Processes request as user        │
                      └───────────────────────────────────────┘
```

---

## 🔧 Implementation Details

### **1. Bot Configuration (appsettings.json)**

**File:** `BotConversationSsoQuickstart/appsettings.json`

```json
{
    "MicrosoftAppId": "89155d3a-359d-4603-b821-0504395e331f",
    "MicrosoftAppPassword": "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI",
    "ConnectionName": "oauthbotsetting",
    "ConnectionNameGraph": "oauthbotsetting",
    "ConnectionNameTickets": "ticketsoauth",
    "MicrosoftAppType": "SingleTenant",
    "MicrosoftAppTenantId": "b22f8675-8375-455b-941a-67bee4cf7747",
    "TicketApi": {
        "BaseUrl": "https://saaliticketsapiclean.azurewebsites.net/",
        "AuthType": "AzureAD"
    }
}
```

**Key Changes:**
- ✅ Added `ConnectionNameTickets`: Points to dedicated OAuth connection for Tickets API
- ✅ `ConnectionNameGraph`: Existing OAuth connection for Microsoft Graph
- ✅ `TicketApi.AuthType`: Set to "AzureAD" to enable authenticated API calls

---

#### **⚠️ Important: Why MicrosoftAppPassword is Still Required**

**Common Question:** *"If we're using user-delegated tokens from OAuth connections, why do we still need the bot's client secret (MicrosoftAppPassword)?"*

**Answer:** The `MicrosoftAppPassword` serves a **different authentication purpose** than user OAuth tokens. There are **two separate authentication layers**:

##### **Authentication Layer 1: Bot Infrastructure Authentication**
```
┌────────────────────────────────────────────────────────────┐
│  Bot ←→ Azure Bot Service Communication                   │
│  Uses: MicrosoftAppId + MicrosoftAppPassword              │
│  Purpose: Bot identity authentication                      │
└────────────────────────────────────────────────────────────┘
```

**What it does:**
- Authenticates the bot application itself to Azure Bot Service
- Required for **receiving messages** from Teams/channels
- Required for **sending responses** back to users
- Required for **retrieving user OAuth tokens** from Bot Service
- Used by Bot Framework SDK automatically in the background

**Code Example:**
```csharp
// Bot Framework uses these credentials automatically when:

// 1. Receiving messages from Teams
// Validates incoming Activity is from legitimate Bot Service

// 2. Sending responses
// Signs outgoing messages with bot's credentials

// 3. Getting user OAuth tokens
var tokenProvider = context.Adapter as IUserTokenProvider;
var userToken = await tokenProvider.GetUserTokenAsync(
    context, 
    "ticketsoauth",  // OAuth connection name
    null, 
    cancellationToken);
// ↑ This call requires bot to authenticate with client secret!
```

##### **Authentication Layer 2: User OAuth Authentication**
```
┌────────────────────────────────────────────────────────────┐
│  User ←→ Bot ←→ API Communication                         │
│  Uses: User-delegated OAuth tokens                         │
│  Purpose: User identity and API authorization              │
└────────────────────────────────────────────────────────────┘
```

**What it does:**
- Represents the **user's identity** (not the bot's)
- Required for **API calls on behalf of the user**
- Contains user's permissions/scopes (Tickets.ReadWrite)
- Obtained through OAuth consent flow

##### **How They Work Together:**

```
┌─────────────────────────────────────────────────────────────────┐
│ STEP 1: User sends message "my tickets"                        │
└─────────┬───────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ STEP 2: Teams → Azure Bot Service → Bot                        │
│ • Bot receives message (requires client secret to validate)    │
└─────────┬───────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ STEP 3: Bot requests user OAuth token                          │
│ • Bot authenticates with client secret to Bot Service          │
│ • Bot Service returns cached/new user OAuth token              │
│ • This is the user-delegated token for API                     │
└─────────┬───────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ STEP 4: Bot calls API with user token                          │
│ • API validates user token (not bot credentials)               │
│ • API processes request as the user                            │
└─────────┬───────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────┐
│ STEP 5: Bot sends response to user                             │
│ • Bot signs response with client secret                        │
│ • Azure Bot Service → Teams → User                             │
└─────────────────────────────────────────────────────────────────┘
```

##### **What Happens If You Remove MicrosoftAppPassword?**

❌ **Bot will fail to start or authenticate:**
- Cannot validate incoming messages from Azure Bot Service
- Cannot send responses back to Teams
- Cannot retrieve user OAuth tokens from Bot Service
- Bot Framework SDK throws authentication errors

**Error you would see:**
```
Microsoft.Bot.Connector.Authentication.AuthenticationException: 
Unauthorized. Invalid AppId or password.
```

##### **Summary:**

| Authentication Type | Purpose | Credentials Used | Audience |
|---------------------|---------|------------------|----------|
| **Bot Infrastructure** | Bot ↔ Azure Bot Service | `MicrosoftAppId` + `MicrosoftAppPassword` | Azure Bot Service |
| **User OAuth** | User identity for API | User-delegated token from OAuth connection | Custom API (`api://botid-{AppId}`) |

**Key Point:** 
- The **bot's client secret** authenticates the bot application itself to Azure infrastructure
- The **user's OAuth token** authenticates the user to your custom API
- **Both are required** - they serve completely different purposes and cannot replace each other

**Analogy:**
Think of it like a hotel:
- **Bot credentials (client secret):** Hotel staff badge - proves you work at the hotel and can access the system
- **User OAuth token:** Guest room key card - proves which guest you're helping and which room they can access

The staff badge doesn't give you access to guest rooms, and the guest key card doesn't let you log into the hotel management system. You need both for the system to work correctly.

---

### **2. MainDialog.cs - Dual OAuth Connection Handling**

**File:** `BotConversationSsoQuickstart/Dialogs/MainDialog.cs`

#### **2.1 Constructor - Initialize Two OAuth Prompts**

```csharp
public MainDialog(IConfiguration config,
                  TicketApiClient ticketClient,
                  ILoggerFactory loggerFactory)
    : base(nameof(MainDialog), config["ConnectionNameGraph"])
{
    _logger = loggerFactory.CreateLogger<MainDialog>();
    _ticketClient = ticketClient;
    
    // Load both OAuth connection names from configuration
    _graphConnection = config["ConnectionNameGraph"] 
        ?? throw new InvalidOperationException("ConnectionNameGraph missing");
    _ticketsConnection = config["ConnectionNameTickets"] 
        ?? throw new InvalidOperationException("ConnectionNameTickets missing");

    // Create OAuth prompt for Microsoft Graph
    AddDialog(new OAuthPrompt(
        GraphPromptId,
        new OAuthPromptSettings
        {
            ConnectionName = _graphConnection,  // "oauthbotsetting"
            Title = "Sign in (Graph)",
            Text = "Please sign in to access Microsoft Graph resources.",
            Timeout = 300000
        }));

    // Create OAuth prompt for Tickets API
    AddDialog(new OAuthPrompt(
        TicketsPromptId,
        new OAuthPromptSettings
        {
            ConnectionName = _ticketsConnection,  // "ticketsoauth"
            Title = "Sign in (Tickets)",
            Text = "Please sign in to access Support Tickets.",
            Timeout = 300000
        }));
    
    // ... rest of dialog setup
}
```

**Key Points:**
- Two separate `OAuthPrompt` dialogs are registered
- Each prompt uses a different Azure Bot Service OAuth connection
- `_graphConnection` → Gets Graph tokens
- `_ticketsConnection` → Gets API tokens with correct audience

---

#### **2.2 Token Acquisition Logic - Route to Correct OAuth**

```csharp
private async Task<DialogTurnResult> EnsureResourceTokenStepAsync(
    WaterfallStepContext step, CancellationToken ct)
{
    var opts = step.Options as GraphActionOptions ?? new GraphActionOptions();
    step.Values[ActionKey] = opts.Action;

    string connection;
    string promptId;
    
    // Determine which OAuth connection to use based on action type
    if (IsGraphAction(opts.Action))
    {
        connection = _graphConnection;     // "oauthbotsetting"
        promptId = GraphPromptId;
    }
    else if (IsTicketsAction(opts.Action))
    {
        connection = _ticketsConnection;   // "ticketsoauth"
        promptId = TicketsPromptId;
    }
    else
    {
        await step.Context.SendActivityAsync("Unsupported action.", cancellationToken: ct);
        return await step.EndDialogAsync(cancellationToken: ct);
    }

    // Try silent token acquisition first
    if (step.Context.Adapter is IUserTokenProvider tp)
    {
        var silent = await tp.GetUserTokenAsync(step.Context, connection, null, ct);
        if (silent != null && !string.IsNullOrEmpty(silent.Token))
        {
            tokens[connection] = silent.Token;
            return await step.NextAsync(null, ct);
        }
    }

    // If silent acquisition fails, prompt user to sign in
    return await step.BeginDialogAsync(promptId, cancellationToken: ct);
}
```

**Key Logic:**
1. **Action Type Detection:**
   - `IsGraphAction()` → Profile, Recent Mail, Send Mail
   - `IsTicketsAction()` → List Tickets, Create Ticket

2. **Connection Selection:**
   - Graph actions → `oauthbotsetting` (Graph token)
   - Tickets actions → `ticketsoauth` (API token)

3. **Silent Token Check:**
   - First tries to get cached token without prompting user
   - If cached token exists, skip OAuth prompt

4. **OAuth Prompt:**
   - If no cached token, show appropriate sign-in prompt
   - User authenticates once, token is cached for session

---

#### **2.3 Token Retrieval and API Call**

```csharp
private async Task<DialogTurnResult> ExecuteActionStepAsync(
    WaterfallStepContext step, CancellationToken ct)
{
    var action = (GraphAction)step.Values[ActionKey];
    var tokens = (Dictionary<string, string>)step.Values[TokensKey];

    string tokenValue = null;
    string connectionNeeded = null;

    // Determine which connection was used based on action type
    if (IsGraphAction(action))
    {
        connectionNeeded = _graphConnection;
    }
    else if (IsTicketsAction(action))
    {
        connectionNeeded = _ticketsConnection;
    }

    // Retrieve the token for the required connection
    if (connectionNeeded != null)
    {
        // If came from OAuth prompt, capture the token
        if (!tokens.ContainsKey(connectionNeeded))
        {
            if (step.Result is TokenResponse tokenResponse && 
                !string.IsNullOrEmpty(tokenResponse.Token))
            {
                tokens[connectionNeeded] = tokenResponse.Token;
            }
        }

        if (!tokens.TryGetValue(connectionNeeded, out tokenValue))
        {
            await step.Context.SendActivityAsync(
                "Authentication failed or was cancelled.", cancellationToken: ct);
            return await step.EndDialogAsync(cancellationToken: ct);
        }
    }

    // Execute the appropriate action with the correct token
    switch (action)
    {
        case GraphAction.ListTickets:
            await ExecuteListTicketsAsync(step, tokenValue, ct);
            break;
        case GraphAction.CreateTicket:
            return await step.BeginDialogAsync(
                CreateTicketDialogId, 
                new CreateTicketOptions { UserToken = tokenValue }, 
                ct);
        // ... other actions
    }
}
```

**Key Points:**
- Retrieves token from the appropriate OAuth connection
- Passes user token to `TicketApiClient` methods
- Token is specific to the action being performed

---

### **3. TicketApiClient.cs - Simplified Token Handling**

**File:** `BotConversationSsoQuickstart/Services/TicketApiClient.cs`

#### **3.1 Direct Token Usage (No OBO)**

```csharp
private async Task<string> GetAccessTokenAsync(string userToken, CancellationToken ct)
{
    // Use the token directly from the ticketsoauth OAuth connection
    // This token already has the correct audience for the API
    if (string.IsNullOrEmpty(userToken))
    {
        Console.WriteLine("[GetAccessTokenAsync] No user token provided");
        return null;
    }

    Console.WriteLine($"[GetAccessTokenAsync] Using token directly from ticketsoauth connection");
    Console.WriteLine($"[GetAccessTokenAsync] Token length: {userToken.Length}");
    
    if (userToken.Length > 50)
    {
        var preview = $"{userToken.Substring(0, 25)}...{userToken.Substring(userToken.Length - 25)}";
        Console.WriteLine($"[GetAccessTokenAsync] Token preview: {preview}");
    }
    
    LastTokenUsed = userToken;
    return await Task.FromResult(userToken);
}
```

**Key Changes:**
- ❌ **Removed:** On-Behalf-Of (OBO) token exchange logic
- ✅ **Simplified:** Direct usage of token from OAuth connection
- ✅ Token already has correct audience: `api://botid-89155d3a-359d-4603-b821-0504395e331f`
- ✅ Logging for debugging and visibility

---

#### **3.2 API Call with User Token**

```csharp
public async Task<TicketDto[]> ListAsync(int top, string userToken, CancellationToken ct)
{
    try
    {
        Console.WriteLine($"[ListAsync] Starting - BaseUrl: {_base}, Top: {top}");
        Console.WriteLine($"[ListAsync] User token provided: {!string.IsNullOrEmpty(userToken)}");
        
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{_base}/api/tickets?top={top}");
        
        // Get the user token (directly from OAuth connection)
        var token = await GetAccessTokenAsync(userToken, ct);
        Console.WriteLine($"[ListAsync] Token to use: {(token != null ? $"Yes (length: {token.Length})" : "No")}");
        
        // Add token to Authorization header
        if (token != null)
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Console.WriteLine("[ListAsync] Authorization header added");
        }

        // Make the API call
        Console.WriteLine($"[ListAsync] Making GET request to: {_base}/api/tickets?top={top}");
        var resp = await _http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        
        Console.WriteLine($"[ListAsync] Response status: {resp.StatusCode}");
        Console.WriteLine($"[ListAsync] Response body: {json}");
        
        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine($"[ListAsync] ERROR: API returned {resp.StatusCode}");
            return null;
        }
        
        var result = JsonSerializer.Deserialize<TicketDto[]>(
            json, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        Console.WriteLine($"[ListAsync] Successfully deserialized {result?.Length ?? 0} tickets");
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ListAsync] Exception: {ex.Message}");
        Console.WriteLine($"[ListAsync] Stack trace: {ex.StackTrace}");
        return null;
    }
}
```

**Key Points:**
- Accepts `userToken` parameter from MainDialog
- Adds token to `Authorization: Bearer {token}` header
- Comprehensive logging for troubleshooting
- Same pattern used in `CreateAsync()` and `SubmitFeedbackAsync()`

---

### **4. Support Tickets API - Token Validation**

**File:** `SupportTicketsApi/Program.cs`

#### **4.1 JWT Bearer Authentication Configuration**

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Azure AD tenant and authority
        options.Authority = "https://login.microsoftonline.com/b22f8675-8375-455b-941a-67bee4cf7747/v2.0";
        
        // Primary audience (bot app ID)
        options.Audience = "89155d3a-359d-4603-b821-0504395e331f";
        
        // Accept tokens with any of these audience values
        options.TokenValidationParameters.ValidAudiences = new[]
        {
            "89155d3a-359d-4603-b821-0504395e331f",              // Bot App ID
            "api://89155d3a-359d-4603-b821-0504395e331f",        // API format
            "api://botid-89155d3a-359d-4603-b821-0504395e331f"   // OAuth connection format ✅
        };
        
        // Validate token issuer
        options.TokenValidationParameters.ValidIssuer = 
            "https://sts.windows.net/b22f8675-8375-455b-941a-67bee4cf7747/";
        
        // Logging for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });
```

**Critical Configuration:**
1. **Authority:** Points to Azure AD tenant endpoint
2. **Audience:** Primary audience is the bot's App ID
3. **ValidAudiences:** **MUST include `api://botid-{AppId}` format** ✅
   - This matches the audience in tokens from `ticketsoauth` OAuth connection
4. **ValidIssuer:** Ensures token is from correct Azure AD tenant
5. **Events:** Logging for authentication success/failure

---

## ☁️ Azure Configuration

### **1. Azure AD App Registration**

**App ID:** `89155d3a-359d-4603-b821-0504395e331f`

#### **1.1 Expose an API**
Navigate to: **Azure AD** → **App registrations** → **Your App** → **Expose an API**

**Configuration:**
```
Application ID URI: api://botid-89155d3a-359d-4603-b821-0504395e331f
```

**Scopes:**
| Scope Name | Value |
|------------|-------|
| Tickets.ReadWrite | `api://botid-89155d3a-359d-4603-b821-0504395e331f/Tickets.ReadWrite` |
| access_as_user | `api://botid-89155d3a-359d-4603-b821-0504395e331f/access_as_user` |

**Scope Configuration:**
- **Who can consent:** Admins and users
- **Admin consent display name:** Access Support Tickets API
- **Admin consent description:** Allows the app to access the Support Tickets API on behalf of the signed-in user
- **User consent display name:** Access your support tickets
- **User consent description:** Allows the app to read and write support tickets on your behalf
- **State:** Enabled

---

### **2. Azure Bot Service - OAuth Connections**

#### **2.1 Graph OAuth Connection**

Navigate to: **Azure Bot** → **Configuration** → **OAuth Connection Settings** → **oauthbotsetting**

| Setting | Value |
|---------|-------|
| **Name** | `oauthbotsetting` |
| **Service Provider** | Azure Active Directory v2 |
| **Client ID** | `89155d3a-359d-4603-b821-0504395e331f` |
| **Client Secret** | `<your-bot-app-secret>` |
| **Tenant ID** | `b22f8675-8375-455b-941a-67bee4cf7747` |
| **Token Exchange URL** | *(leave empty for Graph)* |
| **Scopes** | `openid profile offline_access User.Read Mail.Read Mail.Send` |

**Purpose:** Provides tokens for Microsoft Graph API calls

---

#### **2.2 Tickets API OAuth Connection**

Navigate to: **Azure Bot** → **Configuration** → **OAuth Connection Settings** → **ticketsoauth**

| Setting | Value |
|---------|-------|
| **Name** | `ticketsoauth` |
| **Service Provider** | Azure Active Directory v2 |
| **Client ID** | `89155d3a-359d-4603-b821-0504395e331f` |
| **Client Secret** | `<your-bot-app-secret>` |
| **Tenant ID** | `b22f8675-8375-455b-941a-67bee4cf7747` |
| **Token Exchange URL** | `api://botid-89155d3a-359d-4603-b821-0504395e331f` |
| **Scopes** | `openid profile offline_access api://botid-89155d3a-359d-4603-b821-0504395e331f/Tickets.ReadWrite` |

**Purpose:** Provides tokens for Support Tickets API calls

**Critical Fields:**
- ✅ **Token Exchange URL:** Must match the Application ID URI from "Expose an API"
- ✅ **Scopes:** Must include your custom scope with full URI

---

### **3. Azure Web App (API Deployment)**

**Resource:** `SaaliTicketsApiClean`
**URL:** `https://saaliticketsapiclean.azurewebsites.net`

#### **3.1 Application Settings**

Navigate to: **Azure Web App** → **Configuration** → **Application settings**

No special app settings required - authentication is configured in code.

#### **3.2 Deployment**

**Method:** Azure CLI Zip Deploy

```powershell
# Navigate to API project
cd SupportTicketsApi

# Publish in Release mode
dotnet publish -c Release -o ./publish

# Create deployment package
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force

# Deploy to Azure
az webapp deployment source config-zip `
  --resource-group M365AgentTeamsSSO-rg `
  --name SaaliTicketsApiClean `
  --src ./deploy.zip
```

**Verify deployment:**
```powershell
curl https://saaliticketsapiclean.azurewebsites.net/health
# Expected: {"status":"OK","time":"2025-10-15T09:22:15.0247583+00:00"}
```

---

## 🔄 Token Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        USER INTERACTION                              │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          │ User: "my tickets"
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 1: Action Detection (MainDialog)                               │
│ • IsTicketsAction() returns true                                    │
│ • Routes to ticketsoauth connection                                 │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 2: Token Acquisition (OAuth Prompt)                            │
│ • Check for cached token first                                      │
│ • If not cached, show sign-in card                                  │
│ • User authenticates with Azure AD                                  │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 3: Azure AD Issues Token                                       │
│ • Validates user credentials                                        │
│ • Checks user consented to scopes                                   │
│ • Issues JWT token with:                                            │
│   - Audience: api://botid-89155d3a-359d-4603-b821-0504395e331f    │
│   - Scopes: Tickets.ReadWrite                                       │
│   - User claims (name, email, etc.)                                 │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          │ JWT Token (user-delegated)
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 4: Token Passed to API Client (TicketApiClient)                │
│ • GetAccessTokenAsync() receives token                              │
│ • No transformation needed (direct usage)                           │
│ • Token saved in LastTokenUsed property                             │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          │ HTTP GET /api/tickets
                          │ Authorization: Bearer {token}
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 5: API Validates Token (SupportTicketsApi)                     │
│ • JWT Bearer authentication middleware                              │
│ • Validates signature using Azure AD public keys                    │
│ • Checks audience is in ValidAudiences list                         │
│ • Checks issuer matches ValidIssuer                                 │
│ • Checks token not expired                                          │
│ • Extracts user identity from claims                                │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          │ ✅ Token Valid
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 6: API Processes Request                                       │
│ • Controller receives authenticated request                         │
│ • User identity available in HttpContext.User                       │
│ • Query tickets for authenticated user                              │
│ • Return results                                                    │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          │ HTTP 200 OK + JSON response
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│ STEP 7: Bot Displays Results                                        │
│ • TicketApiClient deserializes response                             │
│ • MainDialog formats and displays tickets                           │
│ • Shows user token for transparency (optional)                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🧪 Testing Steps

### **1. Test OAuth Connection in Azure Portal**

1. Navigate to **Azure Bot** → **Configuration** → **OAuth Connection Settings**
2. Click **Test Connection** next to `ticketsoauth`
3. Sign in with a test user
4. Verify you see "Connection successful" with token displayed
5. **Inspect the token** (copy and paste into jwt.ms):
   - `aud` (audience) should be: `api://botid-89155d3a-359d-4603-b821-0504395e331f`
   - `scp` (scopes) should include: `Tickets.ReadWrite`
   - `name`, `upn` should show user identity

---

### **2. Test Bot in Teams**

1. **Start the bot:**
   ```powershell
   cd BotConversationSsoQuickstart
   dotnet run
   ```

2. **In Teams, send message:** `my tickets`

3. **Expected flow:**
   - Bot shows "Sign in (Tickets)" card
   - Click "Sign in" button
   - Azure AD consent screen appears (first time only)
   - After consent, see "You are now signed in"
   - Bot displays tickets with token preview (for debugging)

4. **Verify console logs:**
   ```
   [GetAccessTokenAsync] Using token directly from ticketsoauth connection
   [GetAccessTokenAsync] Token length: 1543
   [ListAsync] Starting - BaseUrl: https://saaliticketsapiclean.azurewebsites.net/, Top: 5
   [ListAsync] Authorization header added
   [ListAsync] Response status: 200
   [ListAsync] Successfully deserialized 3 tickets
   ```

---

### **3. Test API Directly**

1. **Get a token from OAuth connection** (using Test Connection in Azure Portal)

2. **Make API call with Postman or curl:**
   ```powershell
   $token = "<paste-token-here>"
   
   curl https://saaliticketsapiclean.azurewebsites.net/api/tickets `
     -H "Authorization: Bearer $token"
   ```

3. **Expected response:** `200 OK` with JSON array of tickets

4. **If 401 Unauthorized:**
   - Check API logs in Azure Portal
   - Verify token audience matches ValidAudiences
   - Use jwt.ms to inspect token claims

---

## 🐛 Troubleshooting

### **Problem: 401 Unauthorized from API**

**Symptoms:**
- Bot authentication succeeds
- API returns HTTP 401
- API logs show "Authentication failed"

**Solution:**
1. **Check token audience:**
   - Copy token from bot console logs
   - Paste into https://jwt.ms
   - Verify `aud` claim is `api://botid-89155d3a-359d-4603-b821-0504395e331f`

2. **Check API ValidAudiences:**
   - Open `SupportTicketsApi/Program.cs`
   - Verify `ValidAudiences` array includes the exact audience from step 1
   - **Critical:** Must include `api://botid-` prefix!

3. **Redeploy API** if ValidAudiences was missing the audience

---

### **Problem: OAuth prompt never appears**

**Symptoms:**
- User says "my tickets"
- No sign-in card appears
- Bot says "Authentication failed or was cancelled"

**Solution:**
1. **Check appsettings.json:**
   - Verify `ConnectionNameTickets` is set to `ticketsoauth`

2. **Check Azure Bot OAuth connection:**
   - Ensure connection name exactly matches: `ticketsoauth` (case-sensitive)
   - Test connection in Azure Portal

3. **Check bot code:**
   - Verify `IsTicketsAction()` returns `true` for `ListTickets` action
   - Add logging in `EnsureResourceTokenStepAsync` to see which connection is selected

---

### **Problem: Token has wrong audience**

**Symptoms:**
- OAuth succeeds
- Token audience is `https://graph.microsoft.com` instead of API

**Solution:**
1. **Check OAuth connection configuration:**
   - Verify `ticketsoauth` has correct Token Exchange URL
   - Should be: `api://botid-89155d3a-359d-4603-b821-0504395e331f`

2. **Check scopes in OAuth connection:**
   - Should include: `api://botid-89155d3a-359d-4603-b821-0504395e331f/Tickets.ReadWrite`
   - NOT just `Tickets.ReadWrite` (must be full URI)

3. **Check bot is using correct connection:**
   - Add logging to see which `connectionNeeded` is selected
   - Should be `_ticketsConnection` for ticket actions

---

### **Problem: User consent required every time**

**Symptoms:**
- OAuth prompt appears on every request
- Token not cached between requests

**Solution:**
1. **Check OAuth scopes include offline_access:**
   - Scopes should start with: `openid profile offline_access`
   - This enables refresh tokens

2. **Check bot conversation state:**
   - Silent token acquisition depends on bot conversation state
   - Verify state is persisted correctly

3. **Grant admin consent:**
   - In Azure AD app registration, go to **API permissions**
   - Click "Grant admin consent for {tenant}"
   - This pre-consents for all users in tenant

---

## 📊 Comparison: Before vs After

### **Before (App-Only Tokens)**

```csharp
// Old approach - app-only authentication
var result = await _authApp.AcquireTokenForClient(
    new[] { "api://89155d3a-359d-4603-b821-0504395e331f/.default" })
    .ExecuteAsync(ct);

// Token has:
// - Audience: api://89155d3a-359d-4603-b821-0504395e331f
// - No user identity
// - App-level permissions only
```

**Limitations:**
- ❌ All actions appear as "bot" or "app"
- ❌ Cannot track which user created/viewed tickets
- ❌ Cannot implement user-specific authorization
- ❌ Audit logs show app ID, not user ID

---

### **After (User-Delegated Tokens)**

```csharp
// New approach - user-delegated authentication
// Token comes directly from ticketsoauth OAuth connection
var token = userToken; // From OAuth prompt

// Token has:
// - Audience: api://botid-89155d3a-359d-4603-b821-0504395e331f
// - User identity (name, email, UPN)
// - User-delegated permissions
// - Scopes: Tickets.ReadWrite
```

**Benefits:**
- ✅ API knows which user made the request
- ✅ Can implement row-level security (user sees only their tickets)
- ✅ Audit logs show real user identity
- ✅ Complies with zero-trust security principles
- ✅ Token automatically refreshed with offline_access scope

---

## 📝 Summary of Changes

### **Bot Code Changes**

| File | Change Type | Description |
|------|-------------|-------------|
| `appsettings.json` | **Added** | `ConnectionNameTickets: "ticketsoauth"` |
| `MainDialog.cs` | **Modified** | Added `_ticketsConnection` field |
| `MainDialog.cs` | **Added** | Created `TicketsPromptId` OAuth prompt |
| `MainDialog.cs` | **Modified** | `EnsureResourceTokenStepAsync` routes to correct OAuth |
| `MainDialog.cs` | **Modified** | `ExecuteActionStepAsync` uses correct token per action |
| `TicketApiClient.cs` | **Simplified** | Removed On-Behalf-Of (OBO) logic |
| `TicketApiClient.cs` | **Modified** | Direct token usage from OAuth connection |
| `CreateTicketDialog.cs` | **Modified** | Accepts and passes `UserToken` to API |

---

### **API Code Changes**

| File | Change Type | Description |
|------|-------------|-------------|
| `Program.cs` | **Modified** | Added `api://botid-{AppId}` to ValidAudiences array |

---

### **Azure Configuration Changes**

| Resource | Setting | Change |
|----------|---------|--------|
| **Azure AD App** | Expose an API | Added `Tickets.ReadWrite` scope |
| **Azure Bot** | OAuth Connections | Created `ticketsoauth` connection |
| **Azure Bot** | OAuth Connections | Configured Token Exchange URL |
| **Azure Bot** | OAuth Connections | Configured custom API scopes |

---

## 🎯 Key Takeaways for Customer

1. **Separate OAuth Connections:**
   - Use different OAuth connections for different resource APIs
   - Each connection provides tokens with specific audience
   - No complex OBO token exchange needed

2. **Token Audience is Critical:**
   - OAuth connection Token Exchange URL determines token audience
   - API ValidAudiences MUST include exact audience from token
   - Use jwt.ms to inspect tokens during troubleshooting

3. **User-Delegated Tokens:**
   - Enable user tracking and authorization
   - Provide better security and compliance
   - Require user consent (one-time per user)

4. **Simplified Architecture:**
   - Direct token usage is simpler than OBO
   - Less error-prone configuration
   - Easier to troubleshoot

5. **Testing Strategy:**
   - Always test OAuth connection in Azure Portal first
   - Inspect tokens with jwt.ms
   - Check API logs for authentication failures
   - Verify audience matches before debugging further

---

## 📞 Support Resources

- **JWT Token Inspector:** https://jwt.ms
- **Azure AD Token Reference:** https://learn.microsoft.com/azure/active-directory/develop/access-tokens
- **Bot Framework OAuth:** https://learn.microsoft.com/azure/bot-service/bot-builder-authentication
- **API Authentication:** https://learn.microsoft.com/aspnet/core/security/authentication/

---

**Document Version:** 1.0  
**Last Updated:** October 15, 2025  
**Tested On:** .NET 6.0 (Bot), .NET 9.0 (API)

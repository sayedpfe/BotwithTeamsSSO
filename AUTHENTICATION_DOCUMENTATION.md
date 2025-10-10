# Teams Enterprise Support Hub - Authentication & API Integration Documentation

## Table of Contents
1. [Overview](#overview)
2. [Solution Architecture](#solution-architecture)
3. [Authentication Architecture](#authentication-architecture)
4. [Azure App Registration Setup](#azure-app-registration-setup)
5. [OAuth Connections Configuration](#oauth-connections-configuration)
6. [Bot Authentication Flow](#bot-authentication-flow)
7. [API Integration](#api-integration)
8. [Configuration Files](#configuration-files)
9. [Troubleshooting](#troubleshooting)
10. [Security Best Practices](#security-best-practices)

## Overview

**Teams Enterprise Support Hub** is a comprehensive Microsoft Teams integration solution that combines Single Sign-On (SSO) authentication with intelligent support ticket management. This document provides a complete guide to understanding and configuring the authentication process for enterprise deployment.

### Solution Overview

The Teams Enterprise Support Hub delivers:
- **Seamless Teams Integration**: Native Microsoft Teams bot experience with SSO authentication
- **Intelligent Support Management**: Automated ticket creation, tracking, and feedback collection
- **Enterprise Security**: Azure AD integration with role-based access control
- **User-Friendly Interface**: Intuitive adaptive cards and conversational interface
- **Scalable Architecture**: Cloud-native design supporting enterprise workloads

The solution enables seamless user authentication within Microsoft Teams while maintaining secure access to backend APIs and support systems.

### Key Components
- **Microsoft Teams Bot**: Bot Framework v4 application
- **Azure App Registration**: Identity provider configuration
- **OAuth Connections**: Secure token management
- **Support Tickets API**: Backend REST API with Azure Table Storage
- **Feedback System**: User feedback collection with persistent storage

---

## Authentication Architecture

### High-Level Architecture Diagram

```mermaid
graph TB
    subgraph "Microsoft Teams"
        User[👤 Teams User]
        TeamsApp[Teams Application]
    end
    
    subgraph "Azure Active Directory"
        AAD[Azure AD Tenant]
        AppReg[App Registration<br/>89155d3a-359d-4603-b821-0504395e331f]
        OAuthConn[OAuth Connections<br/>- oauthbotsetting<br/>- ticketsoauth]
    end
    
    subgraph "Bot Infrastructure"
        Bot[Teams Bot<br/>localhost:7130]
        BotFramework[Bot Framework Service]
    end
    
    subgraph "Backend Services"
        API[Support Tickets API<br/>saaliticketsapiclean.azurewebsites.net]
        Storage[Azure Table Storage<br/>- tickets<br/>- feedback]
    end
    
    User --> TeamsApp
    TeamsApp <--> Bot
    Bot <--> BotFramework
    Bot <--> AAD
    AAD --> AppReg
    AppReg --> OAuthConn
    Bot --> API
    API --> Storage
    
    style User fill:#e1f5fe
    style Bot fill:#f3e5f5
    style API fill:#e8f5e8
    style Storage fill:#fff3e0
```

### Authentication Flow Sequence

```mermaid
sequenceDiagram
    participant U as Teams User
    participant T as Teams Client
    participant B as Teams Bot
    participant BF as Bot Framework
    participant AAD as Azure AD
    participant API as Support API
    participant TS as Table Storage
    
    Note over U,TS: Initial Bot Interaction
    U->>T: Opens Teams Chat
    T->>B: Send message
    B->>B: Check authentication state
    
    Note over U,TS: OAuth Flow (if not authenticated)
    alt User not authenticated
        B->>BF: Request OAuth prompt
        BF->>AAD: Initiate OAuth flow
        AAD->>U: Show consent dialog
        U->>AAD: Grant permissions
        AAD->>BF: Return authorization code
        BF->>AAD: Exchange for access token
        AAD->>BF: Return access/refresh tokens
        BF->>B: Provide tokens
    end
    
    Note over U,TS: API Operations
    B->>API: Make API call (no auth required)
    API->>TS: Query/Store data
    TS->>API: Return results
    API->>B: Return response
    B->>T: Send response with feedback buttons
    
    Note over U,TS: Feedback Submission
    U->>T: Click feedback button
    T->>B: Submit feedback form
    B->>API: POST /api/feedback
    API->>TS: Store feedback
    TS->>API: Confirm storage
    API->>B: Return success
    B->>T: Show confirmation
```

---

## Azure App Registration Setup

### Step 1: Create App Registration

1. **Navigate to Azure Portal**
   - Go to [Azure Portal](https://portal.azure.com)
   - Search for "App registrations"

2. **Create New Registration**
   ```
   Name: TeamsBot-SupportTickets
   Supported account types: Single tenant
   Redirect URI: Leave empty (will configure later)
   ```

3. **Record Application Details**
   ```
   Application (client) ID: 89155d3a-359d-4603-b821-0504395e331f
   Directory (tenant) ID: b22f8675-8375-455b-941a-67bee4cf7747
   ```

### Step 2: Configure Authentication

1. **Add Redirect URIs**
   ```
   Platform: Web
   Redirect URIs:
   - https://token.botframework.com/.auth/web/redirect
   - https://teams.microsoft.com/api/platform/v1.0/teams/app/auth/callback
   ```

2. **Configure Implicit Grant**
   ```
   ✓ Access tokens (used for implicit flows)
   ✓ ID tokens (used for implicit and hybrid flows)
   ```

### Step 3: Generate Client Secret

1. **Navigate to "Certificates & secrets"**
2. **Create new client secret**
   ```
   Description: Bot Authentication Secret
   Expires: 24 months
   Value: Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI
   ```

### Step 4: Configure API Permissions

1. **Required Permissions**
   ```
   Microsoft Graph:
   - User.Read (Delegated)
   - Mail.Read (Delegated)
   - Mail.Send (Delegated)
   
   Optional for enhanced features:
   - User.ReadBasic.All (Delegated)
   - Mail.ReadWrite (Delegated)
   ```

2. **Grant Admin Consent**
   - Click "Grant admin consent for [Your Tenant]"
   - Confirm all permissions are granted

---

## OAuth Connections Configuration

### Connection 1: Graph API Access (oauthbotsetting)

```json
{
  "name": "oauthbotsetting",
  "serviceProviderId": "aadv2",
  "parameters": {
    "ClientId": "89155d3a-359d-4603-b821-0504395e331f",
    "ClientSecret": "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI",
    "TenantId": "b22f8675-8375-455b-941a-67bee4cf7747",
    "Scopes": "User.Read Mail.Read Mail.Send"
  }
}
```

### Connection 2: Tickets API Access (ticketsoauth)

```json
{
  "name": "ticketsoauth",
  "serviceProviderId": "aadv2",
  "parameters": {
    "ClientId": "89155d3a-359d-4603-b821-0504395e331f",
    "ClientSecret": "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI",
    "TenantId": "b22f8675-8375-455b-941a-67bee4cf7747",
    "Scopes": "https://graph.microsoft.com/.default"
  }
}
```

### OAuth Connection Setup Process

1. **Access Bot Framework Portal**
   - Go to [Bot Framework Portal](https://dev.botframework.com)
   - Select your bot

2. **Configure OAuth Connections**
   - Navigate to "Settings" > "OAuth Connection Settings"
   - Click "Add Setting"
   - Fill in connection details as shown above

3. **Test Connections**
   - Use "Test Connection" feature
   - Verify successful authentication flow

---

## Bot Authentication Flow

### Authentication State Management

```csharp
public class MainDialog : LogoutDialog
{
    private readonly ILogger _logger;
    private readonly string _graphConnection;
    private readonly string _ticketsConnection;

    // Store tokens per connection name inside dialog values
    private const string TokensKey = "tokens";
    private const string ActionKey = "action";

    public MainDialog(IConfiguration config, ...)
    {
        _graphConnection = config["ConnectionNameGraph"];
        _ticketsConnection = config["ConnectionNameTickets"];
        
        // Configure OAuth prompts
        AddDialog(new OAuthPrompt(
            GraphPromptId,
            new OAuthPromptSettings
            {
                ConnectionName = _graphConnection,
                Title = "Sign in (Graph)",
                Text = "Please sign in to access Microsoft Graph resources.",
                Timeout = 300000
            }));
    }
}
```

### Token Acquisition Process

```csharp
private async Task<DialogTurnResult> EnsureResourceTokenStepAsync(
    WaterfallStepContext step, CancellationToken ct)
{
    var tokens = new Dictionary<string, string>();
    
    // Attempt silent token acquisition
    var tokenResponse = await GetTokenAsync(step.Context, connectionName, ct);
    
    if (tokenResponse?.Token != null)
    {
        tokens[connectionName] = tokenResponse.Token;
    }
    else
    {
        // Prompt for authentication
        return await step.BeginDialogAsync(promptId, null, ct);
    }
    
    step.Values[TokensKey] = tokens;
    return await step.NextAsync(null, ct);
}
```

### Microsoft Graph Integration

```csharp
private async Task ExecuteProfileAsync(WaterfallStepContext step, 
    string token, CancellationToken ct)
{
    var graphClient = new SimpleGraphClient(token);
    var profile = await graphClient.GetMeAsync();
    
    var profileCard = new HeroCard
    {
        Title = "User Profile",
        Subtitle = profile.DisplayName,
        Text = $"Email: {profile.Mail}\nJob Title: {profile.JobTitle}"
    };
    
    await step.Context.SendActivityAsync(
        MessageFactory.Attachment(profileCard.ToAttachment()), ct);
}
```

---

## API Integration

### Current Configuration (No Authentication)

The Support Tickets API is currently configured to operate without authentication for simplified development and testing:

```json
{
  "TicketApi": {
    "BaseUrl": "https://saaliticketsapiclean.azurewebsites.net/",
    "AuthType": "None"
  }
}
```

### API Client Implementation

```csharp
public class TicketApiClient
{
    private readonly HttpClient _http;
    private readonly string _base;
    private readonly string _authType;

    public async Task<TicketDto[]> ListAsync(int top, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, 
            $"{_base}/api/tickets?top={top}");
        
        // Add authentication if configured
        var token = await GetAccessTokenAsync(ct);
        if (token != null)
        {
            req.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }

        var resp = await _http.SendAsync(req, ct);
        // Process response...
    }
}
```

### Future Authentication Options

For production deployment, consider implementing one of these authentication patterns:

#### Option 1: Azure AD Bearer Tokens
```csharp
private async Task<string> GetAccessTokenAsync(CancellationToken ct)
{
    if (_authType != "AzureAD" || _authApp == null)
        return null;

    var result = await _authApp.AcquireTokenForClient(
        new[] { "api://your-api-app-id/.default" })
        .ExecuteAsync();
    
    return result.AccessToken;
}
```

#### Option 2: API Key Authentication
```csharp
private void AddApiKeyAuth(HttpRequestMessage request)
{
    request.Headers.Add("X-API-Key", _apiKey);
    // or
    request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
}
```

---

## Configuration Files

### Bot Configuration (appsettings.json)

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
    "AuthType": "None"
  }
}
```

### API Configuration (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "DefaultEndpointsProtocol=https;AccountName=sayedsupportticketsstg;AccountKey=...",
    "TableStorage": "DefaultEndpointsProtocol=https;AccountName=sayedsupportticketsstg;AccountKey=..."
  },
  "TableStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=sayedsupportticketsstg;AccountKey=...",
    "TicketsTableName": "tickets",
    "FeedbackTableName": "feedback"
  }
}
```

### Environment Variables for Production

```bash
# Azure App Registration
MICROSOFT_APP_ID=89155d3a-359d-4603-b821-0504395e331f
MICROSOFT_APP_PASSWORD=Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI
MICROSOFT_APP_TENANT_ID=b22f8675-8375-455b-941a-67bee4cf7747

# OAuth Connections
CONNECTION_NAME_GRAPH=oauthbotsetting
CONNECTION_NAME_TICKETS=ticketsoauth

# API Configuration
TICKET_API_BASE_URL=https://saaliticketsapiclean.azurewebsites.net/
TICKET_API_AUTH_TYPE=None

# Azure Table Storage
TABLE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=sayedsupportticketsstg;AccountKey=...
```

---

## Troubleshooting

### Common Authentication Issues

#### Issue 1: "Consent Required" Error
```
Symptoms: Users see consent dialog repeatedly
Root Cause: Missing admin consent for app permissions
Solution: Grant admin consent in Azure portal
```

#### Issue 2: Token Expiration
```
Symptoms: Authentication works initially but fails later
Root Cause: Access tokens expired, refresh tokens not working
Solution: Implement proper token refresh logic
```

#### Issue 3: Invalid Redirect URI
```
Symptoms: OAuth flow fails with redirect URI mismatch
Root Cause: Misconfigured redirect URIs in app registration
Solution: Verify redirect URIs match exactly
```

### Debugging Steps

1. **Enable Detailed Logging**
   ```csharp
   builder.Logging.AddConsole();
   builder.Logging.SetMinimumLevel(LogLevel.Debug);
   ```

2. **Check Token Validity**
   ```csharp
   Console.WriteLine($"Token: {token?.Substring(0, 20)}...");
   Console.WriteLine($"Expires: {tokenResponse?.Expiration}");
   ```

3. **Validate API Connectivity**
   ```powershell
   # Test API endpoint
   Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Method GET
   
   # Test with authentication header
   $headers = @{ "Authorization" = "Bearer $token" }
   Invoke-RestMethod -Uri "https://api.example.com/protected" -Headers $headers
   ```

### Log Analysis

#### Bot Framework Logs
```
info: Microsoft.Bot.Builder.Integration.AspNet.Core.IBotFrameworkHttpAdapter[0]
      ProcessActivityAsync
info: System.Net.Http.HttpClient.TicketApiClient.LogicalHandler[100]
      Start processing HTTP request GET https://saaliticketsapiclean.azurewebsites.net/api/tickets?top=5
```

#### Authentication Success Indicators
```
[DialogBot] Parsed as GraphActionOptions: ShowFeedbackForm
[TicketApiClient] Authentication header added
[FeedbackDialog] Feedback submitted successfully - ID: abc123
```

---

## Security Best Practices

### Secret Management

1. **Never Store Secrets in Code**
   ```csharp
   // ❌ DON'T DO THIS
   var secret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI";
   
   // ✅ DO THIS
   var secret = _configuration["MicrosoftAppPassword"];
   ```

2. **Use Azure Key Vault for Production**
   ```csharp
   builder.Configuration.AddAzureKeyVault(
       vaultUri: new Uri("https://your-keyvault.vault.azure.net/"),
       credential: new DefaultAzureCredential());
   ```

### Token Security

1. **Implement Token Refresh**
   ```csharp
   private async Task<string> GetValidTokenAsync()
   {
       if (IsTokenExpired(_currentToken))
       {
           _currentToken = await RefreshTokenAsync();
       }
       return _currentToken;
   }
   ```

2. **Secure Token Storage**
   ```csharp
   // Use encrypted storage for tokens
   var protectedToken = DataProtectionExtensions.Protect(token);
   ```

### Network Security

1. **HTTPS Only**
   ```csharp
   app.UseHttpsRedirection();
   app.UseHsts();
   ```

2. **CORS Configuration**
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddDefaultPolicy(policy =>
       {
           policy.WithOrigins("https://teams.microsoft.com")
                 .AllowAnyHeader()
                 .AllowAnyMethod();
       });
   });
   ```

### Monitoring and Auditing

1. **Authentication Events Logging**
   ```csharp
   _logger.LogInformation("User {UserId} authenticated successfully", userId);
   _logger.LogWarning("Failed authentication attempt for {UserId}", userId);
   ```

2. **API Access Monitoring**
   ```csharp
   _logger.LogInformation("API call to {Endpoint} by user {UserId}", endpoint, userId);
   ```

---

## Summary

This authentication system provides a secure, scalable foundation for Teams bot integration with backend APIs. The current implementation supports:

- ✅ **Single Sign-On (SSO)** for seamless user experience
- ✅ **Multi-connection OAuth** for different resource access
- ✅ **Microsoft Graph integration** for user profile and email access
- ✅ **Flexible API authentication** (currently disabled for development)
- ✅ **Comprehensive logging** for debugging and monitoring
- ✅ **Secure token management** with proper expiration handling

The system is designed to be production-ready with minimal configuration changes, particularly around API authentication and secret management.

For questions or additional configuration needs, refer to the troubleshooting section or consult the Microsoft Bot Framework and Azure AD documentation.
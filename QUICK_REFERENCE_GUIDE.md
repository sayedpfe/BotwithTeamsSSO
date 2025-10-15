# Quick Reference Guide - User-Delegated Tokens

## 🎯 Quick Start Checklist

### Azure AD App Registration
- [ ] Navigate to **Expose an API**
- [ ] Set Application ID URI: `api://botid-89155d3a-359d-4603-b821-0504395e331f`
- [ ] Add scope: `Tickets.ReadWrite`
- [ ] Add scope: `access_as_user`
- [ ] Enable both scopes

### Azure Bot Service OAuth Connections

#### Connection 1: oauthbotsetting (Graph)
```
Name: oauthbotsetting
Service Provider: Azure Active Directory v2
Client ID: 89155d3a-359d-4603-b821-0504395e331f
Scopes: openid profile offline_access User.Read Mail.Read Mail.Send
```

#### Connection 2: ticketsoauth (API)
```
Name: ticketsoauth
Service Provider: Azure Active Directory v2
Client ID: 89155d3a-359d-4603-b821-0504395e331f
Token Exchange URL: api://botid-89155d3a-359d-4603-b821-0504395e331f
Scopes: openid profile offline_access api://botid-89155d3a-359d-4603-b821-0504395e331f/Tickets.ReadWrite
```

### Bot Configuration (appsettings.json)
```json
{
  "ConnectionNameGraph": "oauthbotsetting",
  "ConnectionNameTickets": "ticketsoauth",
  "TicketApi": {
    "BaseUrl": "https://saaliticketsapiclean.azurewebsites.net/",
    "AuthType": "AzureAD"
  }
}
```

### API Configuration (Program.cs)
```csharp
options.TokenValidationParameters.ValidAudiences = new[]
{
    "89155d3a-359d-4603-b821-0504395e331f",
    "api://89155d3a-359d-4603-b821-0504395e331f",
    "api://botid-89155d3a-359d-4603-b821-0504395e331f" // Critical!
};
```

---

## 🔍 Troubleshooting Quick Checks

### 401 Unauthorized
1. Copy token from logs
2. Paste into https://jwt.ms
3. Check `aud` claim matches API ValidAudiences
4. Redeploy API if ValidAudiences missing `api://botid-` prefix

### OAuth Doesn't Appear
1. Check `ConnectionNameTickets` in appsettings.json
2. Verify connection name is exactly `ticketsoauth`
3. Test connection in Azure Portal

### Wrong Audience
1. Check Token Exchange URL in OAuth connection
2. Verify scopes include full URI (not just scope name)
3. Check bot uses `_ticketsConnection` for ticket actions

---

## 📋 Token Inspection Template

Visit https://jwt.ms and paste your token. Verify:

```json
{
  "aud": "api://botid-89155d3a-359d-4603-b821-0504395e331f",
  "iss": "https://sts.windows.net/b22f8675-8375-455b-941a-67bee4cf7747/",
  "scp": "Tickets.ReadWrite access_as_user",
  "name": "John Doe",
  "upn": "john.doe@contoso.com",
  "exp": 1729000000,
  "nbf": 1728996400,
  "iat": 1728996400
}
```

---

## 🚀 Deployment Commands

### Build and Deploy API
```powershell
cd SupportTicketsApi
Remove-Item -Recurse -Force ./publish -ErrorAction SilentlyContinue
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force

az webapp deployment source config-zip `
  --resource-group M365AgentTeamsSSO-rg `
  --name SaaliTicketsApiClean `
  --src ./deploy.zip
```

### Verify API Deployment
```powershell
curl https://saaliticketsapiclean.azurewebsites.net/health
# Expected: {"status":"OK","time":"..."}
```

### Start Bot Locally
```powershell
cd BotConversationSsoQuickstart
dotnet run
```

---

## 🔑 Key Values Reference

| Item | Value |
|------|-------|
| **Tenant ID** | `b22f8675-8375-455b-941a-67bee4cf7747` |
| **Bot App ID** | `89155d3a-359d-4603-b821-0504395e331f` |
| **Application ID URI** | `api://botid-89155d3a-359d-4603-b821-0504395e331f` |
| **API Scope** | `api://botid-89155d3a-359d-4603-b821-0504395e331f/Tickets.ReadWrite` |
| **API URL** | `https://saaliticketsapiclean.azurewebsites.net` |
| **Resource Group** | `M365AgentTeamsSSO-rg` |

---

## ❓ Common Questions

### **Q: Why is MicrosoftAppPassword still required if we use user-delegated tokens?**

**A:** There are **two separate authentication layers**:

**1. Bot Infrastructure Authentication** (`MicrosoftAppPassword`)
- **Purpose:** Bot ↔ Azure Bot Service communication
- **Used for:** 
  - Receiving messages from Teams
  - Sending responses back to users
  - Retrieving user OAuth tokens from Bot Service
- **Cannot be removed** - Bot Framework requires it

**2. User OAuth Authentication** (User-delegated tokens)
- **Purpose:** User identity for API calls
- **Used for:** API authorization on behalf of the user
- **Obtained:** Through OAuth consent flow

**Both are required** - the client secret authenticates the bot application to Azure infrastructure, while user tokens authenticate the user for API access.

**Simple Analogy:**
- **Client secret** = Hotel staff badge (lets you work at the hotel)
- **User token** = Guest room key (grants access to specific rooms)

You need the staff badge to retrieve the guest's room key from the front desk!

---

**For detailed explanations, see: IMPLEMENTATION_GUIDE.md**

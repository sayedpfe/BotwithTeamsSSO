# Teams Bot with SSO and Support Tickets Integration

This Teams bot demonstrates advanced Single Sign-On (SSO) authentication integrated with Microsoft Graph and a protected Support Tickets API. Users can authenticate with their Teams/Azure AD credentials, access their profile information, and create support tickets that are stored securely via an authenticated REST API.

This bot has been created using [Bot Framework](https://dev.botframework.com) and extends the basic SSO functionality to include:
- Real user identification from Teams context
- Integration with a protected Support Tickets API using JWT authentication
- Enhanced user experience with personalized ticket creation
- Comprehensive error handling and token management

## Key Features

### Authentication & Security
* **Teams SSO (Single Sign-On)** - Users authenticate with their Teams/Azure AD credentials
* **JWT Client Credentials** - Bot authenticates with APIs using client credentials flow
* **Microsoft Graph Integration** - Access user profiles, emails, and Microsoft 365 data
* **Token Management** - Automatic token refresh and secure storage
* **Logout Support** - Clean session termination

### Support Ticket Functionality
* **Real User Identification** - Extracts actual Teams user names instead of generic placeholders
* **Authenticated API Calls** - Secure communication with Support Tickets API
* **Personalized Ticket Creation** - Support requests titled with actual user names
* **Persistent Storage** - Tickets stored securely via REST API

### Enhanced User Experience
* **Adaptive Cards** - Modern, interactive UI components
* **Contextual Responses** - Personalized greetings and confirmations
* **Error Handling** - Graceful degradation for authentication failures
* **Status Updates** - Real-time feedback for ticket creation

## Architecture Overview

```
Teams User → Teams Bot → Azure AD SSO → Microsoft Graph
              ↓
         JWT Client Credentials → Support Tickets API → File Storage
```

### Authentication Flow
1. User initiates conversation in Teams
2. Bot prompts for Azure AD authentication (if not already authenticated)
3. User completes OAuth flow and receives access token
4. Bot can now access Microsoft Graph data
5. For API calls, bot obtains JWT client credentials token
6. Bot makes authenticated requests to Support Tickets API

## Included Features
* Teams SSO (bots)
* Microsoft Graph API integration
* JWT client credentials authentication
* Support ticket creation and management
* Real user identification from Teams context
* Adaptive Card UI
* Comprehensive error handling

## Interaction with App

The bot supports the following interactions:

1. **Initial Authentication**: User signs in with Teams/Azure AD credentials
2. **Profile Access**: Bot displays user information from Microsoft Graph
3. **Ticket Creation**: User can create support tickets with their real name
4. **Status Updates**: Bot provides confirmation of ticket creation
5. **Logout**: Users can sign out and clear authentication

## Prerequisites

- Microsoft Teams account (not a guest account)
- [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows) or [ngrok](https://ngrok.com/download) for local development
- [M365 developer account](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/build-and-test/prepare-your-o365-tenant) or Teams account with app installation permissions
- Azure subscription for app registration and deployment

## Quick Start with Microsoft 365 Agents Toolkit

The simplest way to run this sample in Teams is to use Microsoft 365 Agents Toolkit for Visual Studio.

1. Install Visual Studio 2022 **Version 17.14 or higher**
2. Install [Microsoft 365 Agents Toolkit extension](https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/toolkit-v4/install-teams-toolkit-vs?pivots=visual-studio-v17-7)
3. In Visual Studio debug dropdown, select Dev Tunnels > Create A Tunnel (set authentication type to Public)
4. Right-click the 'M365Agent' project and select **Microsoft 365 Agents Toolkit > Select Microsoft 365 Account**
5. Sign in with a **Microsoft 365 work or school account**
6. Set `Startup Item` as `Microsoft Teams (browser)`
7. Press F5 to start debugging
8. In the opened browser, select Add button to install the app in Teams

## Manual Setup

### 1. Azure App Registration Setup

Create an Azure AD app registration with the following configuration:

**App Registration Details:**
- **Application ID**: `89155d3a-359d-4603-b821-0504395e331f` (or create your own)
- **Supported account types**: Accounts in any organizational directory (Multitenant)

**API Permissions Required:**
- `User.Read` (Microsoft Graph) - Delegated
- `email` (OpenID) - Delegated  
- `openid` (OpenID) - Delegated
- `profile` (OpenID) - Delegated

**Authentication Configuration:**
- **Redirect URIs**: Add your bot endpoint (e.g., `https://your-bot-url.com/api/messages`)
- **Client Secret**: Generate and securely store
- **Implicit grant**: Enable ID tokens

### 2. Bot Framework Registration

1. Create a new Bot Channels Registration in Azure
2. Set the messaging endpoint to your bot URL
3. Enable Microsoft Teams channel
4. Configure OAuth Connection Settings:
   - **Connection Name**: `BotConversationSsoQuickstart`
   - **Service Provider**: Azure Active Directory v2
   - **Client ID**: Your app registration ID
   - **Client Secret**: Your app registration secret
   - **Tenant ID**: Your Azure AD tenant ID
   - **Scopes**: `User.Read email openid profile`

### 3. Local Development Setup

1. **Install tunneling solution:**
   ```bash
   # Using ngrok
   ngrok http 3978 --host-header="localhost:3978"
   
   # OR using dev tunnels
   devtunnel host -p 3978 --allow-anonymous
   ```

2. **Clone and configure:**
   ```bash
   git clone <repository-url>
   cd BotConversationSsoQuickstart
   ```

3. **Update `appsettings.json`:**
   ```json
   {
     "MicrosoftAppType": "MultiTenant",
     "MicrosoftAppId": "89155d3a-359d-4603-b821-0504395e331f",
     "MicrosoftAppPassword": "<your-app-password>",
     "MicrosoftAppTenantId": "<your-tenant-id>",
     "ConnectionName": "BotConversationSsoQuickstart",
     "SupportTicketsApiBaseUrl": "https://your-api-url.azurewebsites.net"
   }
   ```

4. **Build and run:**
   ```bash
   dotnet build
   dotnet run
   ```

### 4. Support Tickets API Setup

Ensure the Support Tickets API is deployed and configured:

1. Deploy the `SupportTicketsApi` project to Azure App Service
2. Configure the API with the same app registration for JWT authentication
3. Update the `SupportTicketsApiBaseUrl` in bot settings
4. Verify API health at `https://your-api-url.azurewebsites.net/health`

### 5. Teams App Manifest

1. Navigate to `M365Agent/appPackage/`
2. Update `manifest.json` with your app registration ID
3. Create a zip file with manifest.json and icon files
4. Upload to Teams via Teams Toolkit or manually

## Project Structure

```
BotConversationSsoQuickstart/
├── Bots/
│   ├── DialogBot.cs           # Base dialog bot implementation
│   └── TeamsBot.cs           # Teams-specific bot logic
├── Controllers/
│   └── BotController.cs      # HTTP controller for bot messages
├── Dialogs/
│   ├── MainDialog.cs         # Primary conversation dialog
│   ├── LogoutDialog.cs       # Authentication logout dialog
│   └── GraphAction.cs        # Microsoft Graph integration
├── Services/
│   ├── SimpleGraphClient.cs  # Microsoft Graph API client
│   └── TicketApiClient.cs    # Support Tickets API client
├── AdapterWithErrorHandler.cs # Bot error handling
├── Program.cs                # Application startup
└── appsettings.json         # Configuration settings
```

## Key Components

### MainDialog.cs - Core Conversation Logic

**Purpose**: Manages the main conversation flow, user authentication, and ticket creation.

**Key Features**:
- OAuth prompt for Teams SSO authentication
- Real user identification: `step.Context.Activity.From.Name`
- Microsoft Graph integration for user profile data
- Support ticket creation with authenticated API calls
- Personalized responses with actual user names

**Key Methods**:
- `PromptStepAsync()`: Initiates OAuth authentication
- `LoginStepAsync()`: Processes authentication results
- `DisplayTokenPhase1Async()`: Shows user profile from Graph
- `ExecuteCreateTicketAsync()`: Creates support tickets with real user data

### TicketApiClient.cs - API Integration

**Purpose**: Handles authenticated communication with the Support Tickets API.

**Key Features**:
- JWT client credentials authentication
- Secure token acquisition using Microsoft.Identity.Client
- HTTP client configuration for API calls
- Error handling for API communication

**Authentication Scope**: `89155d3a-359d-4603-b821-0504395e331f/.default`

### TeamsBot.cs - Teams Integration

**Purpose**: Provides Teams-specific bot functionality and message handling.

**Key Features**:
- Teams activity handling
- Invoke activity forwarding for OAuth
- Teams-specific welcome messages
- Channel-specific behavior

## Configuration

### Required App Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `MicrosoftAppId` | Azure AD app registration ID | `89155d3a-359d-4603-b821-0504395e331f` |
| `MicrosoftAppPassword` | Client secret from app registration | `your-secret-value` |
| `MicrosoftAppTenantId` | Azure AD tenant ID | `your-tenant-id` |
| `ConnectionName` | Bot Framework OAuth connection name | `BotConversationSsoQuickstart` |
| `SupportTicketsApiBaseUrl` | Base URL for Support Tickets API | `https://your-api.azurewebsites.net` |

### Optional Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `MicrosoftAppType` | Application type | `MultiTenant` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development` |

## User Experience Flow

### 1. Initial Authentication
```
User → "Hello" → Bot → OAuth Card → User Signs In → Success Message
```

### 2. Profile Information
```
User → Authenticated → Bot → Graph API Call → Display User Profile
```

### 3. Ticket Creation
```
User → "Create ticket" → Bot → API Authentication → Create Ticket → Confirmation
```

### 4. Logout Process
```
User → "Logout" → Bot → Clear Tokens → Sign Out → Confirmation
```

## Testing

### Authentication Testing

Use the provided PowerShell script to test authentication:

```powershell
# Navigate to project directory
cd BotConversationSsoQuickstart

# Run authentication test
./testtoken.ps1
```

### Manual Testing

1. **Bot Framework Emulator**: Test basic bot functionality locally
2. **Teams Client**: Test full Teams integration with SSO
3. **API Testing**: Verify Support Tickets API connectivity
4. **Graph Integration**: Confirm Microsoft Graph data access

### Test Scenarios

- [ ] User can authenticate with Teams SSO
- [ ] Bot displays correct user profile information
- [ ] Support tickets are created with real user names
- [ ] API authentication works correctly
- [ ] Token refresh functions properly
- [ ] Logout clears authentication state
- [ ] Error handling works for failed authentication
- [ ] CORS works for cross-origin API requests

## Deployment

### Deploy to Azure

1. **Create Azure Resources:**
   - Azure Bot Service
   - Azure App Service (for API)
   - Application Insights (optional)

2. **Deploy Bot:**
   ```bash
   dotnet publish -c Release
   # Deploy published files to Azure App Service
   ```

3. **Configure Environment:**
   - Set production app settings in Azure
   - Configure OAuth connection in Bot Channels Registration
   - Update Teams app manifest with production URLs

### Environment Variables for Production

```bash
MicrosoftAppId=89155d3a-359d-4603-b821-0504395e331f
MicrosoftAppPassword=<production-secret>
MicrosoftAppTenantId=<tenant-id>
ConnectionName=BotConversationSsoQuickstart
SupportTicketsApiBaseUrl=https://production-api-url.azurewebsites.net
```

## Troubleshooting

### Common Issues

1. **Authentication Failures**
   - Verify app registration configuration
   - Check OAuth connection settings
   - Ensure correct redirect URIs

2. **Graph API Errors**
   - Confirm API permissions are granted
   - Verify admin consent for application permissions
   - Check token scopes

3. **API Connection Issues**
   - Validate Support Tickets API URL
   - Check API authentication configuration
   - Verify network connectivity

4. **Teams Integration Problems**
   - Ensure manifest is correctly configured
   - Check Teams app installation permissions
   - Verify bot endpoint accessibility

### Debug Tools

- **Bot Framework Emulator**: Local bot testing
- **Azure Portal**: Service monitoring and logs
- **Application Insights**: Detailed telemetry
- **Teams Developer Console**: Teams-specific debugging

### Logging

Enable detailed logging by uncommenting debug lines in `AdapterWithErrorHandler.cs`:

```csharp
// Uncomment for detailed error logging
// await context.SendActivityAsync("Sorry, it looks like something went wrong.");
```

## Security Considerations

### Authentication Security
- OAuth tokens stored securely in bot state
- JWT tokens have appropriate expiration times
- Client secrets stored in secure configuration
- HTTPS enforced for all communications

### Data Protection
- Minimal user data collection
- Secure API communication
- Proper token refresh handling
- User consent for data access

## Further Reading

- [Bot Framework Documentation](https://docs.botframework.com)
- [Teams Bot Development](https://docs.microsoft.com/en-us/microsoftteams/platform/bots/)
- [Azure Bot Service](https://docs.microsoft.com/en-us/azure/bot-service/)
- [Microsoft Graph API](https://docs.microsoft.com/en-us/graph/)
- [Teams SSO Documentation](https://docs.microsoft.com/en-us/microsoftteams/platform/tabs/how-to/authentication/auth-aad-sso)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -am 'Add new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Note**: This bot demonstrates enterprise-grade authentication patterns and API integration suitable for production Teams applications.

# M365 Agent - Teams App Configuration

This project contains the Microsoft Teams application manifest and configuration for the Teams Bot with SSO and Support Tickets integration. It defines how the bot appears and behaves within Microsoft Teams, including authentication, permissions, and user interface elements.

## Overview

The M365 Agent project configures a Teams bot that provides:
- Single Sign-On (SSO) authentication with Azure AD
- Microsoft Graph integration for user profile access
- Support ticket creation through authenticated API calls
- Seamless user experience within Microsoft Teams

## Project Structure

```
M365Agent/
├── appPackage/
│   ├── manifest.json           # Teams app manifest
│   ├── color.png              # App icon (color version)
│   ├── outline.png            # App icon (outline version)
│   └── build/
│       ├── appPackage.local.zip    # Development package
│       └── manifest.local.json     # Local development manifest
├── infra/
│   ├── azure.bicep            # Infrastructure as Code (Bicep)
│   └── azure.parameters.json  # Bicep parameters
├── build/
│   └── aad.manifest.local.json    # Azure AD app manifest
├── env/                       # Environment configuration files
├── aad.manifest.json          # Azure AD app registration manifest
├── m365agents.yml             # Teams Toolkit configuration
├── m365agents.local.yml       # Local development configuration
└── M365Agent.ttkproj          # Teams Toolkit project file
```

## Key Configuration Files

### manifest.json - Teams App Manifest

Defines the Teams application configuration including:

**App Information:**
- App ID and version
- Developer information
- App description and icons
- Privacy and terms of use URLs

**Bot Configuration:**
- Bot registration ID: `89155d3a-359d-4603-b821-0504395e331f`
- Supported scopes: `personal`, `team`, `groupchat`
- Command definitions and help text

**Permissions:**
- `identity` - Required for SSO authentication
- `messageTeamMembers` - For bot messaging capabilities

**Web Application Info:**
- SSO configuration for Azure AD authentication
- Resource URL for Microsoft Graph access

### aad.manifest.json - Azure AD App Registration

Configures the Azure AD application registration:

**API Permissions:**
- `User.Read` (Microsoft Graph) - Read user profile
- `email` - Access user email
- `openid` - OpenID Connect authentication
- `profile` - Access user profile information

**Authentication:**
- Redirect URIs for bot authentication
- Implicit grant flow configuration
- Required resource access definitions

### m365agents.yml - Teams Toolkit Configuration

Defines the development and deployment pipeline:

**Environment Configuration:**
- Local development settings
- Azure resource provisioning
- Deployment targets and parameters

**Build Configuration:**
- App package generation
- Manifest validation
- Asset compilation

## Authentication Configuration

### Single Sign-On (SSO) Setup

The Teams app is configured for SSO with the following components:

1. **Azure AD App Registration:**
   - Application ID: `89155d3a-359d-4603-b821-0504395e331f`
   - Multi-tenant support enabled
   - Required API permissions configured

2. **Teams Manifest SSO Section:**
   ```json
   "webApplicationInfo": {
     "id": "89155d3a-359d-4603-b821-0504395e331f",
     "resource": "https://graph.microsoft.com"
   }
   ```

3. **Valid Domains:**
   - `token.botframework.com` - Required for Bot Framework OAuth
   - Your bot domain (e.g., `your-bot.azurewebsites.net`)

### Required Permissions

The application requires the following permissions:

| Permission | Type | Purpose |
|------------|------|---------|
| `User.Read` | Delegated | Read user profile information |
| `email` | Delegated | Access user email address |
| `openid` | Delegated | OpenID Connect sign-in |
| `profile` | Delegated | Access user profile data |

## Bot Commands

The Teams app defines the following bot commands:

### `/login`
- **Description:** Sign in to access your profile and create support tickets
- **Scope:** Personal, Team, Group Chat
- **Usage:** Type `/login` to initiate authentication

### `/logout` 
- **Description:** Sign out and clear your authentication tokens
- **Scope:** Personal, Team, Group Chat
- **Usage:** Type `/logout` to terminate your session

### `/help`
- **Description:** Get help and information about available commands
- **Scope:** Personal, Team, Group Chat
- **Usage:** Type `/help` to see all available commands

### `/createticket`
- **Description:** Create a new support ticket (requires authentication)
- **Scope:** Personal, Team, Group Chat
- **Usage:** Type `/createticket` to create a support request

## Development Setup

### Prerequisites
- [Teams Toolkit for Visual Studio](https://learn.microsoft.com/en-us/microsoftteams/platform/toolkit/toolkit-v4/install-teams-toolkit-vs)
- Microsoft 365 Developer account
- Azure subscription
- Visual Studio 2022 version 17.14 or higher

### Local Development

1. **Open in Visual Studio:**
   Open the solution file and ensure the M365Agent project is loaded.

2. **Sign in to Microsoft 365:**
   Use Teams Toolkit to sign in with your Microsoft 365 developer account.

3. **Configure Environment:**
   Teams Toolkit will automatically configure local development settings.

4. **Start Debugging:**
   Press F5 or select "Microsoft Teams (browser)" as startup item.

5. **Install in Teams:**
   Teams will open in browser with option to install your app.

### Environment Configuration

The project supports multiple environments:

**Local Development (`m365agents.local.yml`):**
- Uses local tunneling (dev tunnels or ngrok)
- Development Azure AD app registration
- Local bot endpoint

**Production (`m365agents.yml`):**
- Azure-hosted bot service
- Production Azure AD app registration
- Production API endpoints

## Deployment

### Manual Deployment

1. **Update Manifest:**
   - Replace placeholder values with production settings
   - Update bot endpoint URLs
   - Verify Azure AD app registration ID

2. **Create App Package:**
   ```bash
   # Navigate to appPackage directory
   cd appPackage
   
   # Create zip file with manifest and icons
   zip -r ../teams-app.zip manifest.json color.png outline.png
   ```

3. **Upload to Teams:**
   - Go to Teams Admin Center or Teams App Studio
   - Upload the app package
   - Submit for approval if required

### Automated Deployment with Teams Toolkit

1. **Provision Resources:**
   Teams Toolkit can provision Azure resources automatically.

2. **Deploy App:**
   Use Teams Toolkit deploy command to publish the app.

3. **Publish to Store:**
   Submit to Teams App Store for organization-wide distribution.

## Infrastructure as Code

### Azure Bicep Template (`infra/azure.bicep`)

The Bicep template provisions:
- Azure Bot Service registration
- App Service for bot hosting
- Application Insights for monitoring
- Required IAM roles and permissions

### Parameters (`infra/azure.parameters.json`)

Configure deployment parameters:
```json
{
  "resourceBaseName": "teams-sso-bot",
  "webAppSku": "B1",
  "location": "East US",
  "aadAppClientId": "89155d3a-359d-4603-b821-0504395e331f"
}
```

## Security Configuration

### App Permissions

The Teams app requests minimal required permissions:
- **Identity**: For SSO authentication only
- **MessageTeamMembers**: For bot messaging capabilities
- No additional Teams data access beyond authentication

### Azure AD Configuration

**Supported Account Types:**
- Accounts in any organizational directory (Multi-tenant)
- Personal Microsoft accounts not supported (by design)

**Redirect URIs:**
- Bot Framework OAuth redirect: `https://token.botframework.com/.auth/web/redirect`
- Your bot endpoint: `https://your-bot-url.azurewebsites.net/auth`

**API Permissions:**
- Microsoft Graph permissions with admin consent
- Minimal scope principle applied

## Testing

### Local Testing

1. **Teams Toolkit Debugging:**
   Use F5 debugging in Visual Studio with Teams Toolkit.

2. **Bot Framework Emulator:**
   Test basic bot functionality locally.

3. **Manual Testing:**
   Install app in Teams development tenant.

### Test Scenarios

- [ ] App installs successfully in Teams
- [ ] SSO authentication works correctly
- [ ] Bot responds to commands
- [ ] User can create support tickets
- [ ] Logout clears authentication state
- [ ] Error handling works for failed authentication
- [ ] App works in personal, team, and group chat scopes

## Troubleshooting

### Common Issues

1. **App Installation Fails:**
   - Check manifest.json validation
   - Verify all required fields are populated
   - Ensure bot ID matches Azure registration

2. **SSO Not Working:**
   - Verify Azure AD app registration configuration
   - Check redirect URIs are correct
   - Ensure API permissions are granted with admin consent

3. **Bot Not Responding:**
   - Check bot endpoint accessibility
   - Verify messaging endpoint in Azure Bot Service
   - Check bot service health and logs

4. **Permission Errors:**
   - Verify Azure AD app permissions
   - Check admin consent status
   - Ensure user has appropriate licenses

### Debug Tools

- **Teams Toolkit Logs**: Check output window in Visual Studio
- **Azure Portal**: Monitor bot service and app registration
- **Teams Admin Center**: Check app status and permissions
- **Bot Framework Emulator**: Test bot logic locally

## Customization

### Branding

**App Icons:**
- `color.png`: 192x192 pixels, full-color app icon
- `outline.png`: 32x32 pixels, transparent outline icon

**App Information:**
Update `manifest.json` with your organization details:
```json
{
  "name": "Your Support Bot",
  "description": "Custom description for your organization",
  "developer": {
    "name": "Your Organization",
    "websiteUrl": "https://your-website.com",
    "privacyUrl": "https://your-website.com/privacy",
    "termsOfUseUrl": "https://your-website.com/terms"
  }
}
```

### Commands

Add custom bot commands in `manifest.json`:
```json
{
  "commandLists": [{
    "scopes": ["personal", "team", "groupchat"],
    "commands": [{
      "title": "Custom Command",
      "description": "Description of your custom command"
    }]
  }]
}
```

### Additional Scopes

Enable additional Teams capabilities:
- `team`: For team-specific functionality
- `groupchat`: For group chat features
- `meetings`: For meeting integration

## Best Practices

### Security
- Use minimal required permissions
- Implement proper error handling for authentication failures
- Regular security reviews of Azure AD configuration
- Monitor authentication logs and usage

### User Experience
- Provide clear command descriptions
- Implement helpful error messages
- Test across different Teams clients (desktop, web, mobile)
- Follow Teams design guidelines

### Deployment
- Use separate app registrations for development and production
- Implement proper CI/CD pipelines
- Monitor app performance and usage
- Plan for app updates and versioning

## Support

For issues with Teams app configuration:
- Check Teams Toolkit documentation
- Review Azure Bot Service troubleshooting guides
- Contact Microsoft support for platform issues
- Review this project's main README for architecture details

## Contributing

1. Follow Teams app manifest schema requirements
2. Test changes in multiple Teams environments
3. Update documentation for configuration changes
4. Ensure security best practices are maintained

---

**Note**: This Teams app configuration supports enterprise deployment with proper security and compliance considerations.
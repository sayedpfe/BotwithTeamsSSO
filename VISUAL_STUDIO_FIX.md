# Visual Studio Integration Fix - Teams Enterprise Support Hub

## Problem Identified
Visual Studio was not calling the local Support Tickets API after recent changes to the M365Agent configuration.

## Root Cause Analysis
After comparing the current configuration with the working version 1.2.0, we discovered:

### Version 1.2.0 (Working)
```json
{
  "AuthType": "AzureAD"
}
```

### Current Version (Broken)
```json
{
  "AuthType": "None"
}
```

## Key Differences Found

### 1. Authentication Type
- **v1.2.0**: Used `AuthType: "AzureAD"` 
- **Current**: Was using `AuthType: "None"`

### 2. M365Agent Configuration
- **v1.2.0**: Simpler configuration without custom TicketApi variables
- **Current**: Added custom TicketApi configuration that needed proper environment variable setup

## Solution Applied

### 1. Updated Environment Variables
**File**: `M365Agent/env/.env.local`
```
TICKET_API_BASE_URL=https://localhost:7266
TICKET_API_AUTH_TYPE=AzureAD  # Changed from "None" to "AzureAD"
```

### 2. Updated Application Settings
**File**: `BotConversationSsoQuickstart/appsettings.json`
```json
{
  "TicketApi": {
    "BaseUrl": "https://localhost:7266/",
    "AuthType": "AzureAD"  // Changed from "None"
  }
}
```

### 3. M365Agent Configuration
**File**: `M365Agent/m365agents.local.yml`
- Maintained the TicketApi configuration block
- Ensured environment variables are properly referenced

## Testing Instructions

### Before Testing
1. Ensure Support Tickets API is running on `https://localhost:7266`
2. Verify appsettings.json has correct configuration (run `.\check-m365-config.ps1`)

### Test in Visual Studio
1. Open the solution in Visual Studio
2. Set `M365Agent` as the startup project
3. Press F5 to run
4. Test creating/viewing support tickets through the bot
5. Check console logs for API calls to localhost:7266

### Verification
- The bot should now successfully call the local Support Tickets API
- Authentication tokens should be properly handled with AzureAD
- API logging should show detailed request/response information

## Diagnostic Tools

### Configuration Checker
Run the diagnostic script to verify configuration:
```powershell
.\check-m365-config.ps1
```

This script will:
- ✅ Verify appsettings.json has TicketApi configuration
- ✅ Check environment variables are set
- ✅ Validate M365Agent yml configuration
- 💡 Provide solutions if issues are found

## Prevention Measures

1. **Always check appsettings.json** after running from Visual Studio
2. **Use the diagnostic script** to verify configuration
3. **Commit working configurations** to prevent regression
4. **Test both AuthType settings** when making authentication changes

## Related Files Modified
- `M365Agent/env/.env.local` - Updated TICKET_API_AUTH_TYPE
- `BotConversationSsoQuickstart/appsettings.json` - Updated AuthType and BaseUrl
- `check-m365-config.ps1` - Diagnostic script for configuration verification

## Status
✅ **RESOLVED** - Visual Studio should now properly call the local Support Tickets API with AzureAD authentication.
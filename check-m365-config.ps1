# M365Agent Configuration Checker for Teams Enterprise Support Hub
# This script helps diagnose configuration issues when running from Visual Studio

Write-Host "🔍 M365Agent Configuration Checker" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Check if running from correct directory
$currentPath = Get-Location
Write-Host "📍 Current Path: $currentPath" -ForegroundColor White

# Check if appsettings.json exists and has correct content
$appsettingsPath = "BotConversationSsoQuickstart\appsettings.json"
if (Test-Path $appsettingsPath) {
    Write-Host "✅ appsettings.json found" -ForegroundColor Green
    
    try {
        $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
        
        # Check required properties
        Write-Host "`n🔍 Configuration Check:" -ForegroundColor Cyan
        
        if ($appsettings.MicrosoftAppId) {
            Write-Host "  ✅ MicrosoftAppId: $($appsettings.MicrosoftAppId.Substring(0,8))..." -ForegroundColor Green
        } else {
            Write-Host "  ❌ MicrosoftAppId: Missing" -ForegroundColor Red
        }
        
        if ($appsettings.TicketApi) {
            Write-Host "  ✅ TicketApi Configuration:" -ForegroundColor Green
            Write-Host "     BaseUrl: $($appsettings.TicketApi.BaseUrl)" -ForegroundColor White
            Write-Host "     AuthType: $($appsettings.TicketApi.AuthType)" -ForegroundColor White
        } else {
            Write-Host "  ❌ TicketApi: Missing - This is the problem!" -ForegroundColor Red
            Write-Host "     The M365Agent may have overwritten your appsettings.json" -ForegroundColor Yellow
        }
        
        if ($appsettings.ConnectionName) {
            Write-Host "  ✅ ConnectionName: $($appsettings.ConnectionName)" -ForegroundColor Green
        } else {
            Write-Host "  ❌ ConnectionName: Missing" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "❌ Error reading appsettings.json: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "❌ appsettings.json not found at $appsettingsPath" -ForegroundColor Red
}

# Check M365Agent environment files
Write-Host "`n🔍 M365Agent Environment Check:" -ForegroundColor Cyan

$envPath = "M365Agent\env\.env.local"
if (Test-Path $envPath) {
    Write-Host "✅ .env.local found" -ForegroundColor Green
    
    $envContent = Get-Content $envPath -Raw
    if ($envContent -match "TICKET_API_BASE_URL") {
        Write-Host "  ✅ TICKET_API_BASE_URL configured" -ForegroundColor Green
    } else {
        Write-Host "  ❌ TICKET_API_BASE_URL missing" -ForegroundColor Red
    }
    
    if ($envContent -match "TICKET_API_AUTH_TYPE") {
        Write-Host "  ✅ TICKET_API_AUTH_TYPE configured" -ForegroundColor Green
    } else {
        Write-Host "  ❌ TICKET_API_AUTH_TYPE missing" -ForegroundColor Red
    }
} else {
    Write-Host "❌ .env.local not found at $envPath" -ForegroundColor Red
}

$ymlPath = "M365Agent\m365agents.local.yml"
if (Test-Path $ymlPath) {
    Write-Host "✅ m365agents.local.yml found" -ForegroundColor Green
    
    $ymlContent = Get-Content $ymlPath -Raw
    if ($ymlContent -match "TicketApi:") {
        Write-Host "  ✅ TicketApi configuration in yml" -ForegroundColor Green
    } else {
        Write-Host "  ❌ TicketApi configuration missing from yml" -ForegroundColor Red
    }
} else {
    Write-Host "❌ m365agents.local.yml not found at $ymlPath" -ForegroundColor Red
}

Write-Host "`n💡 Solutions:" -ForegroundColor Yellow
Write-Host "1. If TicketApi is missing from appsettings.json:" -ForegroundColor White
Write-Host "   - The M365Agent has overwritten your configuration" -ForegroundColor White
Write-Host "   - Run this to restore it:" -ForegroundColor White
Write-Host '   $config = @"' -ForegroundColor Gray
Write-Host '{' -ForegroundColor Gray
Write-Host '  "MicrosoftAppId": "89155d3a-359d-4603-b821-0504395e331f",' -ForegroundColor Gray
Write-Host '  "MicrosoftAppPassword": "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI",' -ForegroundColor Gray
Write-Host '  "ConnectionName": "oauthbotsetting",' -ForegroundColor Gray
Write-Host '  "ConnectionNameGraph": "oauthbotsetting",' -ForegroundColor Gray
Write-Host '  "ConnectionNameTickets": "ticketsoauth",' -ForegroundColor Gray
Write-Host '  "MicrosoftAppType": "SingleTenant",' -ForegroundColor Gray
Write-Host '  "MicrosoftAppTenantId": "b22f8675-8375-455b-941a-67bee4cf7747",' -ForegroundColor Gray
Write-Host '  "TicketApi": {' -ForegroundColor Gray
Write-Host '    "BaseUrl": "https://saaliticketsapiclean.azurewebsites.net/",' -ForegroundColor Gray
Write-Host '    "AuthType": "None"' -ForegroundColor Gray
Write-Host '  }' -ForegroundColor Gray
Write-Host '}' -ForegroundColor Gray
Write-Host '"@' -ForegroundColor Gray
Write-Host '   $config | Out-File -FilePath "BotConversationSsoQuickstart\appsettings.json" -Encoding UTF8' -ForegroundColor Gray

Write-Host "`n2. To prevent M365Agent from overwriting:" -ForegroundColor White
Write-Host "   - Use the updated M365Agent configuration files we just fixed" -ForegroundColor White
Write-Host "   - Always check appsettings.json after running from Visual Studio" -ForegroundColor White

Write-Host "`n3. Alternative - Run directly:" -ForegroundColor White
Write-Host "   cd BotConversationSsoQuickstart" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray

Write-Host "`n✅ Configuration check complete!" -ForegroundColor Green
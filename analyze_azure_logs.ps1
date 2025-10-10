# Azure App Service Log Analysis Script
$appName = "SaaliTicketsApiClean"
$resourceGroup = "M365AgentTeamsSSO-rg"

Write-Host "📋 Azure App Service Log Analysis" -ForegroundColor Cyan
Write-Host "=" * 40 -ForegroundColor Cyan

# Get recent logs
Write-Host "`n📄 Fetching recent application logs..." -ForegroundColor Yellow
try {
    az webapp log download --resource-group $resourceGroup --name $appName --log-file "app_logs.zip"
    Write-Host "✅ Logs downloaded to app_logs.zip" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to download logs: $($_.Exception.Message)" -ForegroundColor Red
}

# Get live logs (last 20 lines)
Write-Host "`n📺 Fetching live logs..." -ForegroundColor Yellow
try {
    $liveLogJob = Start-Job -ScriptBlock {
        az webapp log tail --resource-group $using:resourceGroup --name $using:appName | Select-Object -First 20
    }
    
    # Wait for 10 seconds then get the results
    Wait-Job $liveLogJob -Timeout 10
    $liveLog = Receive-Job $liveLogJob
    
    if ($liveLog) {
        Write-Host "📄 Recent log entries:" -ForegroundColor Cyan
        $liveLog | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
    } else {
        Write-Host "ℹ️  No recent log entries found" -ForegroundColor Yellow
    }
    
    Remove-Job $liveLogJob -Force
}
catch {
    Write-Host "❌ Failed to get live logs: $($_.Exception.Message)" -ForegroundColor Red
}

# Check app settings
Write-Host "`n⚙️  Checking app settings..." -ForegroundColor Yellow
try {
    $settings = az webapp config appsettings list --resource-group $resourceGroup --name $appName | ConvertFrom-Json
    
    Write-Host "📋 Current app settings:" -ForegroundColor Cyan
    $settings | ForEach-Object {
        if ($_.name -like "*Storage*" -or $_.name -like "*Auth*" -or $_.name -like "*Audience*" -or $_.name -like "*JWT*") {
            Write-Host "   $($_.name): $($_.value)" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "❌ Failed to get app settings: $($_.Exception.Message)" -ForegroundColor Red
}

# Check connection strings
Write-Host "`n🔗 Checking connection strings..." -ForegroundColor Yellow
try {
    $connectionStrings = az webapp config connection-string list --resource-group $resourceGroup --name $appName | ConvertFrom-Json
    
    if ($connectionStrings.Count -gt 0) {
        Write-Host "📋 Connection strings found:" -ForegroundColor Cyan
        $connectionStrings | ForEach-Object {
            Write-Host "   $($_.name): [HIDDEN]" -ForegroundColor Gray
        }
    } else {
        Write-Host "ℹ️  No connection strings configured" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Failed to get connection strings: $($_.Exception.Message)" -ForegroundColor Red
}

# Test simple health endpoint with detailed output
Write-Host "`n🏥 Testing health endpoint with detailed response..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://$appName.azurewebsites.net/health" -UseBasicParsing -Verbose
    Write-Host "✅ Health check response:" -ForegroundColor Green
    Write-Host "   Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "   Headers: $($response.Headers | ConvertTo-Json -Depth 1)" -ForegroundColor Gray
    Write-Host "   Content: $($response.Content)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔍 Analysis complete!" -ForegroundColor Green
Write-Host "`nℹ️  If the health endpoint works but API endpoints fail, the issue is likely:" -ForegroundColor Yellow
Write-Host "   1. Missing environment variables for Azure Table Storage" -ForegroundColor Gray
Write-Host "   2. JWT authentication configuration issues" -ForegroundColor Gray
Write-Host "   3. Missing dependencies in the deployed package" -ForegroundColor Gray
# Enhanced API Testing Script - Comprehensive endpoint testing without breaking auth config
$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"
$apiBaseUrl = "https://saaliticketsapiclean.azurewebsites.net"

Write-Host "🔧 Enhanced API Testing Script" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan

# Function to test API endpoint
function Test-ApiEndpoint {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [string]$Description
    )
    
    Write-Host "`n🔍 Testing: $Description" -ForegroundColor Yellow
    Write-Host "   URL: $Method $Url" -ForegroundColor Gray
    
    try {
        $splat = @{
            Uri = $Url
            Method = $Method
            UseBasicParsing = $true
        }
        
        if ($Headers.Count -gt 0) {
            $splat.Headers = $Headers
            Write-Host "   Headers: Authorization Bearer token included" -ForegroundColor Gray
        } else {
            Write-Host "   Headers: No authentication" -ForegroundColor Gray
        }
        
        if ($Body) {
            $splat.Body = $Body
            $splat.ContentType = "application/json"
            Write-Host "   Body: $Body" -ForegroundColor Gray
        }
        
        $response = Invoke-WebRequest @splat
        Write-Host "   ✅ SUCCESS: $($response.StatusCode) $($response.StatusDescription)" -ForegroundColor Green
        
        if ($response.Content) {
            $content = $response.Content
            if ($content.Length -gt 200) {
                $content = $content.Substring(0, 200) + "..."
            }
            Write-Host "   📄 Response: $content" -ForegroundColor Cyan
        }
        
        return @{ Success = $true; StatusCode = $response.StatusCode; Content = $response.Content }
    }
    catch {
        Write-Host "   ❌ FAILED: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            Write-Host "   📊 Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        }
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# Test 1: Health endpoint (should always work)
Write-Host "`n🏥 PHASE 1: Testing Health Endpoints" -ForegroundColor Magenta
Test-ApiEndpoint -Url "$apiBaseUrl/health" -Description "Health check (no auth)"

# Test 2: Get working token
Write-Host "`n🔐 PHASE 2: Token Acquisition" -ForegroundColor Magenta
$workingToken = $null
$body = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $tokenResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
    $workingToken = $tokenResponse.access_token
    Write-Host "✅ Token acquired successfully (Length: $($workingToken.Length))" -ForegroundColor Green
}
catch {
    Write-Host "❌ Token acquisition failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: API endpoints without authentication
Write-Host "`n🔓 PHASE 3: Testing API Endpoints WITHOUT Authentication" -ForegroundColor Magenta
Test-ApiEndpoint -Url "$apiBaseUrl/api/tickets" -Description "Get tickets (no auth)"
Test-ApiEndpoint -Url "$apiBaseUrl/api/feedback" -Description "Get feedback (no auth)"

# Test 4: API endpoints with authentication (if token available)
if ($workingToken) {
    Write-Host "`n🔒 PHASE 4: Testing API Endpoints WITH Authentication" -ForegroundColor Magenta
    $authHeaders = @{ "Authorization" = "Bearer $workingToken" }
    
    Test-ApiEndpoint -Url "$apiBaseUrl/api/tickets" -Headers $authHeaders -Description "Get tickets (with auth)"
    Test-ApiEndpoint -Url "$apiBaseUrl/api/feedback" -Headers $authHeaders -Description "Get feedback (with auth)"
    
    # Test POST endpoints with auth
    $sampleTicket = @{
        title = "Test Ticket from PowerShell"
        description = "This is a test ticket created via PowerShell script for troubleshooting"
    } | ConvertTo-Json
    
    Test-ApiEndpoint -Url "$apiBaseUrl/api/tickets" -Method "POST" -Headers $authHeaders -Body $sampleTicket -Description "Create ticket (with auth)"
    
    $sampleFeedback = @{
        userId = "test-user-ps"
        userName = "PowerShell Test User"
        conversationId = "test-conversation-ps"
        activityId = "test-activity-ps"
        botResponse = "This is a test bot response"
        reaction = "like"
        comment = "Test feedback from PowerShell script"
        category = "Testing"
    } | ConvertTo-Json
    
    Test-ApiEndpoint -Url "$apiBaseUrl/api/feedback" -Method "POST" -Headers $authHeaders -Body $sampleFeedback -Description "Create feedback (with auth)"
}

# Test 5: Alternative authentication methods
Write-Host "`n🔄 PHASE 5: Testing Alternative Authentication Methods" -ForegroundColor Magenta

# Try Graph token
Write-Host "`n📈 Trying Microsoft Graph token..." -ForegroundColor Cyan
$graphBody = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "https://graph.microsoft.com/.default"
}

try {
    $graphTokenResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $graphBody
    $graphToken = $graphTokenResponse.access_token
    Write-Host "✅ Graph token acquired (Length: $($graphToken.Length))" -ForegroundColor Green
    
    $graphHeaders = @{ "Authorization" = "Bearer $graphToken" }
    Test-ApiEndpoint -Url "$apiBaseUrl/api/tickets" -Headers $graphHeaders -Description "Get tickets (Graph token)"
}
catch {
    Write-Host "❌ Graph token failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 6: Azure App Service diagnostics endpoints
Write-Host "`n🩺 PHASE 6: Testing Azure Diagnostics Endpoints" -ForegroundColor Magenta
Test-ApiEndpoint -Url "$apiBaseUrl/diag/file/raw" -Description "Diagnostic file endpoint"

# Test 7: Detailed API configuration check
Write-Host "`n⚙️  PHASE 7: API Configuration Analysis" -ForegroundColor Magenta
Write-Host "📋 Current bot configuration:" -ForegroundColor Yellow
Write-Host "   App ID: $appId" -ForegroundColor Gray
Write-Host "   Tenant ID: $tenantId" -ForegroundColor Gray
Write-Host "   API Base URL: $apiBaseUrl" -ForegroundColor Gray

# Check what the Azure app settings reveal about auth configuration
Write-Host "`n🔍 Checking Azure app configuration..." -ForegroundColor Yellow
try {
    $azConfig = az webapp config appsettings list --resource-group "M365AgentTeamsSSO-rg" --name "SaaliTicketsApiClean" --output json | ConvertFrom-Json
    $authSettings = $azConfig | Where-Object { $_.name -like "*auth*" -or $_.name -like "*Auth*" -or $_.name -like "*JWT*" -or $_.name -like "*jwt*" }
    
    if ($authSettings) {
        Write-Host "🔧 Authentication-related settings found:" -ForegroundColor Cyan
        $authSettings | ForEach-Object {
            $value = if ($_.value.Length -gt 50) { $_.value.Substring(0, 50) + "..." } else { $_.value }
            Write-Host "   $($_.name): $value" -ForegroundColor Gray
        }
    } else {
        Write-Host "❌ No authentication settings found in Azure configuration" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Could not retrieve Azure app settings: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host "`n📊 SUMMARY & RECOMMENDATIONS" -ForegroundColor Magenta
Write-Host "=" * 50 -ForegroundColor Magenta

if ($workingToken) {
    Write-Host "✅ Token acquisition: WORKING" -ForegroundColor Green
    Write-Host "   Recommended bot config: AuthType = 'AzureAD'" -ForegroundColor Green
} else {
    Write-Host "❌ Token acquisition: FAILED" -ForegroundColor Red
    Write-Host "   Recommended bot config: AuthType = 'None'" -ForegroundColor Yellow
}

Write-Host "`n🎯 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Review the test results above" -ForegroundColor Gray
Write-Host "2. If any endpoint works, note the authentication method" -ForegroundColor Gray
Write-Host "3. Update bot appsettings.json AuthType accordingly" -ForegroundColor Gray
Write-Host "4. If all fail, check Azure API logs for more details" -ForegroundColor Gray

Write-Host "`n🔧 Testing completed!" -ForegroundColor Cyan
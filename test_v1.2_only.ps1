# Test only the tickets endpoint that existed in v1.2
$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

Write-Host "🧪 Testing v1.2 Original Functionality (Tickets Only)" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan

# Get token using the same method as v1.2
Write-Host "`n🎫 Acquiring token..." -ForegroundColor Yellow
$body = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $response = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
    $token = $response.access_token
    Write-Host "✅ Token acquired successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Token acquisition failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$headers = @{ 
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test health first
Write-Host "`n🏥 Testing health endpoint..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/health"
    Write-Host "✅ Health: OK" -ForegroundColor Green
} catch {
    Write-Host "❌ Health failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test unauthorized access to tickets
Write-Host "`n🔒 Testing tickets without authentication..." -ForegroundColor Yellow
try {
    $ticketsResponse = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets"
    Write-Host "❌ ERROR: Should require authentication!" -ForegroundColor Red
} catch {
    $statusCode = $_.Exception.Response.StatusCode
    if ($statusCode -eq "Unauthorized") {
        Write-Host "✅ Correctly requires authentication (401)" -ForegroundColor Green
    } else {
        Write-Host "❌ Wrong error: $statusCode" -ForegroundColor Red
    }
}

# Test authenticated access to tickets (v1.2 functionality)
Write-Host "`n🎫 Testing GET /api/tickets with authentication..." -ForegroundColor Yellow
try {
    $ticketsResponse = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Headers $headers
    Write-Host "✅ GET tickets: SUCCESS" -ForegroundColor Green
    Write-Host "   Found $($ticketsResponse.Count) tickets" -ForegroundColor Gray
    if ($ticketsResponse.Count -gt 0) {
        Write-Host "   Sample ticket: $($ticketsResponse[0].Title)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ GET tickets failed: $($_.Exception.Message)" -ForegroundColor Red
    $statusCode = $_.Exception.Response.StatusCode
    Write-Host "   Status: $statusCode" -ForegroundColor Red
    
    # If this fails, the basic v1.2 functionality is broken
    Write-Host "`n🔍 This suggests the issue is NOT with feedback additions" -ForegroundColor Yellow
    Write-Host "   The core v1.2 tickets functionality is broken" -ForegroundColor Gray
}

Write-Host "`n📋 Analysis:" -ForegroundColor Yellow
Write-Host "- If tickets work, the v1.2 base is OK and we can add feedback safely" -ForegroundColor Gray
Write-Host "- If tickets fail, there's a deeper issue with the deployment or configuration" -ForegroundColor Gray
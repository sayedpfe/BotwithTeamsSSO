# Test script to verify token acquisition for the bot app registration
$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

Write-Host "Testing token acquisition for bot app registration..." -ForegroundColor Yellow

# Test 1: Try to get token for the app itself
Write-Host "Test 1: Getting token for api://$appId/.default" -ForegroundColor Cyan
$body1 = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "api://$appId/.default"
}

try {
    $response1 = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body1
    Write-Host "✅ Success: Token acquired for api://$appId/.default" -ForegroundColor Green
    Write-Host "Token length: $($response1.access_token.Length)" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
}

# Test 2: Try alternative scope format
Write-Host "`nTest 2: Getting token for $appId/.default" -ForegroundColor Cyan
$body2 = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $response2 = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body2
    Write-Host "✅ Success: Token acquired for $appId/.default" -ForegroundColor Green
    Write-Host "Token length: $($response2.access_token.Length)" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Try Graph scope (what currently works)
Write-Host "`nTest 3: Getting token for https://graph.microsoft.com/.default" -ForegroundColor Cyan
$body3 = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "https://graph.microsoft.com/.default"
}

try {
    $response3 = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body3
    Write-Host "✅ Success: Token acquired for https://graph.microsoft.com/.default" -ForegroundColor Green
    Write-Host "Token length: $($response3.access_token.Length)" -ForegroundColor Green
    
    # Test this token against the API
    Write-Host "`nTesting Graph token against API..." -ForegroundColor Magenta
    $headers = @{ "Authorization" = "Bearer $($response3.access_token)" }
    try {
        $apiResponse = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Headers $headers
        Write-Host "✅ API call succeeded with Graph token!" -ForegroundColor Green
    } catch {
        Write-Host "❌ API call failed: $($_.Exception.Message)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nDone testing token acquisition." -ForegroundColor Yellow
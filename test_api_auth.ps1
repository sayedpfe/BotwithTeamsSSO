# Test the working token against the API
$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

Write-Host "Getting token with working scope..." -ForegroundColor Yellow

$body = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $tokenResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
    $token = $tokenResponse.access_token
    Write-Host "✅ Token acquired successfully" -ForegroundColor Green
    
    # Test API calls with this token
    $headers = @{ "Authorization" = "Bearer $token" }
    
    Write-Host "`nTesting API endpoints..." -ForegroundColor Cyan
    
    # Test 1: List tickets
    Write-Host "Test 1: GET /api/tickets" -ForegroundColor White
    try {
        $tickets = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Headers $headers
        Write-Host "✅ List tickets successful. Found $($tickets.Count) tickets" -ForegroundColor Green
    } catch {
        Write-Host "❌ List tickets failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Response details: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
    
    # Test 2: Create ticket
    Write-Host "`nTest 2: POST /api/tickets" -ForegroundColor White
    $ticketData = @{
        title = "Test Ticket from Auth Test"
        description = "This ticket was created to test the authenticated API"
    } | ConvertTo-Json
    
    try {
        $newTicket = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Method POST -Headers $headers -Body $ticketData -ContentType "application/json"
        Write-Host "✅ Create ticket successful. Ticket ID: $($newTicket.id)" -ForegroundColor Green
    } catch {
        Write-Host "❌ Create ticket failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Response details: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Failed to get token: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nAPI authentication test complete!" -ForegroundColor Yellow
# Test complete feedback system functionality
Write-Host "Testing Complete Feedback System..." -ForegroundColor Green

$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

# Get token
$body = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $response = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
    Write-Host "✅ Token acquired" -ForegroundColor Green
    
    $headers = @{ 
        "Authorization" = "Bearer $($response.access_token)"
        "Content-Type" = "application/json"
    }
    
    # Test endpoints
    Write-Host "`nTesting endpoints:" -ForegroundColor Yellow
    
    # Health check
    $health = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/health"
    Write-Host "✅ Health: $($health.status)" -ForegroundColor Green
    
    # Authentication
    $auth = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/test/auth" -Headers $headers
    Write-Host "✅ Auth: $($auth.authenticated)" -ForegroundColor Green
    
    # Tickets
    $tickets = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Headers $headers
    Write-Host "✅ Tickets: $($tickets.Count) found" -ForegroundColor Green
    
    # Create test ticket
    $ticketData = @{
        title = "Test Feedback System"
        description = "Testing complete feedback system integration"
    } | ConvertTo-Json
    
    $newTicket = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Method POST -Headers $headers -Body $ticketData
    Write-Host "✅ Ticket created: $($newTicket.id)" -ForegroundColor Green
    
    Write-Host "`n🎉 Complete feedback system is WORKING!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
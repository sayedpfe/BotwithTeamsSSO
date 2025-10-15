# Try to troubleshoot the authentication issue by testing with curl
Write-Host "Testing authentication with different approaches..." -ForegroundColor Green

# Test 1: Check if client credentials are working with detailed error info
Write-Host "`n1. Testing token acquisition..." -ForegroundColor Yellow

$tokenUri = "https://login.microsoftonline.com/b22f8675-8375-455b-941a-67bee4cf7747/oauth2/v2.0/token"
$body = @{
    client_id = '89155d3a-359d-4603-b821-0504395e331f'
    scope = 'https://89155d3a-359d-4603-b821-0504395e331f/.default'
    client_secret = 'i1q8Q~1VJHl0Zw3dUYeOt4CRj1DQCqVPQrqW.cyU'
    grant_type = 'client_credentials'
}

try {
    # Use Invoke-WebRequest for more detailed error information
    $response = Invoke-WebRequest -Uri $tokenUri -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"
    $tokenData = $response.Content | ConvertFrom-Json
    Write-Host "✅ Token acquired successfully!" -ForegroundColor Green
    
    # Test 2: Try the API endpoints
    Write-Host "`n2. Testing API endpoints..." -ForegroundColor Yellow
    $token = $tokenData.access_token
    $headers = @{ 'Authorization' = "Bearer $token" }
    
    # Test the tickets endpoint
    try {
        $ticketsResponse = Invoke-RestMethod -Uri 'https://saaliticketsapiclean.azurewebsites.net/api/tickets' -Method Get -Headers $headers
        Write-Host "✅ Tickets endpoint accessible!" -ForegroundColor Green
        Write-Host "Tickets: $($ticketsResponse | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
    } catch {
        Write-Host "❌ Tickets endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Token acquisition failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        $streamReader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $responseBody = $streamReader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
    }
}
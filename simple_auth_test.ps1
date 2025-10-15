# Test authentication endpoint
Write-Host "Testing Authentication..." -ForegroundColor Green

# Get token
$body = @{
    client_id = '89155d3a-359d-4603-b821-0504395e331f'
    scope = 'https://89155d3a-359d-4603-b821-0504395e331f/.default'
    client_secret = 'i1q8Q~1VJHl0Zw3dUYeOt4CRj1DQCqVPQrqW.cyU'
    grant_type = 'client_credentials'
}

try {
    $tokenResponse = Invoke-RestMethod -Uri 'https://login.microsoftonline.com/b22f8675-8375-455b-941a-67bee4cf7747/oauth2/v2.0/token' -Method Post -Body $body
    $token = $tokenResponse.access_token
    Write-Host "Token acquired" -ForegroundColor Green

    # Test auth endpoint
    $headers = @{ 'Authorization' = "Bearer $token" }
    $authResponse = Invoke-RestMethod -Uri 'https://saaliticketsapiclean.azurewebsites.net/test/auth' -Method Get -Headers $headers
    Write-Host "Auth test successful:" -ForegroundColor Green
    $authResponse | ConvertTo-Json

} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
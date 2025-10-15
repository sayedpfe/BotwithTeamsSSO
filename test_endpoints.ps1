# Test different endpoints to understand the API state
Write-Host "Testing API endpoints..." -ForegroundColor Green

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
    Write-Host "✅ Token acquired" -ForegroundColor Green

    $headers = @{ 'Authorization' = "Bearer $token" }

    # Test different endpoints
    $endpoints = @(
        'https://saaliticketsapiclean.azurewebsites.net/health',
        'https://saaliticketsapiclean.azurewebsites.net/api/tickets',
        'https://saaliticketsapiclean.azurewebsites.net/test/auth'
    )

    foreach ($endpoint in $endpoints) {
        Write-Host "`nTesting: $endpoint" -ForegroundColor Yellow
        try {
            if ($endpoint -eq 'https://saaliticketsapiclean.azurewebsites.net/health') {
                # Health endpoint doesn't need auth
                $response = Invoke-RestMethod -Uri $endpoint -Method Get
            } else {
                $response = Invoke-RestMethod -Uri $endpoint -Method Get -Headers $headers
            }
            Write-Host "✅ Success: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor Green
        } catch {
            Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
            if ($_.Exception.Response) {
                Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
            }
        }
    }

} catch {
    Write-Host "❌ Token error: $($_.Exception.Message)" -ForegroundColor Red
}
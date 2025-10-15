# Simple token debug
Write-Host "Testing token..." -ForegroundColor Green

$body = @{
    client_id = '89155d3a-359d-4603-b821-0504395e331f'
    scope = 'https://89155d3a-359d-4603-b821-0504395e331f/.default'
    client_secret = 'i1q8Q~1VJHl0Zw3dUYeOt4CRj1DQCqVPQrqW.cyU'
    grant_type = 'client_credentials'
}

try {
    $response = Invoke-RestMethod -Uri 'https://login.microsoftonline.com/b22f8675-8375-455b-941a-67bee4cf7747/oauth2/v2.0/token' -Method Post -Body $body
    Write-Host "Token acquired!" -ForegroundColor Green
    
    # Test with simple health endpoint first
    Write-Host "`nTesting health endpoint..." -ForegroundColor Yellow
    $healthResponse = Invoke-RestMethod -Uri 'https://saaliticketsapiclean.azurewebsites.net/health' -Method Get
    Write-Host "Health: $($healthResponse.status)" -ForegroundColor Green
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}
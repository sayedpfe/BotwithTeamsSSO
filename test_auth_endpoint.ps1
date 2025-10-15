# Test the authentication endpoint separately from storage
Write-Host "🔐 Testing Authentication Endpoint..." -ForegroundColor Green

# Get access token
$body = @{
    client_id = '89155d3a-359d-4603-b821-0504395e331f'
    scope = 'https://89155d3a-359d-4603-b821-0504395e331f/.default'
    client_secret = 'i1q8Q~1VJHl0Zw3dUYeOt4CRj1DQCqVPQrqW.cyU'
    grant_type = 'client_credentials'
}

try {
    Write-Host "📝 Getting access token..." -ForegroundColor Yellow
    $tokenResponse = Invoke-RestMethod -Uri 'https://login.microsoftonline.com/b22f8675-8375-455b-941a-67bee4cf7747/oauth2/v2.0/token' -Method Post -Body $body
    $accessToken = $tokenResponse.access_token
    Write-Host "✅ Token acquired successfully" -ForegroundColor Green

    # Test health endpoint (no auth required)
    Write-Host "`n🏥 Testing health endpoint..." -ForegroundColor Yellow
    $healthResponse = Invoke-RestMethod -Uri 'https://saaliticketsapiclean.azurewebsites.net/health' -Method Get
    Write-Host "✅ Health check: $($healthResponse.status)" -ForegroundColor Green

    # Test unauthorized access to auth endpoint
    Write-Host "`n🚫 Testing unauthorized access to auth endpoint..." -ForegroundColor Yellow
    try {
        $unauthorizedResponse = Invoke-RestMethod -Uri 'https://saaliticketsapiclean.azurewebsites.net/test/auth' -Method Get
        Write-Host "❌ Expected 401 but got: $unauthorizedResponse" -ForegroundColor Red
    } catch {
        if ($_.Exception.Response.StatusCode -eq 401) {
            Write-Host "✅ Correctly received 401 Unauthorized" -ForegroundColor Green
        } else {
            Write-Host "❌ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # Test authorized access to auth endpoint
    Write-Host "`n🔑 Testing authorized access to auth endpoint..." -ForegroundColor Yellow
    $headers = @{
        'Authorization' = "Bearer $accessToken"
        'Content-Type' = 'application/json'
    }
    
    try {
        $authResponse = Invoke-RestMethod -Uri 'https://saaliticketsapiclean.azurewebsites.net/test/auth' -Method Get -Headers $headers
        Write-Host "✅ Authentication test successful!" -ForegroundColor Green
        Write-Host "Response: $($authResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
    } catch {
        Write-Host "❌ Authentication test failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
            $streamReader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $responseBody = $streamReader.ReadToEnd()
            Write-Host "Response Body: $responseBody" -ForegroundColor Red
        }
    }

} catch {
    Write-Host "❌ Failed to get access token: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🏁 Authentication test completed" -ForegroundColor Green
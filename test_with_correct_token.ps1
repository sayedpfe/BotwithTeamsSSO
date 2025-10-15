# Test API with the working token from test_token.ps1
Write-Host "Testing API with working token..." -ForegroundColor Green

$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"  # Correct secret
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

# Use the working scope from test_token.ps1
Write-Host "Getting token for $appId/.default" -ForegroundColor Yellow
$body = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $response = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
    Write-Host "✅ Token acquired successfully!" -ForegroundColor Green
    
    $headers = @{ "Authorization" = "Bearer $($response.access_token)" }
    
    # Test the API endpoints with the working token
    $endpoints = @(
        @{ url = "https://saaliticketsapiclean.azurewebsites.net/health"; auth = $false },
        @{ url = "https://saaliticketsapiclean.azurewebsites.net/test/auth"; auth = $true },
        @{ url = "https://saaliticketsapiclean.azurewebsites.net/api/tickets"; auth = $true }
    )
    
    foreach ($endpoint in $endpoints) {
        Write-Host "`nTesting: $($endpoint.url)" -ForegroundColor Cyan
        try {
            if ($endpoint.auth) {
                $result = Invoke-RestMethod -Uri $endpoint.url -Headers $headers
            } else {
                $result = Invoke-RestMethod -Uri $endpoint.url
            }
            Write-Host "✅ Success!" -ForegroundColor Green
            Write-Host "Response: $($result | ConvertTo-Json -Depth 2)" -ForegroundColor Gray
        } catch {
            Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
            if ($_.Exception.Response) {
                Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
            }
        }
    }
    
} catch {
    Write-Host "❌ Token acquisition failed: $($_.Exception.Message)" -ForegroundColor Red
}
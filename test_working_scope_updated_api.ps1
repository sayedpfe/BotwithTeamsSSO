# Test the working scope against updated API
Write-Host "Testing working scope against updated API..." -ForegroundColor Green

$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

# Use the working scope
$body = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $response = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
    Write-Host "✅ Token acquired!" -ForegroundColor Green
    
    $headers = @{ "Authorization" = "Bearer $($response.access_token)" }
    
    # Test API endpoints
    $endpoints = @(
        "https://saaliticketsapiclean.azurewebsites.net/test/auth",
        "https://saaliticketsapiclean.azurewebsites.net/api/tickets"
    )
    
    foreach ($endpoint in $endpoints) {
        Write-Host "`nTesting: $endpoint" -ForegroundColor Yellow
        try {
            $result = Invoke-RestMethod -Uri $endpoint -Headers $headers
            Write-Host "✅ Success!" -ForegroundColor Green
            Write-Host "Response: $($result | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
        } catch {
            Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "❌ Token failed: $($_.Exception.Message)" -ForegroundColor Red
}
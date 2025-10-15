# Test the exposed API scopes
Write-Host "Testing exposed API scopes..." -ForegroundColor Green

$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

# Test the exposed API scopes
$scopes = @(
    "api://botid-89155d3a-359d-4603-b821-0504395e331f/Tickets.ReadWrite",
    "api://botid-89155d3a-359d-4603-b821-0504395e331f/access_as_user"
)

foreach ($scope in $scopes) {
    Write-Host "`nTesting scope: $scope" -ForegroundColor Yellow
    
    $body = @{
        grant_type = "client_credentials"
        client_id = $appId
        client_secret = $appSecret
        scope = $scope
    }
    
    try {
        $response = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
        Write-Host "✅ Token acquired successfully!" -ForegroundColor Green
        Write-Host "Token length: $($response.access_token.Length)" -ForegroundColor Green
        
        # Test API call with this token
        $headers = @{ "Authorization" = "Bearer $($response.access_token)" }
        try {
            $apiResponse = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Headers $headers
            Write-Host "✅ API call succeeded!" -ForegroundColor Green
            Write-Host "Response: $($apiResponse | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
        } catch {
            Write-Host "❌ API call failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "❌ Token acquisition failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}
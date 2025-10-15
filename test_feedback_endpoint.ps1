# Test feedback endpoint specifically
Write-Host "Testing Feedback Endpoint..." -ForegroundColor Green

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
    
    # Test feedback GET endpoint
    Write-Host "`nTesting GET /api/feedback..." -ForegroundColor Yellow
    try {
        $feedback = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/feedback" -Headers $headers
        Write-Host "✅ Feedback GET successful: $($feedback.Count) items" -ForegroundColor Green
    } catch {
        Write-Host "❌ Feedback GET failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        }
    }
    
    # Test feedback POST endpoint
    Write-Host "`nTesting POST /api/feedback..." -ForegroundColor Yellow
    $testFeedback = @{
        userId = "test-user"
        userName = "Test User"
        conversationId = "test-conversation"
        activityId = "test-activity"
        botResponse = "Test bot response"
        reaction = "like"
        comment = "This is a test feedback"
        category = "general"
    } | ConvertTo-Json
    
    try {
        $newFeedback = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/feedback" -Method POST -Headers $headers -Body $testFeedback
        Write-Host "✅ Feedback POST successful!" -ForegroundColor Green
        Write-Host "   Feedback ID: $($newFeedback.id)" -ForegroundColor Cyan
    } catch {
        Write-Host "❌ Feedback POST failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "❌ Token failed: $($_.Exception.Message)" -ForegroundColor Red
}
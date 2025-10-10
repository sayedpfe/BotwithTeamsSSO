# Test script to verify feedback API with Table Storage
$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

Write-Host "Testing feedback API with Table Storage..." -ForegroundColor Yellow

# Get authentication token
Write-Host "Getting authentication token..." -ForegroundColor Cyan
$body = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $tokenResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
    Write-Host "✅ Token acquired successfully" -ForegroundColor Green
    
    $headers = @{ "Authorization" = "Bearer $($tokenResponse.access_token)" }
    
    # Test 1: Get existing feedback
    Write-Host "`nTest 1: Getting existing feedback..." -ForegroundColor Cyan
    try {
        $feedbackList = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/feedback" -Headers $headers
        Write-Host "✅ GET feedback succeeded - Count: $($feedbackList.Count)" -ForegroundColor Green
    } catch {
        Write-Host "❌ GET feedback failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Test 2: Create new feedback
    Write-Host "`nTest 2: Creating new feedback..." -ForegroundColor Cyan
    $newFeedback = @{
        userId = "test-user-table-storage"
        userName = "Test User"
        conversationId = "test-conversation-table-storage"
        activityId = "test-activity-123"
        botResponse = "This is a test bot response for Table Storage testing"
        reaction = "like"
        comment = "Test feedback from PowerShell script - Table Storage test"
        category = "Test"
    } | ConvertTo-Json
    
    try {
        $feedbackResult = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/feedback" -Method POST -Headers $headers -Body $newFeedback -ContentType "application/json"
        Write-Host "✅ POST feedback succeeded - ID: $($feedbackResult.id)" -ForegroundColor Green
        
        # Test 3: Verify the feedback was stored in Table Storage
        Write-Host "`nTest 3: Verifying feedback was stored..." -ForegroundColor Cyan
        $feedbackListAfter = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/feedback" -Headers $headers
        Write-Host "✅ Verification succeeded - Total feedback count: $($feedbackListAfter.Count)" -ForegroundColor Green
        
        if ($feedbackListAfter.Count -gt 0) {
            $latestFeedback = $feedbackListAfter | Sort-Object timestamp -Descending | Select-Object -First 1
            Write-Host "Latest feedback - Rating: $($latestFeedback.rating), Comment: $($latestFeedback.comment)" -ForegroundColor Green
        }
        
    } catch {
        Write-Host "❌ POST feedback failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Token acquisition failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nFeedback API test completed." -ForegroundColor Yellow
# Test API with proper authentication after fixes
$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

Write-Host "🧪 Testing API with Authentication (After Fixes)" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan

# Get token
Write-Host "`n🎫 Acquiring token..." -ForegroundColor Yellow
$body = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $response = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
    $token = $response.access_token
    Write-Host "✅ Token acquired successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Token acquisition failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test authenticated endpoints
$headers = @{ 
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "`n📋 Testing authenticated endpoints..." -ForegroundColor Yellow

# Test GET tickets
Write-Host "`n🎫 Testing GET /api/tickets..." -ForegroundColor Cyan
try {
    $ticketsResponse = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Headers $headers
    Write-Host "✅ GET tickets: SUCCESS" -ForegroundColor Green
    Write-Host "   Found $($ticketsResponse.Count) tickets" -ForegroundColor Gray
    if ($ticketsResponse.Count -gt 0) {
        Write-Host "   Sample ticket: $($ticketsResponse[0].Title)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ GET tickets failed: $($_.Exception.Message)" -ForegroundColor Red
    $statusCode = $_.Exception.Response.StatusCode
    Write-Host "   Status: $statusCode" -ForegroundColor Red
}

# Test GET feedback
Write-Host "`n💬 Testing GET /api/feedback..." -ForegroundColor Cyan
try {
    $feedbackResponse = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/feedback" -Headers $headers
    Write-Host "✅ GET feedback: SUCCESS" -ForegroundColor Green
    Write-Host "   Found $($feedbackResponse.Count) feedback items" -ForegroundColor Gray
} catch {
    Write-Host "❌ GET feedback failed: $($_.Exception.Message)" -ForegroundColor Red
    $statusCode = $_.Exception.Response.StatusCode
    Write-Host "   Status: $statusCode" -ForegroundColor Red
}

# Test POST ticket
Write-Host "`n📝 Testing POST /api/tickets..." -ForegroundColor Cyan
$ticketData = @{
    title = "Test Ticket via Auth - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    description = "This is a test ticket created via authenticated PowerShell call to verify the API is working correctly."
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Method POST -Headers $headers -Body $ticketData
    Write-Host "✅ POST ticket: SUCCESS" -ForegroundColor Green
    Write-Host "   Created ticket ID: $($createResponse.id)" -ForegroundColor Gray
    Write-Host "   Title: $($createResponse.title)" -ForegroundColor Gray
} catch {
    Write-Host "❌ POST ticket failed: $($_.Exception.Message)" -ForegroundColor Red
    $statusCode = $_.Exception.Response.StatusCode
    Write-Host "   Status: $statusCode" -ForegroundColor Red
}

# Test POST feedback
Write-Host "`n🗣️  Testing POST /api/feedback..." -ForegroundColor Cyan
$feedbackData = @{
    userName = "PowerShell Test User"
    comment = "Test feedback from authenticated PowerShell call - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    conversationId = "test-conversation-auth"
    activityId = "test-activity-auth"
    botResponse = "This is a test bot response for feedback verification"
    userId = "test-user-auth"
    category = "Testing"
    reaction = "like"
} | ConvertTo-Json

try {
    $feedbackCreateResponse = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/feedback" -Method POST -Headers $headers -Body $feedbackData
    Write-Host "✅ POST feedback: SUCCESS" -ForegroundColor Green
    Write-Host "   Created feedback ID: $($feedbackCreateResponse.id)" -ForegroundColor Gray
    Write-Host "   Comment: $($feedbackCreateResponse.comment)" -ForegroundColor Gray
} catch {
    Write-Host "❌ POST feedback failed: $($_.Exception.Message)" -ForegroundColor Red
    $statusCode = $_.Exception.Response.StatusCode
    Write-Host "   Status: $statusCode" -ForegroundColor Red
}

Write-Host "`n🎉 Authentication testing completed!" -ForegroundColor Green
Write-Host "`n📊 Summary:" -ForegroundColor Yellow
Write-Host "- If all tests pass, the feedback system is fully working!" -ForegroundColor Gray
Write-Host "- Any failures indicate specific areas that need attention" -ForegroundColor Gray
# Teams Enterprise Support Hub - API Token Testing Script
# This script helps test API calls and observe token logging

Write-Host "🔍 Teams Enterprise Support Hub - API Token Testing" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Check if bot is running
$botRunning = $false
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7130" -Method HEAD -TimeoutSec 5 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        $botRunning = $true
    }
} catch {
    # Bot not running
}

if (-not $botRunning) {
    Write-Host "❌ Bot is not running on localhost:7130" -ForegroundColor Red
    Write-Host "💡 Please start the bot first using:" -ForegroundColor Yellow
    Write-Host "   cd BotConversationSsoQuickstart" -ForegroundColor Yellow
    Write-Host "   dotnet run" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Then interact with the bot in Teams to trigger API calls and see token logging." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Bot is running on localhost:7130" -ForegroundColor Green

# Check API endpoint
Write-Host ""
Write-Host "🔍 Testing API endpoint connectivity..." -ForegroundColor Cyan

$apiUrl = "https://saaliticketsapiclean.azurewebsites.net"
try {
    $healthResponse = Invoke-WebRequest -Uri "$apiUrl/health" -Method GET -TimeoutSec 10
    Write-Host "✅ API Health Check: $($healthResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ API Health Check Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Feedback endpoint
try {
    $feedbackTest = @{
        UserId = "test-user"
        UserName = "Test User"
        ConversationId = "test-conversation"
        ActivityId = "test-activity"
        BotResponse = "Test response"
        Reaction = "thumbs_up"
        Comment = "Test comment"
        Category = "general"
    }
    
    $feedbackJson = $feedbackTest | ConvertTo-Json
    $feedbackResponse = Invoke-WebRequest -Uri "$apiUrl/api/feedback" -Method POST -Body $feedbackJson -ContentType "application/json" -TimeoutSec 10
    Write-Host "✅ API Feedback Test: $($feedbackResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ API Feedback Test Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎯 How to see token logging in action:" -ForegroundColor Cyan
Write-Host "1. Open Microsoft Teams" -ForegroundColor White
Write-Host "2. Go to your bot conversation" -ForegroundColor White
Write-Host "3. Send any message to trigger the bot" -ForegroundColor White
Write-Host "4. Click on feedback buttons (👍/👎)" -ForegroundColor White
Write-Host "5. Watch the console output for detailed token information" -ForegroundColor White

Write-Host ""
Write-Host "🔍 Look for these log entries in your bot console:" -ForegroundColor Cyan
Write-Host "  [TicketApiClient] GetAccessTokenAsync called - AuthType: None" -ForegroundColor Gray
Write-Host "  [HTTP-xxxxxxxx] ===== OUTGOING HTTP REQUEST =====" -ForegroundColor Gray
Write-Host "  [HTTP-xxxxxxxx] 🔐 Authorization: Bearer <TOKEN>" -ForegroundColor Gray
Write-Host "  [HTTP-xxxxxxxx] 🔍 Token Length: xxx characters" -ForegroundColor Gray
Write-Host "  [HTTP-xxxxxxxx] 🔍 Token Preview: eyJ0eXAiOiJKV1Qi..." -ForegroundColor Gray

Write-Host ""
Write-Host "💡 Current Configuration:" -ForegroundColor Yellow
Write-Host "  Bot URL: http://localhost:7130" -ForegroundColor White
Write-Host "  API URL: $apiUrl" -ForegroundColor White
Write-Host "  Auth Type: None (as configured in appsettings.json)" -ForegroundColor White
Write-Host "  Token Logging: ✅ Enabled with detailed headers and JWT structure" -ForegroundColor Green

Write-Host ""
Write-Host "🔧 To test with Azure AD authentication:" -ForegroundColor Yellow
Write-Host "1. Change AuthType from 'None' to 'AzureAD' in appsettings.json" -ForegroundColor White
Write-Host "2. Restart the bot" -ForegroundColor White
Write-Host "3. Try the same interactions - you'll see actual JWT tokens being logged" -ForegroundColor White

Write-Host ""
Write-Host "✅ Ready for token testing! Check your bot console for detailed logs." -ForegroundColor Green
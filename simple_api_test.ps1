# Simple deployment verification
Write-Host "🔍 Verifying API Deployment" -ForegroundColor Cyan

# Test health endpoint
try {
    $healthResponse = Invoke-WebRequest -Uri "https://saaliticketsapiclean.azurewebsites.net/health" -UseBasicParsing
    Write-Host "✅ Health: $($healthResponse.StatusCode)" -ForegroundColor Green
    Write-Host "   Content: $($healthResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Health failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test a simple unauthorized endpoint to see if we get the right error
Write-Host "`n🔓 Testing unauthorized access..." -ForegroundColor Yellow
try {
    $ticketsResponse = Invoke-WebRequest -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -UseBasicParsing
    Write-Host "✅ Tickets (no auth): $($ticketsResponse.StatusCode)" -ForegroundColor Green
} catch {
    $statusCode = $_.Exception.Response.StatusCode
    Write-Host "   Status: $statusCode" -ForegroundColor $(if ($statusCode -eq "Unauthorized") { "Green" } else { "Red" })
    if ($statusCode -eq "Unauthorized") {
        Write-Host "   ✅ Correct - API requires authentication" -ForegroundColor Green
    } elseif ($statusCode -eq "InternalServerError") {
        Write-Host "   ❌ 500 Error - Something is broken in the API" -ForegroundColor Red
    } else {
        Write-Host "   ⚠️  Unexpected status: $statusCode" -ForegroundColor Yellow
    }
}

Write-Host "`n📋 Analysis:" -ForegroundColor Yellow
Write-Host "- If health works but tickets gives 500, the API deployment has issues" -ForegroundColor Gray
Write-Host "- If tickets gives 401, authentication is working correctly" -ForegroundColor Gray
Write-Host "- If tickets gives 500, there's a runtime error in the API" -ForegroundColor Gray
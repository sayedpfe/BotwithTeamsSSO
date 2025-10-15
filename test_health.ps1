# Test just the health endpoint first
Write-Host "Testing health endpoint..." -ForegroundColor Green

try {
    $healthResponse = Invoke-RestMethod -Uri 'https://saaliticketsapiclean.azurewebsites.net/health' -Method Get
    Write-Host "✅ Health check successful!" -ForegroundColor Green
    Write-Host "Status: $($healthResponse.status)" -ForegroundColor Cyan
    Write-Host "Response: $($healthResponse | ConvertTo-Json)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}
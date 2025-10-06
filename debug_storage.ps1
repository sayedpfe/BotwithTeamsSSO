# Test ticket creation and immediately check if it's stored
$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

Write-Host "Creating test ticket and checking storage..." -ForegroundColor Yellow

# Get token
$body = @{
    grant_type = "client_credentials"
    client_id = $appId
    client_secret = $appSecret
    scope = "$appId/.default"
}

try {
    $tokenResponse = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" -Method POST -Body $body
    $token = $tokenResponse.access_token
    $headers = @{ "Authorization" = "Bearer $token" }
    
    Write-Host "✅ Token acquired" -ForegroundColor Green
    
    # Create a test ticket
    $ticketData = @{
        title = "DEBUG: Test Ticket $(Get-Date -Format 'HH:mm:ss')"
        description = "This is a test ticket to debug storage. Created at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    } | ConvertTo-Json
    
    Write-Host "Creating ticket..." -ForegroundColor Cyan
    $newTicket = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Method POST -Headers $headers -Body $ticketData -ContentType "application/json"
    Write-Host "✅ Ticket created: $($newTicket.id)" -ForegroundColor Green
    
    # Wait a moment for file write
    Start-Sleep -Seconds 2
    
    # Check if ticket shows up in list
    Write-Host "Listing tickets..." -ForegroundColor Cyan
    $tickets = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Headers $headers
    Write-Host "Found $($tickets.Count) tickets total" -ForegroundColor Green
    
    $foundTicket = $tickets | Where-Object { $_.id -eq $newTicket.id }
    if ($foundTicket) {
        Write-Host "✅ NEW TICKET FOUND in list!" -ForegroundColor Green
        Write-Host "   Title: $($foundTicket.title)" -ForegroundColor White
    } else {
        Write-Host "❌ New ticket NOT found in list" -ForegroundColor Red
    }
    
    # Check the file directly
    Write-Host "Checking file storage..." -ForegroundColor Cyan
    $fileContent = Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/diag/file/raw"
    $fileTickets = $fileContent.Tickets
    Write-Host "File contains $($fileTickets.Count) tickets" -ForegroundColor Green
    
    $foundInFile = $fileTickets | Where-Object { $_.Id -eq $newTicket.id }
    if ($foundInFile) {
        Write-Host "✅ NEW TICKET FOUND in file!" -ForegroundColor Green
    } else {
        Write-Host "❌ New ticket NOT found in file" -ForegroundColor Red
        Write-Host "File tickets:" -ForegroundColor Yellow
        $fileTickets | ForEach-Object { Write-Host "  - $($_.Id): $($_.Title)" -ForegroundColor Gray }
    }
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nDebugging complete." -ForegroundColor Yellow
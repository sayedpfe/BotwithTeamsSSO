# Configure Azure App Service with missing settings
$resourceGroup = "M365AgentTeamsSSO-rg"
$appName = "SaaliTicketsApiClean"

Write-Host "⚙️  Configuring Azure App Service Settings" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan

# Configuration values from appsettings.json
$storageConnectionString = "DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;AccountName=sayedsupportticketsstg;AccountKey=yaWC+VJopwODJycb71dUEb61RzSs7ITC1iN67d8Gmp2bIGE/n5kBndQQz2KH81WhZaCwIYCkjuwu+AStL+rn0g==;BlobEndpoint=https://sayedsupportticketsstg.blob.core.windows.net/;FileEndpoint=https://sayedsupportticketsstg.file.core.windows.net/;QueueEndpoint=https://sayedsupportticketsstg.queue.core.windows.net/;TableEndpoint=https://sayedsupportticketsstg.table.core.windows.net/"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"
$clientId = "89155d3a-359d-4603-b821-0504395e331f"
$audience = "api://botid-89155d3a-359d-4603-b821-0504395e331f"

Write-Host "`n🔗 Step 1: Setting up Storage Connection String..." -ForegroundColor Yellow
try {
    az webapp config connection-string set `
        --resource-group $resourceGroup `
        --name $appName `
        --connection-string-type Custom `
        --settings TableStorage="$storageConnectionString"
    
    Write-Host "✅ Storage connection string configured" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to set storage connection string: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🛡️  Step 2: Setting up Azure AD Authentication Settings..." -ForegroundColor Yellow
try {
    # Set Azure AD settings as app settings
    az webapp config appsettings set `
        --resource-group $resourceGroup `
        --name $appName `
        --settings `
        "AzureAd__Instance=https://login.microsoftonline.com/" `
        "AzureAd__TenantId=$tenantId" `
        "AzureAd__ClientId=$clientId" `
        "AzureAd__Audience=$audience"
    
    Write-Host "✅ Azure AD settings configured" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to set Azure AD settings: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🗃️  Step 3: Setting up Table Storage Settings..." -ForegroundColor Yellow
try {
    # Set table storage specific settings
    az webapp config appsettings set `
        --resource-group $resourceGroup `
        --name $appName `
        --settings `
        "AzureTable__AccountUrl=https://sayedsupportticketsstg.table.core.windows.net" `
        "AzureTable__TableName=SupportTickets" `
        "Storage__TableStorage__ConnectionString=$storageConnectionString"
    
    Write-Host "✅ Table storage settings configured" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to set table storage settings: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔄 Step 4: Restarting App Service..." -ForegroundColor Yellow
try {
    az webapp restart --resource-group $resourceGroup --name $appName
    Write-Host "✅ App Service restarted" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to restart app service: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n⏳ Step 5: Waiting for app to restart..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host "`n🧪 Step 6: Testing configuration..." -ForegroundColor Yellow
try {
    # Test health endpoint
    $healthResponse = Invoke-WebRequest -Uri "https://$appName.azurewebsites.net/health" -UseBasicParsing
    Write-Host "✅ Health check: $($healthResponse.StatusCode)" -ForegroundColor Green
    
    # Test tickets endpoint (should still be 401 without auth, but not 500)
    try {
        $ticketsResponse = Invoke-WebRequest -Uri "https://$appName.azurewebsites.net/api/tickets" -UseBasicParsing
        Write-Host "✅ Tickets endpoint: $($ticketsResponse.StatusCode)" -ForegroundColor Green
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode
        if ($statusCode -eq "Unauthorized") {
            Write-Host "✅ Tickets endpoint: 401 Unauthorized (expected without auth)" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Tickets endpoint: $statusCode (investigate if not 401)" -ForegroundColor Yellow
        }
    }
    
    # Test feedback endpoint
    try {
        $feedbackResponse = Invoke-WebRequest -Uri "https://$appName.azurewebsites.net/api/feedback" -UseBasicParsing
        Write-Host "✅ Feedback endpoint: $($feedbackResponse.StatusCode)" -ForegroundColor Green
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode
        if ($statusCode -eq "Unauthorized") {
            Write-Host "✅ Feedback endpoint: 401 Unauthorized (expected without auth)" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Feedback endpoint: $statusCode (investigate if not 401)" -ForegroundColor Yellow
        }
    }
}
catch {
    Write-Host "❌ Testing failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 Configuration completed!" -ForegroundColor Green
Write-Host "`n📋 Next steps:" -ForegroundColor Yellow
Write-Host "1. Run the enhanced test script to verify authentication works" -ForegroundColor Gray
Write-Host "2. Test ticket creation from the bot" -ForegroundColor Gray
Write-Host "3. Test the feedback system" -ForegroundColor Gray
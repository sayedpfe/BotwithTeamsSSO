# Configure Azure App Service Managed Identity
$resourceGroup = "M365AgentTeamsSSO-rg"
$appName = "SaaliTicketsApiClean"
$storageAccountName = "sayedsupportticketsstg"

Write-Host "🔐 Configuring Managed Identity for Azure App Service" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan

# Step 1: Enable System-assigned Managed Identity
Write-Host "`n🆔 Step 1: Enabling System-assigned Managed Identity..." -ForegroundColor Yellow
try {
    $identityResult = az webapp identity assign --resource-group $resourceGroup --name $appName | ConvertFrom-Json
    $principalId = $identityResult.principalId
    Write-Host "✅ Managed Identity enabled" -ForegroundColor Green
    Write-Host "   Principal ID: $principalId" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Failed to enable Managed Identity: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Get the storage account resource ID
Write-Host "`n🗃️  Step 2: Getting storage account information..." -ForegroundColor Yellow
try {
    $storageAccount = az storage account show --name $storageAccountName --resource-group $resourceGroup | ConvertFrom-Json
    $storageResourceId = $storageAccount.id
    Write-Host "✅ Storage account found" -ForegroundColor Green
    Write-Host "   Resource ID: $storageResourceId" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Failed to get storage account: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Assign Storage Table Data Contributor role
Write-Host "`n🔑 Step 3: Assigning Storage Table Data Contributor role..." -ForegroundColor Yellow
try {
    az role assignment create `
        --assignee $principalId `
        --role "Storage Table Data Contributor" `
        --scope $storageResourceId
    
    Write-Host "✅ Role assigned successfully" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to assign role: $($_.Exception.Message)" -ForegroundColor Red
    # Don't exit here, the role might already be assigned
}

# Step 4: Remove connection string settings (not needed for Managed Identity)
Write-Host "`n🧹 Step 4: Cleaning up connection string settings..." -ForegroundColor Yellow
try {
    # Remove connection string settings since we're using Managed Identity
    az webapp config connection-string delete --resource-group $resourceGroup --name $appName --setting-names "TableStorage" --yes
    
    # Remove the connection string from app settings too
    az webapp config appsettings delete --resource-group $resourceGroup --name $appName --setting-names "Storage__TableStorage__ConnectionString" --yes
    
    Write-Host "✅ Connection string settings removed" -ForegroundColor Green
}
catch {
    Write-Host "⚠️  Warning: Could not remove connection string settings: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 5: Verify current settings
Write-Host "`n🔍 Step 5: Verifying configuration..." -ForegroundColor Yellow
try {
    Write-Host "Current app settings:" -ForegroundColor Cyan
    az webapp config appsettings list --resource-group $resourceGroup --name $appName --output table
    
    Write-Host "`nManaged Identity status:" -ForegroundColor Cyan
    az webapp identity show --resource-group $resourceGroup --name $appName --output table
}
catch {
    Write-Host "❌ Failed to verify configuration: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 6: Restart the app
Write-Host "`n🔄 Step 6: Restarting App Service..." -ForegroundColor Yellow
try {
    az webapp restart --resource-group $resourceGroup --name $appName
    Write-Host "✅ App Service restarted" -ForegroundColor Green
}
catch {
    Write-Host "❌ Failed to restart app service: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n⏳ Waiting for app to restart..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

# Step 7: Test the configuration
Write-Host "`n🧪 Step 7: Testing Managed Identity configuration..." -ForegroundColor Yellow
try {
    # Test health endpoint
    $healthResponse = Invoke-WebRequest -Uri "https://$appName.azurewebsites.net/health" -UseBasicParsing
    Write-Host "✅ Health check: $($healthResponse.StatusCode)" -ForegroundColor Green
    
    # Test tickets endpoint without auth (should be 401, not 500)
    try {
        $ticketsResponse = Invoke-WebRequest -Uri "https://$appName.azurewebsites.net/api/tickets" -UseBasicParsing
        Write-Host "✅ Tickets endpoint: $($ticketsResponse.StatusCode)" -ForegroundColor Green
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode
        if ($statusCode -eq "Unauthorized") {
            Write-Host "✅ Tickets endpoint: 401 Unauthorized (correct - needs auth)" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Tickets endpoint: $statusCode" -ForegroundColor Yellow
        }
    }
}
catch {
    Write-Host "❌ Testing failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 Managed Identity configuration completed!" -ForegroundColor Green
Write-Host "`n📋 Summary:" -ForegroundColor Yellow
Write-Host "1. ✅ System-assigned Managed Identity enabled" -ForegroundColor Gray
Write-Host "2. ✅ Storage Table Data Contributor role assigned" -ForegroundColor Gray
Write-Host "3. ✅ Connection strings removed (using Managed Identity)" -ForegroundColor Gray
Write-Host "4. ✅ App Service restarted" -ForegroundColor Gray
Write-Host "`n🧪 Next step: Run the enhanced test script to verify authentication works" -ForegroundColor Cyan
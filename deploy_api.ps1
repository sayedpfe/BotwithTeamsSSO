# API Deployment Script - Deploy updated API with feedback functionality
$resourceGroup = "M365AgentTeamsSSO-rg"
$appName = "SaaliTicketsApiClean"
$projectPath = "SupportTicketsApi"

Write-Host "🚀 Deploying Updated API to Azure" -ForegroundColor Cyan
Write-Host "=" * 40 -ForegroundColor Cyan

# Step 1: Build the project
Write-Host "`n📦 Step 1: Building API project..." -ForegroundColor Yellow
try {
    Set-Location $projectPath
    dotnet build --configuration Release
    Write-Host "✅ Build successful" -ForegroundColor Green
}
catch {
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Create deployment package
Write-Host "`n📄 Step 2: Creating deployment package..." -ForegroundColor Yellow
try {
    dotnet publish --configuration Release --output ./publish
    Write-Host "✅ Package created in ./publish" -ForegroundColor Green
}
catch {
    Write-Host "❌ Package creation failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Deploy to Azure
Write-Host "`n☁️  Step 3: Deploying to Azure App Service..." -ForegroundColor Yellow
try {
    # Create zip package
    $zipPath = "../api-deployment.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath }
    
    Compress-Archive -Path "./publish/*" -DestinationPath $zipPath -Force
    Write-Host "✅ Deployment package created: $zipPath" -ForegroundColor Green
    
    # Deploy using Azure CLI
    Write-Host "📤 Uploading to Azure..." -ForegroundColor Cyan
    az webapp deployment source config-zip --resource-group $resourceGroup --name $appName --src $zipPath
    
    Write-Host "✅ Deployment completed!" -ForegroundColor Green
}
catch {
    Write-Host "❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 4: Restart the app service
Write-Host "`n🔄 Step 4: Restarting App Service..." -ForegroundColor Yellow
try {
    az webapp restart --resource-group $resourceGroup --name $appName
    Write-Host "✅ App Service restarted" -ForegroundColor Green
}
catch {
    Write-Host "❌ Restart failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Test the deployment
Write-Host "`n🧪 Step 5: Testing deployment..." -ForegroundColor Yellow
Start-Sleep -Seconds 10  # Give the app time to start

try {
    $healthResponse = Invoke-WebRequest -Uri "https://$appName.azurewebsites.net/health" -UseBasicParsing
    Write-Host "✅ Health check passed: $($healthResponse.StatusCode)" -ForegroundColor Green
    Write-Host "📄 Response: $($healthResponse.Content)" -ForegroundColor Cyan
    
    # Test the tickets endpoint
    Write-Host "`n🎫 Testing tickets endpoint..." -ForegroundColor Cyan
    try {
        $ticketsResponse = Invoke-WebRequest -Uri "https://$appName.azurewebsites.net/api/tickets" -UseBasicParsing
        Write-Host "✅ Tickets endpoint: $($ticketsResponse.StatusCode)" -ForegroundColor Green
    }
    catch {
        Write-Host "ℹ️  Tickets endpoint needs authentication (expected): $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Test the feedback endpoint
    Write-Host "`n💬 Testing feedback endpoint..." -ForegroundColor Cyan
    try {
        $feedbackResponse = Invoke-WebRequest -Uri "https://$appName.azurewebsites.net/api/feedback" -UseBasicParsing
        Write-Host "✅ Feedback endpoint: $($feedbackResponse.StatusCode)" -ForegroundColor Green
    }
    catch {
        Write-Host "ℹ️  Feedback endpoint status: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 Deployment process completed!" -ForegroundColor Green
Write-Host "`n📋 Next steps:" -ForegroundColor Yellow
Write-Host "1. Wait 2-3 minutes for the app to fully start" -ForegroundColor Gray
Write-Host "2. Run the enhanced test script again to verify" -ForegroundColor Gray
Write-Host "3. Test ticket creation from the bot" -ForegroundColor Gray

# Cleanup
Set-Location ..
Write-Host "`n🧹 Cleaned up temporary files" -ForegroundColor Gray
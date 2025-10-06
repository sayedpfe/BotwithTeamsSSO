# Azure Storage Account Setup Script
# This script creates an Azure Storage Account for your Support Tickets API

# Set variables
$resourceGroupName = "M365AgentTeamsSSO-rg"
$storageAccountName = "sayedticketstorage"
$location = "East US"

Write-Host "Creating Azure Storage Account for Support Tickets..." -ForegroundColor Green

# Check if resource group exists
$resourceGroup = az group show --name $resourceGroupName --query "name" -o tsv 2>$null
if (-not $resourceGroup) {
    Write-Host "Creating resource group: $resourceGroupName" -ForegroundColor Yellow
    az group create --name $resourceGroupName --location $location
}

# Create storage account
Write-Host "Creating storage account: $storageAccountName" -ForegroundColor Yellow
az storage account create `
    --name $storageAccountName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Standard_LRS `
    --kind StorageV2 `
    --access-tier Hot

# Get connection string
Write-Host "Getting connection string..." -ForegroundColor Yellow
$connectionString = az storage account show-connection-string `
    --name $storageAccountName `
    --resource-group $resourceGroupName `
    --query "connectionString" -o tsv

Write-Host "✅ Storage Account Created Successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Configuration Details:" -ForegroundColor Cyan
Write-Host "Storage Account Name: $storageAccountName"
Write-Host "Resource Group: $resourceGroupName"
Write-Host ""
Write-Host "🔐 Connection String:" -ForegroundColor Yellow
Write-Host $connectionString
Write-Host ""
Write-Host "📝 Update your appsettings.json with:" -ForegroundColor Cyan
Write-Host '"ConnectionStrings": {'
Write-Host '  "TableStorage": "' + $connectionString + '"'
Write-Host '}'
Write-Host ""
Write-Host "🚀 Your API will now use secure Azure Table Storage instead of local files!"
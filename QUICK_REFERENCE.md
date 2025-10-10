# Teams Enterprise Support Hub - Quick Reference Guide

## Solution Overview

**Teams Enterprise Support Hub** is an enterprise-grade Microsoft Teams integration that combines Azure AD SSO authentication with intelligent support ticket management. This guide provides essential configuration and deployment information.

### Key Features
- 🔐 **Azure AD Single Sign-On**: Seamless authentication within Teams
- 🎫 **Smart Ticket Management**: Automated ticket creation and tracking  
- 📊 **Real-time Feedback**: User experience analytics and insights
- 🏢 **Enterprise Ready**: Scalable cloud-native architecture
- 📱 **Teams Native**: Full integration with Microsoft Teams ecosystem

---

## 🔧 Essential Configuration Values

### Azure App Registration
```
Application ID: 89155d3a-359d-4603-b821-0504395e331f
Tenant ID: b22f8675-8375-455b-941a-67bee4cf7747
Client Secret: Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI
```

### OAuth Connections
```
Graph Connection: oauthbotsetting
Tickets Connection: ticketsoauth
```

### Endpoints
```
Bot URL: https://localhost:7130 (dev) / https://your-bot.azurewebsites.net (prod)
API URL: https://saaliticketsapiclean.azurewebsites.net
Storage Account: sayedsupportticketsstg
```

### Required Permissions
```
Microsoft Graph:
- User.Read (Delegated)
- Mail.Read (Delegated) 
- Mail.Send (Delegated)
```

### Redirect URIs
```
- https://token.botframework.com/.auth/web/redirect
- https://teams.microsoft.com/api/platform/v1.0/teams/app/auth/callback
```

## 🚀 Quick Setup Commands

### Azure CLI Setup
```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "your-subscription-id"

# Create resource group
az group create --name "M365AgentTeamsSSO-rg" --location "East US"

# Create storage account
az storage account create \
  --name "sayedsupportticketsstg" \
  --resource-group "M365AgentTeamsSSO-rg" \
  --location "East US" \
  --sku "Standard_LRS"
```

### Bot Deployment
```bash
# Build and publish bot
dotnet publish -c Release -o ./publish

# Deploy to Azure App Service
az webapp deployment source config-zip \
  --resource-group "M365AgentTeamsSSO-rg" \
  --name "your-bot-app" \
  --src "./publish.zip"
```

### API Deployment
```bash
# Deploy API
az webapp deployment source config-zip \
  --resource-group "M365AgentTeamsSSO-rg" \
  --name "SaaliTicketsApiClean" \
  --src "./api-deployment.zip"
```

## 🔍 Troubleshooting Quick Checks

### Authentication Issues
```bash
# Test OAuth connection
curl -X GET "https://login.microsoftonline.com/b22f8675-8375-455b-941a-67bee4cf7747/oauth2/v2.0/authorize?client_id=89155d3a-359d-4603-b821-0504395e331f&response_type=code&scope=User.Read"

# Verify app registration
az ad app show --id 89155d3a-359d-4603-b821-0504395e331f
```

### API Connectivity
```powershell
# Test API endpoint
Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/tickets" -Method GET

# Test feedback endpoint
$body = @{
    UserId = "test-user"
    UserName = "Test User"
    Reaction = "like"
    Comment = "Test feedback"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://saaliticketsapiclean.azurewebsites.net/api/feedback" -Method POST -Body $body -ContentType "application/json"
```

### Bot Framework Testing
```bash
# Test bot endpoint
curl -X POST "https://your-bot.azurewebsites.net/api/messages" \
  -H "Content-Type: application/json" \
  -d '{"type":"message","text":"hello"}'
```

## 📋 Pre-Go-Live Checklist

### Security ✅
- [ ] Client secrets stored securely (Key Vault)
- [ ] HTTPS enabled on all endpoints
- [ ] CORS properly configured
- [ ] API authentication enabled (for production)

### Configuration ✅
- [ ] Production connection strings updated
- [ ] Environment variables configured
- [ ] Monitoring and logging enabled
- [ ] Backup strategies implemented

### Testing ✅
- [ ] Authentication flow tested
- [ ] All bot commands working
- [ ] API endpoints responding
- [ ] Feedback system functional
- [ ] Error handling verified

### Deployment ✅
- [ ] Production environment provisioned
- [ ] DNS records configured
- [ ] SSL certificates installed
- [ ] Health checks passing
- [ ] Performance baseline established
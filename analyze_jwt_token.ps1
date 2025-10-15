# JWT Token Analysis Script
$appId = "89155d3a-359d-4603-b821-0504395e331f"
$appSecret = "Unr8Q~Y8alFpMHAIDMAjXIW.LwLZShxj1xeoZbvI"
$tenantId = "b22f8675-8375-455b-941a-67bee4cf7747"

Write-Host "🔍 JWT Token Analysis for API Troubleshooting" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan

# Get token for our app ID
Write-Host "`n🎫 Getting token for app ID..." -ForegroundColor Yellow
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
    
    # Decode JWT token (simple base64 decode of the payload)
    $tokenParts = $token.Split('.')
    $header = $tokenParts[0]
    $payload = $tokenParts[1]
    
    # Add padding if needed for base64 decode
    while ($payload.Length % 4) { $payload += "=" }
    
    $headerDecoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($header))
    $payloadDecoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload))
    
    Write-Host "`n📄 Token Header:" -ForegroundColor Cyan
    $headerJson = $headerDecoded | ConvertFrom-Json
    $headerJson | ConvertTo-Json -Depth 3
    
    Write-Host "`n📄 Token Payload:" -ForegroundColor Cyan  
    $payloadJson = $payloadDecoded | ConvertFrom-Json
    $payloadJson | ConvertTo-Json -Depth 3
    
    Write-Host "`n🔑 Key Token Claims:" -ForegroundColor Yellow
    Write-Host "   Issuer (iss): $($payloadJson.iss)" -ForegroundColor Gray
    Write-Host "   Audience (aud): $($payloadJson.aud)" -ForegroundColor Gray
    Write-Host "   Subject (sub): $($payloadJson.sub)" -ForegroundColor Gray
    Write-Host "   App ID (appid): $($payloadJson.appid)" -ForegroundColor Gray
    Write-Host "   Tenant ID (tid): $($payloadJson.tid)" -ForegroundColor Gray
    Write-Host "   Expiry (exp): $($payloadJson.exp) ($(Get-Date -UnixTimeSeconds $payloadJson.exp))" -ForegroundColor Gray
    
    # Check what the API expects vs what we have
    Write-Host "`n🔍 API Configuration Analysis:" -ForegroundColor Yellow
    Write-Host "   Expected Audience in API: 89155d3a-359d-4603-b821-0504395e331f OR api://botid-89155d3a-359d-4603-b821-0504395e331f" -ForegroundColor Gray
    Write-Host "   Token Audience: $($payloadJson.aud)" -ForegroundColor Gray
    Write-Host "   Expected Issuer: https://sts.windows.net/b22f8675-8375-455b-941a-67bee4cf7747/" -ForegroundColor Gray
    Write-Host "   Token Issuer: $($payloadJson.iss)" -ForegroundColor Gray
    
    if ($payloadJson.aud -eq "89155d3a-359d-4603-b821-0504395e331f" -or $payloadJson.aud -eq "api://botid-89155d3a-359d-4603-b821-0504395e331f") {
        Write-Host "   ✅ Audience matches!" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Audience mismatch!" -ForegroundColor Red
    }
    
    if ($payloadJson.iss -eq "https://sts.windows.net/b22f8675-8375-455b-941a-67bee4cf7747/") {
        Write-Host "   ✅ Issuer matches!" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Issuer mismatch!" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Token acquisition failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test a simple API call to see the exact error
Write-Host "`n🧪 Testing API call with detailed error info..." -ForegroundColor Yellow
try {
    $headers = @{ 
        "Authorization" = "Bearer $token"
        "Accept" = "application/json"
    }
    
    # Make a call and capture full response
    $uri = "https://saaliticketsapiclean.azurewebsites.net/api/tickets"
    Write-Host "   Making request to: $uri" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $uri -Headers $headers -UseBasicParsing
        Write-Host "   ✅ Success: $($response.StatusCode)" -ForegroundColor Green
        Write-Host "   Response: $($response.Content)" -ForegroundColor Gray
    } catch {
        $errorResponse = $_.Exception.Response
        if ($errorResponse) {
            Write-Host "   ❌ HTTP Error: $($errorResponse.StatusCode)" -ForegroundColor Red
            
            # Try to read the error response body
            try {
                $errorStream = $errorResponse.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorBody = $reader.ReadToEnd()
                Write-Host "   Error details: $errorBody" -ForegroundColor Red
            } catch {
                Write-Host "   Could not read error details" -ForegroundColor Red
            }
        } else {
            Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "❌ API test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n📋 Summary:" -ForegroundColor Yellow
Write-Host "1. Check if token audience/issuer matches API configuration" -ForegroundColor Gray
Write-Host "2. Verify Managed Identity has proper permissions" -ForegroundColor Gray  
Write-Host "3. Check if API is starting up correctly" -ForegroundColor Gray
Write-Host "4. Review JWT authentication configuration in Program.cs" -ForegroundColor Gray
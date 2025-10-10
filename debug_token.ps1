# Debug token acquisition step by step
Write-Host "🔍 Debugging Token Acquisition..." -ForegroundColor Green

# Test different token configurations
$configs = @(
    @{
        name = "Default scope"
        body = @{
            client_id = '89155d3a-359d-4603-b821-0504395e331f'
            scope = 'https://89155d3a-359d-4603-b821-0504395e331f/.default'
            client_secret = 'i1q8Q~1VJHl0Zw3dUYeOt4CRj1DQCqVPQrqW.cyU'
            grant_type = 'client_credentials'
        }
        tenant = 'b22f8675-8375-455b-941a-67bee4cf7747'
    },
    @{
        name = "API scope"
        body = @{
            client_id = '89155d3a-359d-4603-b821-0504395e331f'
            scope = 'api://89155d3a-359d-4603-b821-0504395e331f/.default'
            client_secret = 'i1q8Q~1VJHl0Zw3dUYeOt4CRj1DQCqVPQrqW.cyU'
            grant_type = 'client_credentials'
        }
        tenant = 'b22f8675-8375-455b-941a-67bee4cf7747'
    }
)

foreach ($config in $configs) {
    Write-Host "`n📝 Testing: $($config.name)" -ForegroundColor Yellow
    
    $uri = "https://login.microsoftonline.com/$($config.tenant)/oauth2/v2.0/token"
    Write-Host "URI: $uri" -ForegroundColor Gray
    Write-Host "Scope: $($config.body.scope)" -ForegroundColor Gray
    
    try {
        $response = Invoke-RestMethod -Uri $uri -Method Post -Body $config.body
        Write-Host "✅ Token acquired successfully!" -ForegroundColor Green
        
        # Decode JWT to see claims
        $tokenParts = $response.access_token.Split('.')
        $header = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenParts[0] + "=="))
        $payload = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenParts[1] + "=="))
        
        Write-Host "Token payload:" -ForegroundColor Cyan
        $payload | ConvertFrom-Json | ConvertTo-Json -Depth 3
        
    } catch {
        Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response: $responseBody" -ForegroundColor Red
        }
    }
}
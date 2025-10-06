param(
  [string]$AppId = "89155d3a-359d-4603-b821-0504395e331f",
  [string]$TenantId = "b22f8675-8375-455b-941a-67bee4cf7747",
  [string]$ApiBase = "https://saaliticketsapi.azurewebsites.net",
  [string]$ScopeName = "Tickets.ReadWrite"   # adjust if different
)

$ErrorActionPreference = "Stop"

Write-Host "== Discover Application ID URI =="
$resourceBase = az ad app show --id $AppId --query "identifierUris[0]" -o tsv
if (-not $resourceBase) { Write-Error "Could not discover identifierUris"; exit 1 }
Write-Host "Resource base: $resourceBase"

$scope      = "$resourceBase/$ScopeName"
$altScope   = "$resourceBase/.default"

Write-Host "== Azure Login (ensure correct tenant) =="
# If you have multiple tenants or no subs, include allow-no-subscriptions
az login --tenant $TenantId --allow-no-subscriptions | Out-Null

Write-Host "== Acquire token for $scope =="
try {
    $token = az account get-access-token --scope $scope --query accessToken -o tsv
} catch {
    Write-Warning "Direct scope failed: $($_.Exception.Message)"
    Write-Host "Trying .default (requires prior consent)"
    $token = az account get-access-token --scope $altScope --query accessToken -o tsv
}

if (-not $token) {
    Write-Host "Interaction still required. Run explicitly:"
    Write-Host "az login --scope $scope --tenant $TenantId --allow-no-subscriptions"
    exit 1
}

Write-Host "Token (first 60): $($token.Substring(0,60))..."

# Decode payload (best effort)
$payloadPart = $token.Split('.')[1]
$padded = $payloadPart + ('=' * ((4 - $payloadPart.Length % 4) % 4))
$payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($padded))
Write-Host "Decoded payload (truncated):"
if ($payloadJson.Length -gt 400) { $payloadJson.Substring(0,400) + "..." } else { $payloadJson }

# Extract scp & aud for confirmation
try {
  $json = $payloadJson | ConvertFrom-Json
  Write-Host "aud: $($json.aud)"
  Write-Host "scp: $($json.scp)"
} catch {}

# Create ticket
$body = @{ title = "Repro Ticket"; description = "Created via script" } | ConvertTo-Json
Write-Host "`n== Create Ticket =="
$createResp = Invoke-WebRequest -Method Post -Uri "$ApiBase/api/tickets" -Headers @{Authorization="Bearer $token"} -Body $body -ContentType "application/json" -ErrorAction SilentlyContinue
Write-Host "HTTP:" $createResp.StatusCode
Write-Host $createResp.Content

if ($createResp.StatusCode -ne 201) {
    Write-Warning "Create failed. Check:
    1) aud matches API Audience
    2) scp contains $ScopeName
    3) API application setting AzureTable__ConnectionString is valid (not UseDevelopmentStorage=true in Azure)"
    exit 2
}

Write-Host "`n== List Tickets =="
$listResp = Invoke-WebRequest -Method Get -Uri "$ApiBase/api/tickets?top=5" -Headers @{Authorization="Bearer $token"} -ErrorAction SilentlyContinue
Write-Host "HTTP:" $listResp.StatusCode
Write-Host $listResp.Content
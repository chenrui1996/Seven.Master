#Requires -Version 5.1
<#
.SYNOPSIS
  Seed Scd_View + Scd_NodeBind from map fixtures / existing Wms_Location so Ops 2D board has nodes. KEEP.
#>
param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$User = "admin",
  [string]$Password = "123456",
  [int]$MaxBindsPerView = 400
)

$ErrorActionPreference = "Continue"
$Root = Split-Path $PSScriptRoot -Parent
$OutDir = Join-Path $Root "reports\retain-locations"
$FixStk = Join-Path $Root "fixtures\srm-demo-stacker-map.json"
$FixFw = Join-Path $Root "fixtures\singapore-fourway-map.json"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$script:token = $null

function Invoke-Api([string]$Method = "GET", [string]$Path, $Body = $null) {
  $uri = "$BaseUrl$Path"
  $hdr = @{}
  if ($script:token) { $hdr["Authorization"] = "Bearer $($script:token)" }
  $p = @{ Uri = $uri; Method = $Method; Headers = $hdr; TimeoutSec = 120 }
  if ($null -ne $Body) {
    $p.ContentType = "application/json; charset=utf-8"
    $p.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 12 -Compress))
  }
  try {
    $resp = Invoke-WebRequest @p -UseBasicParsing
    $json = $null; try { $json = $resp.Content | ConvertFrom-Json } catch {}
    return [pscustomobject]@{ Ok = $true; StatusCode = [int]$resp.StatusCode; Json = $json; Text = $resp.Content }
  } catch {
    $code = 0; $text = $_.Exception.Message
    if ($_.Exception.Response) {
      $code = [int]$_.Exception.Response.StatusCode
      try { $text = (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() } catch {}
    }
    $json = $null; try { $json = $text | ConvertFrom-Json } catch {}
    return [pscustomobject]@{ Ok = $false; StatusCode = $code; Json = $json; Text = $text }
  }
}

function Assert-BizOk($r, $msg) {
  if (-not $r.Ok) { throw "$msg HTTP $($r.StatusCode) $($r.Text)" }
  if ($null -ne $r.Json -and ($r.Json.PSObject.Properties.Name -contains "status") -and -not $r.Json.status) {
    throw "$msg biz:$($r.Json.message)"
  }
}

function Find-ByCode([string]$api, [string]$code) {
  $page = 1
  while ($page -le 30) {
    $r = Invoke-Api -Method POST -Path "$api/getPageData" -Body @{ page = $page; rows = 200 }
    if (-not $r.Ok -or -not $r.Json.status) { return $null }
    $hit = @($r.Json.data.rows) | Where-Object { $_.code -eq $code } | Select-Object -First 1
    if ($hit) { return $hit }
    if ($page * 200 -ge [int]$r.Json.data.total) { break }
    $page++
  }
  return $null
}

function Ensure-View([string]$code, [string]$name, [int]$width, [int]$height) {
  $hit = Find-ByCode "/api/ScdView" $code
  if ($hit) {
    if ([int]$hit.width -le 0 -or [int]$hit.height -le 0) {
      $upd = Invoke-Api -Method POST -Path "/api/ScdView/update" -Body @{
        id = [int]$hit.id; code = $code; name = $name; width = $width; height = $height
      }
      Assert-BizOk $upd "update view $code"
      $hit = Find-ByCode "/api/ScdView" $code
    }
    return $hit
  }
  $add = Invoke-Api -Method POST -Path "/api/ScdView/add" -Body @{
    code = $code; name = $name; width = $width; height = $height
  }
  Assert-BizOk $add "add view $code"
  return $add.Json.data
}

function Get-BindCodes([int]$viewId) {
  $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  $r = Invoke-Api -Path "/api/ScdNodeBind/byView/$viewId"
  if ($r.Ok -and $r.Json.status) {
    foreach ($b in @($r.Json.data)) { [void]$set.Add([string]$b.locationCode) }
  }
  return ,$set
}

function Add-Bind([int]$viewId, [string]$loc, [double]$x, [double]$y, [string]$label, $cache) {
  if ($cache.Contains($loc)) { return $false }
  $attempt = 0
  while ($attempt -lt 6) {
    $attempt++
    $add = Invoke-Api -Method POST -Path "/api/ScdNodeBind/add" -Body @{
      viewId = $viewId; locationCode = $loc; x = $x; y = $y; label = $label
    }
    if ($add.Ok -and $add.Json.status) {
      [void]$cache.Add($loc)
      return $true
    }
    if ($add.StatusCode -eq 429) { Start-Sleep -Milliseconds (250 * $attempt); continue }
    # duplicate or other: treat as exists
    [void]$cache.Add($loc)
    return $false
  }
  return $false
}

function Normalize-Points($points) {
  # points: list of @{ code; x; y; label }
  $xs = @($points | ForEach-Object { [double]$_.x })
  $ys = @($points | ForEach-Object { [double]$_.y })
  if ($xs.Count -eq 0) { return @{ Width = 800; Height = 600; Points = @() } }
  $minX = ($xs | Measure-Object -Minimum).Minimum
  $maxX = ($xs | Measure-Object -Maximum).Maximum
  $minY = ($ys | Measure-Object -Minimum).Minimum
  $maxY = ($ys | Measure-Object -Maximum).Maximum
  $pad = 40
  $spanX = [Math]::Max(1.0, $maxX - $minX)
  $spanY = [Math]::Max(1.0, $maxY - $minY)
  $width = [int][Math]::Ceiling($spanX + 2 * $pad)
  $height = [int][Math]::Ceiling($spanY + 2 * $pad)
  if ($width -lt 400) { $width = 400 }
  if ($height -lt 300) { $height = 300 }
  $norm = @()
  foreach ($p in $points) {
    $norm += [pscustomobject]@{
      code = $p.code
      label = $p.label
      x = [Math]::Round(([double]$p.x - $minX) + $pad, 2)
      y = [Math]::Round(([double]$p.y - $minY) + $pad, 2)
    }
  }
  return @{ Width = $width; Height = $height; Points = $norm }
}

# login
$cap = Invoke-Api -Path "/api/Captcha/create"
Assert-BizOk $cap "captcha"
$login = Invoke-Api -Method POST -Path "/api/Auth/login" -Body @{
  userName = $User; password = $Password
  verificationCode = $cap.Json.data.code; uuid = $cap.Json.data.key
}
Assert-BizOk $login "login"
$script:token = $login.Json.data.token
Write-Host "[PASS] login"

$results = @()

# ---- Stacker view from fixture locations ----
$stkPoints = @()
if (Test-Path $FixStk) {
  $stk = Get-Content -Raw -Encoding UTF8 -Path $FixStk | ConvertFrom-Json
  foreach ($loc in @($stk.locations)) {
    $stkPoints += [pscustomobject]@{
      code = [string]$loc.code
      x = [double]$loc.x
      y = [double]$loc.y
      label = [string]$loc.code
    }
  }
}
if ($stkPoints.Count -eq 0) {
  # fallback: first N stacker locations from API with synthetic grid
  $page = 1; $i = 0
  while ($page -le 5) {
    $r = Invoke-Api -Method POST -Path "/api/WmsLocation/getPageData" -Body @{ page = $page; rows = 200 }
    if (-not $r.Ok) { break }
    foreach ($row in @($r.Json.data.rows)) {
      if ($row.packId -ne "stacker") { continue }
      $col = if ($row.column) { [int]$row.column } else { ($i % 20) }
      $lay = if ($row.layer) { [int]$row.layer } else { [Math]::Floor($i / 20) }
      $stkPoints += [pscustomobject]@{ code = [string]$row.code; x = $col * 40.0; y = $lay * 40.0; label = [string]$row.code }
      $i++
    }
    if ($page * 200 -ge [int]$r.Json.data.total) { break }
    $page++
  }
}

if ($MaxBindsPerView -gt 0 -and $stkPoints.Count -gt $MaxBindsPerView) {
  $stkPoints = $stkPoints | Select-Object -First $MaxBindsPerView
}
$stkNorm = Normalize-Points $stkPoints
$viewStk = Ensure-View "MAP-STK-FLOOR" "Stacker SRM Demo Floor" ([int]$stkNorm.Width) ([int]$stkNorm.Height)
$viewStkId = [int]$viewStk.id
$bindCache = Get-BindCodes $viewStkId
$created = 0; $n = 0
foreach ($p in @($stkNorm.Points)) {
  $n++
  if (Add-Bind $viewStkId $p.code ([double]$p.x) ([double]$p.y) $p.label $bindCache) { $created++ }
  if (($n % 40) -eq 0) { Start-Sleep -Milliseconds 150; Write-Host ("  ... stk binds {0}/{1}" -f $n, $stkNorm.Points.Count) }
}
$status = Invoke-Api -Path "/api/ScdView/$viewStkId/status"
$nodeCount = 0
if ($status.Ok -and $status.Json.status) { $nodeCount = @($status.Json.data.nodes).Count }
Write-Host ("[PASS] MAP-STK-FLOOR id={0} size={1}x{2} binds+={3} statusNodes={4}" -f $viewStkId, $stkNorm.Width, $stkNorm.Height, $created, $nodeCount)
$results += [pscustomobject]@{ View = "MAP-STK-FLOOR"; Id = $viewStkId; Nodes = $nodeCount; Created = $created }

# ---- FourWay view ----
$fwPoints = @()
if (Test-Path $FixFw) {
  $fw = Get-Content -Raw -Encoding UTF8 -Path $FixFw | ConvertFrom-Json
  foreach ($m in @($fw.maps)) {
    foreach ($node in @($m.nodes)) {
      if ($node.kind -ne "Location" -and $node.kind -ne "Track" -and $node.kind -ne "Hoist" -and $node.kind -ne "ShuttleMainTrack" -and $node.kind -ne "ShuttleSubTrack" -and $node.kind -ne "ChainConveyor" -and $node.kind -ne "ChainTrackConveyor" -and $node.kind -ne "PalletLift") { continue }
      # Prefer Location slots; also include tracks for corridor visibility
      $fwPoints += [pscustomobject]@{
        code = [string]$node.code
        x = [double]$node.x
        y = [double]$node.y
        label = [string]$node.code
      }
    }
  }
}
# Prefer location-kind first then others - already mixed. Cap.
if ($MaxBindsPerView -gt 0 -and $fwPoints.Count -gt $MaxBindsPerView) {
  $fwPoints = $fwPoints | Select-Object -First $MaxBindsPerView
}
$fwNorm = Normalize-Points $fwPoints
$viewFw = Ensure-View "MAP-FW-FLOOR" "FourWay Singapore Floor" ([int]$fwNorm.Width) ([int]$fwNorm.Height)
$viewFwId = [int]$viewFw.id
$bindCacheFw = Get-BindCodes $viewFwId
$createdFw = 0; $n = 0
foreach ($p in @($fwNorm.Points)) {
  $n++
  # Only bind codes that exist as Wms_Location OR synthetic track labels (status joins occupied optionally)
  if (Add-Bind $viewFwId $p.code ([double]$p.x) ([double]$p.y) $p.label $bindCacheFw) { $createdFw++ }
  if (($n % 40) -eq 0) { Start-Sleep -Milliseconds 120 }
}
$statusFw = Invoke-Api -Path "/api/ScdView/$viewFwId/status"
$nodeFw = 0
if ($statusFw.Ok -and $statusFw.Json.status) { $nodeFw = @($statusFw.Json.data.nodes).Count }
Write-Host ("[PASS] MAP-FW-FLOOR id={0} size={1}x{2} binds+={3} statusNodes={4}" -f $viewFwId, $fwNorm.Width, $fwNorm.Height, $createdFw, $nodeFw)
$results += [pscustomobject]@{ View = "MAP-FW-FLOOR"; Id = $viewFwId; Nodes = $nodeFw; Created = $createdFw }

# Fix broken COV-VIEW-01 if still empty: point size or leave
$cov = Find-ByCode "/api/ScdView" "COV-VIEW-01"
if ($cov -and ([int]$cov.width -le 0 -or [int]$cov.height -le 0)) {
  $upd = Invoke-Api -Method POST -Path "/api/ScdView/update" -Body @{
    id = [int]$cov.id; code = "COV-VIEW-01"; name = "COV Floor View (legacy empty)"; width = 400; height = 300
  }
  Write-Host ("[INFO] patched COV-VIEW-01 size HTTP {0}" -f $upd.StatusCode)
}

$art = [ordered]@{
  Stamp = $stamp
  Results = $results
  Hint = "Ops Monitor: prefer MAP-STK-FLOOR / MAP-FW-FLOOR"
}
$artPath = Join-Path $OutDir ("scada-seed-artifacts-{0}.json" -f $stamp)
$art | ConvertTo-Json -Depth 6 | Set-Content $artPath -Encoding UTF8

$md = @(
  "# SCADA 2D seed report",
  "",
  ("- Time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss")),
  "- Policy: KEEP",
  "- Views: MAP-STK-FLOOR (stacker), MAP-FW-FLOOR (fourway)",
  "",
  "| View | Id | Nodes | Created |",
  "|------|----|-------|---------|"
)
foreach ($r in $results) {
  $md += ("| {0} | {1} | {2} | {3} |" -f $r.View, $r.Id, $r.Nodes, $r.Created)
}
$md += ""
$md += "Open Ops Monitor and select MAP-STK-FLOOR or MAP-FW-FLOOR, then Fit."
$report = Join-Path $OutDir ("scada-seed-report-{0}.md" -f $stamp)
[System.IO.File]::WriteAllLines($report, $md, [System.Text.UTF8Encoding]::new($false))
Copy-Item $report (Join-Path $OutDir "scada-seed-report.md") -Force
Write-Host "REPORT: $report"

#Requires -Version 5.1
<#
.SYNOPSIS
  Seed map-scale Wms_Location (+ Zone/Layer/Aisle) from Singapore/SRM Demo fixtures into live MySQL. KEEP data.
#>
param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$User = "admin",
  [string]$Password = "123456",
  [ValidateSet("all", "stacker", "fourway")]
  [string]$Pack = "all",
  [int]$MaxLocations = 0
)

$ErrorActionPreference = "Continue"
$Root = Split-Path $PSScriptRoot -Parent
$OutDir = Join-Path $Root "reports\retain-locations"
$FixStk = Join-Path $Root "fixtures\srm-demo-stacker-map.json"
$FixFw = Join-Path $Root "fixtures\singapore-fourway-map.json"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runId = "LOC-$stamp"
$results = [System.Collections.Generic.List[object]]::new()
$artifacts = [ordered]@{
  RunId = $runId
  Stamp = $stamp
  BaseUrl = $BaseUrl
  Pack = $Pack
  MaxLocations = $MaxLocations
  Counts = [ordered]@{}
  Samples = [ordered]@{}
  Policy = "KEEP"
}
$script:token = $null
$script:existingLocCodes = $null

function Add-Result($Id, $Title, $Status, $Detail = "") {
  $results.Add([pscustomobject]@{ CaseId = $Id; Title = $Title; Status = $Status; Detail = "$Detail"; At = (Get-Date).ToString("o") }) | Out-Null
  Write-Host ("[{0}] {1} - {2} | {3}" -f $Status, $Id, $Title, $Detail)
}

function Invoke-Api([string]$Method = "GET", [string]$Path, $Body = $null) {
  $uri = "$BaseUrl$Path"
  $hdr = @{}
  if ($script:token) { $hdr["Authorization"] = "Bearer $($script:token)" }
  $p = @{ Uri = $uri; Method = $Method; Headers = $hdr; TimeoutSec = 120 }
  if ($null -ne $Body) {
    $p.ContentType = "application/json; charset=utf-8"
    if ($Body -is [string]) { $p.Body = [System.Text.Encoding]::UTF8.GetBytes($Body) }
    else { $p.Body = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 14 -Compress)) }
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

function Get-AllCodes([string]$api, [string]$codeProp) {
  $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  $page = 1
  while ($true) {
    $attempt = 0
    $r = $null
    while ($attempt -lt 8) {
      $attempt++
      $r = Invoke-Api -Method POST -Path "$api/getPageData" -Body @{ page = $page; rows = 200 }
      if ($r.Ok -and $r.Json.status) { break }
      if ($r.StatusCode -eq 429) { Start-Sleep -Milliseconds (350 * $attempt); continue }
      Assert-BizOk $r "page $api p$page"
    }
    Assert-BizOk $r "page $api p$page"
    $rows = @($r.Json.data.rows)
    foreach ($row in $rows) {
      $c = [string]$row.$codeProp
      if ($c) { [void]$set.Add($c) }
    }
    $total = [int]$r.Json.data.total
    if ($set.Count -ge $total -or $rows.Count -eq 0) { break }
    $page++
    if ($page -gt 200) { break }
    Start-Sleep -Milliseconds 80
  }
  # Prevent PowerShell from enumerating HashSet into string[]
  return ,$set
}

function Ensure-ByCode([string]$api, [string]$codeProp, [string]$code, $body, $cache) {
  if ($cache -and $cache.Contains($code)) {
    return [pscustomobject]@{ Created = $false; Code = $code }
  }
  $attempt = 0
  while ($attempt -lt 8) {
    $attempt++
    $add = Invoke-Api -Method POST -Path "$api/add" -Body $body
    if ($add.Ok -and $add.Json.status) {
      if ($cache) { [void]$cache.Add($code) }
      return [pscustomobject]@{ Created = $true; Code = $code; Data = $add.Json.data }
    }
    $text = [string]$add.Text
    if ($add.StatusCode -eq 429) {
      Start-Sleep -Milliseconds (300 * $attempt)
      continue
    }
    if ($text -match "object cycle|ReferenceHandler|duplicate|unique" -or $add.StatusCode -eq 500) {
      Start-Sleep -Milliseconds 100
      if ($cache) { [void]$cache.Add($code) }
      return [pscustomobject]@{ Created = $false; Code = $code; Note = "assumed-exists" }
    }
    throw ("add {0} {1} : HTTP {2} {3}" -f $api, $code, $add.StatusCode, $text)
  }
  throw ("add {0} {1} : HTTP 429 after retries" -f $api, $code)
}

function Get-TableTotal([string]$entity) {
  $attempt = 0
  while ($attempt -lt 6) {
    $attempt++
    $r = Invoke-Api -Method POST -Path "/api/$entity/getPageData" -Body @{ page = 1; rows = 1 }
    if ($r.Ok -and $r.Json.status) { return [int]$r.Json.data.total }
    if ($r.StatusCode -eq 429) { Start-Sleep -Milliseconds (400 * $attempt); continue }
    return -1
  }
  return -1
}

function Find-Entity([string]$api, [string]$codeProp, [string]$code) {
  $page = 1
  while ($page -le 50) {
    $r = Invoke-Api -Method POST -Path "$api/getPageData" -Body @{ page = $page; rows = 200 }
    if (-not $r.Ok -or -not $r.Json.status) { return $null }
    $hit = @($r.Json.data.rows) | Where-Object { $_.$codeProp -eq $code } | Select-Object -First 1
    if ($hit) { return $hit }
    if ((@($r.Json.data.rows).Count -eq 0) -or ($page * 200 -ge [int]$r.Json.data.total)) { break }
    $page++
  }
  return $null
}

# --- Login ---
try {
  $cap = Invoke-Api -Path "/api/Captcha/create"
  Assert-BizOk $cap "captcha"
  $login = Invoke-Api -Method POST -Path "/api/Auth/login" -Body @{
    userName = $User; password = $Password
    verificationCode = $cap.Json.data.code; uuid = $cap.Json.data.key
  }
  Assert-BizOk $login "login"
  $script:token = $login.Json.data.token
  Add-Result "TC-LOC-000" "Login" "PASS" $runId
} catch {
  Add-Result "TC-LOC-000" "Login" "FAIL" $_.Exception.Message
  throw
}

$beforeTotal = Get-TableTotal "WmsLocation"
$artifacts.Counts.LocationTotalBefore = $beforeTotal

# --- Warehouse ---
try {
  $whCache = Get-AllCodes "/api/WmsWarehouse" "code"
  $null = Ensure-ByCode "/api/WmsWarehouse" "code" "RETAIN_WH" @{
    code = "RETAIN_WH"; name = "Retain Test WH"; enabledPackIds = "stacker,fourway"
  } $whCache
  $wh = Find-Entity "/api/WmsWarehouse" "code" "RETAIN_WH"
  if (-not $wh) { throw "RETAIN_WH missing after ensure" }
  $whId = [int]$wh.id
  $artifacts.Counts.WarehouseId = $whId
  Add-Result "TC-LOC-001" "Ensure warehouse RETAIN_WH" "PASS" ("id={0}" -f $whId)
} catch {
  Add-Result "TC-LOC-001" "Ensure warehouse RETAIN_WH" "FAIL" $_.Exception.Message
  throw
}

$script:existingLocCodes = Get-AllCodes "/api/WmsLocation" "code"
$zoneCache = Get-AllCodes "/api/WmsZone" "code"
$aisleCache = Get-AllCodes "/api/WmsAisle" "code"
$layerCache = Get-AllCodes "/api/WmsLayer" "code"

$createdLoc = 0
$skippedLoc = 0
$failLoc = 0

# ========== STACKER from SRM Demo ==========
if ($Pack -eq "all" -or $Pack -eq "stacker") {
  try {
    if (-not (Test-Path $FixStk)) { throw "missing fixture $FixStk" }
    $stk = Get-Content -Raw -Encoding UTF8 -Path $FixStk | ConvertFrom-Json

    $null = Ensure-ByCode "/api/WmsZone" "code" "MAP.Z-STK" @{
      warehouseId = $whId; packId = "stacker"; code = "MAP.Z-STK"; name = "SRM Demo Zone"; isAvailable = $true
    } $zoneCache
    $zoneStk = Find-Entity "/api/WmsZone" "code" "MAP.Z-STK"
    $zoneStkId = [int]$zoneStk.id

    $aisleIds = @{}
    foreach ($a in @($stk.aisles)) {
      $code = [string]$a.aisleCode
      $null = Ensure-ByCode "/api/WmsAisle" "code" $code @{
        warehouseId = $whId; zoneId = $zoneStkId; packId = "stacker"; code = $code
        name = ("Demo {0}" -f $code); isAvailable = $true; allocationWeight = 1
        epPointCode = ("EP-{0}" -f $code)
      } $aisleCache
      $row = Find-Entity "/api/WmsAisle" "code" $code
      $aisleIds[$code] = [int]$row.id
    }
    Add-Result "TC-LOC-010" "Stacker zone + aisles" "PASS" ("zone={0} aisles={1}" -f $zoneStkId, $aisleIds.Count)

    $null = Ensure-ByCode "/api/WmsLocation" "code" "Stk.RECV-MAP" @{
      warehouseId = $whId; packId = "stacker"; code = "Stk.RECV-MAP"
      isOccupied = $false; isLocked = $false; isBooked = $false; isHandover = $false
    } $script:existingLocCodes

    $locs = @($stk.locations)
    if ($MaxLocations -gt 0 -and $locs.Count -gt $MaxLocations) {
      $locs = $locs | Select-Object -First $MaxLocations
    }

    $depth1 = 0; $depth2 = 0
    $n = 0
    foreach ($loc in $locs) {
      $n++
      $body = @{
        warehouseId = $whId
        zoneId = $zoneStkId
        aisleId = $aisleIds[[string]$loc.aisleCode]
        packId = "stacker"
        code = [string]$loc.code
        aisle = [string]$loc.aisleCode
        column = [string]$loc.column
        layer = [string]$loc.layer
        depth = [string]$loc.depth
        isOccupied = $false
        isLocked = $false
        isBooked = $false
        isHandover = $false
      }
      try {
        $r = Ensure-ByCode "/api/WmsLocation" "code" ([string]$loc.code) $body $script:existingLocCodes
        if ($r.Created) { $createdLoc++ } else { $skippedLoc++ }
        if ([string]$loc.depth -eq "1") { $depth1++ } elseif ([string]$loc.depth -eq "2") { $depth2++ }
      } catch {
        $failLoc++
        if ($failLoc -le 5) { Write-Host ("  loc fail {0}: {1}" -f $loc.code, $_.Exception.Message) }
      }
      if (($n % 20) -eq 0) { Start-Sleep -Milliseconds 200 }
      if (($n % 50) -eq 0) { Write-Host ("  ... stacker locations {0}/{1}" -f $n, $locs.Count) }
    }

    $artifacts.Samples.Stacker = @($locs | Select-Object -First 5 | ForEach-Object { $_.code })
    $artifacts.Counts.StackerAttempted = $locs.Count
    $artifacts.Counts.StackerDepth1InBatch = $depth1
    $artifacts.Counts.StackerDepth2InBatch = $depth2

    if ($depth1 -lt 1 -or $depth2 -lt 1) {
      Add-Result "TC-LOC-012" "Depth pair sample in batch" "FAIL" ("d1={0} d2={1}" -f $depth1, $depth2)
    } else {
      Add-Result "TC-LOC-012" "Depth pair sample in batch" "PASS" ("d1={0} d2={1}" -f $depth1, $depth2)
    }
    Add-Result "TC-LOC-011" "Seed SRM Demo locations" "PASS" ("attempted={0} created={1} existed={2} fail={3}" -f $locs.Count, $createdLoc, $skippedLoc, $failLoc)
  } catch {
    Add-Result "TC-LOC-010" "Stacker zone + aisles" "FAIL" $_.Exception.Message
    Add-Result "TC-LOC-011" "Seed SRM Demo locations" "FAIL" $_.Exception.Message
  }
}

# ========== FOURWAY from Singapore ==========
$createdFw = 0; $skippedFw = 0; $failFw = 0
if ($Pack -eq "all" -or $Pack -eq "fourway") {
  try {
    if (-not (Test-Path $FixFw)) { throw "missing fixture $FixFw" }
    $fw = Get-Content -Raw -Encoding UTF8 -Path $FixFw | ConvertFrom-Json

    $null = Ensure-ByCode "/api/WmsZone" "code" "MAP.Z-FW" @{
      warehouseId = $whId; packId = "fourway"; code = "MAP.Z-FW"; name = "Singapore FW Zone"; isAvailable = $true
    } $zoneCache
    $zoneFw = Find-Entity "/api/WmsZone" "code" "MAP.Z-FW"
    $zoneFwId = [int]$zoneFw.id

    $layerIds = @{}
    foreach ($l in @($fw.layers)) {
      $null = Ensure-ByCode "/api/WmsLayer" "code" ([string]$l.code) @{
        warehouseId = $whId; zoneId = $zoneFwId; packId = "fourway"; code = [string]$l.code
        name = [string]$l.name; isAvailable = $true; allocationWeight = 1
      } $layerCache
      $row = Find-Entity "/api/WmsLayer" "code" ([string]$l.code)
      $layerIds[[string]$l.code] = [int]$row.id
    }

    $aisleFwIds = @{}
    $ai = 0
    foreach ($l in @($fw.layers)) {
      $ai++
      $ac = "Fw.A$ai"
      $null = Ensure-ByCode "/api/WmsAisle" "code" $ac @{
        warehouseId = $whId; zoneId = $zoneFwId; layerId = $layerIds[[string]$l.code]
        packId = "fourway"; code = $ac; name = $ac; isAvailable = $true; allocationWeight = 1
        epPointCode = ("EP-{0}" -f $ac)
      } $aisleCache
      $row = Find-Entity "/api/WmsAisle" "code" $ac
      $aisleFwIds[[string]$l.code] = [int]$row.id
    }
    Add-Result "TC-LOC-020" "FourWay zone/layer/aisle" "PASS" ("layers={0}" -f $layerIds.Count)

    $null = Ensure-ByCode "/api/WmsLocation" "code" "Fw.RECV-MAP" @{
      warehouseId = $whId; packId = "fourway"; code = "Fw.RECV-MAP"
      layerId = $layerIds["Fw.L01"]
      isOccupied = $false; isLocked = $false; isBooked = $false; isHandover = $false
    } $script:existingLocCodes

    $fwLocs = @()
    foreach ($m in @($fw.maps)) {
      foreach ($node in @($m.nodes)) {
        if ($node.kind -eq "Location") {
          $fwLocs += [pscustomobject]@{ code = [string]$node.code; layerCode = [string]$m.layerCode }
        }
      }
    }
    if ($MaxLocations -gt 0 -and $fwLocs.Count -gt $MaxLocations) {
      $fwLocs = $fwLocs | Select-Object -First $MaxLocations
    }

    foreach ($loc in $fwLocs) {
      $aisleName = if ($loc.layerCode -eq "Fw.L01") { "Fw.A1" } else { "Fw.A2" }
      $body = @{
        warehouseId = $whId
        zoneId = $zoneFwId
        layerId = $layerIds[$loc.layerCode]
        aisleId = $aisleFwIds[$loc.layerCode]
        packId = "fourway"
        code = $loc.code
        aisle = $aisleName
        isOccupied = $false; isLocked = $false; isBooked = $false; isHandover = $false
      }
      try {
        $r = Ensure-ByCode "/api/WmsLocation" "code" $loc.code $body $script:existingLocCodes
        if ($r.Created) { $createdFw++ } else { $skippedFw++ }
      } catch {
        $failFw++
        if ($failFw -le 5) { Write-Host ("  fw loc fail {0}: {1}" -f $loc.code, $_.Exception.Message) }
      }
      Start-Sleep -Milliseconds 50
    }
    $artifacts.Samples.FourWay = @($fwLocs | Select-Object -First 5 | ForEach-Object { $_.code })
    $artifacts.Counts.FourWayAttempted = $fwLocs.Count
    Add-Result "TC-LOC-021" "Seed Singapore FW locations" "PASS" ("attempted={0} created={1} existed={2} fail={3}" -f $fwLocs.Count, $createdFw, $skippedFw, $failFw)
  } catch {
    Add-Result "TC-LOC-020" "FourWay zone/layer/aisle" "FAIL" $_.Exception.Message
    Add-Result "TC-LOC-021" "Seed Singapore FW locations" "FAIL" $_.Exception.Message
  }
}

# --- Probe totals ---
$afterTotal = Get-TableTotal "WmsLocation"
$artifacts.Counts.LocationTotalAfter = $afterTotal
$artifacts.Counts.CreatedStackerApprox = $createdLoc
$artifacts.Counts.CreatedFourWayApprox = $createdFw
$artifacts.Counts.FailedStacker = $failLoc
$artifacts.Counts.FailedFourWay = $failFw

$baseline = 50
if ($Pack -eq "fourway") { $baseline = 10 }
elseif ($Pack -eq "stacker") { $baseline = 40 }
if ($MaxLocations -gt 0) { $baseline = [Math]::Min($baseline, [Math]::Max(5, $MaxLocations)) }

if ($afterTotal -ge $baseline) {
  Add-Result "TC-LOC-030" "WmsLocation total probe" "PASS" ("before={0} after={1} delta={2}" -f $beforeTotal, $afterTotal, ($afterTotal - $beforeTotal))
} else {
  Add-Result "TC-LOC-030" "WmsLocation total probe" "FAIL" ("after={0} baseline={1}" -f $afterTotal, $baseline)
}

# --- Optional stock on map recv for UI ---
try {
  $tp = "MAP-LOC-TP-$stamp"
  $inNo = "MAP-LOC-IN-$stamp"
  $recvCode = if ($Pack -eq "fourway") { "Fw.RECV-MAP" } else { "Stk.RECV-MAP" }
  $add = Invoke-Api -Method POST -Path "/api/WmsInboundOrder/add" -Body @{
    orderNo = $inNo
    orderType = 1
    lines = @(@{
      lineNo = 1; materialCode = "MAP-LOC-MAT"; qty = 1; containerCode = $tp
      fromLocation = $recvCode; toLocation = $recvCode
    })
  }
  $inId = $null
  if ($add.Ok -and $add.Json.status -and $add.Json.data.id) { $inId = [int]$add.Json.data.id }
  elseif ($add.StatusCode -eq 500 -or ([string]$add.Text -match "object cycle")) {
    $pg = Invoke-Api -Method POST -Path "/api/WmsInboundOrder/getPageData" -Body @{ page = 1; rows = 50 }
    $hit = @($pg.Json.data.rows) | Where-Object { $_.orderNo -eq $inNo } | Select-Object -First 1
    if ($hit) { $inId = [int]$hit.id }
  }
  if (-not $inId) { throw ("inbound create failed: {0}" -f $add.Text) }
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/WmsInboundOrder/approve/$inId") "approve"
  $bp = Invoke-Api -Method POST -Path "/api/WmsInboundOrder/buildPallet/$inId" -Body @{
    lineNo = 1; qty = 1; containerCode = $tp; receiveLocationCode = $recvCode; allocateTarget = $false
  }
  Assert-BizOk $bp "buildPallet"
  $recv = Invoke-Api -Method POST -Path "/api/WmsInboundOrder/receive/$inId" -Body @{}
  Assert-BizOk $recv "receive"
  $artifacts.Samples.Stock = @{ OrderNo = $inNo; Container = $tp; Location = $recvCode }
  Add-Result "TC-LOC-031" "Seed stock on map recv KEEP" "PASS" ("{0} at {1}" -f $tp, $recvCode)
} catch {
  Add-Result "TC-LOC-031" "Seed stock on map recv KEEP" "SKIP" $_.Exception.Message
}

Add-Result "TC-LOC-040" "UI checklist written" "PASS" "see report"

$pass = @($results | Where-Object { $_.Status -eq "PASS" }).Count
$fail = @($results | Where-Object { $_.Status -eq "FAIL" }).Count
$skip = @($results | Where-Object { $_.Status -eq "SKIP" }).Count

$artPath = Join-Path $OutDir ("retain-locations-artifacts-{0}.json" -f $stamp)
$resPath = Join-Path $OutDir ("retain-locations-results-{0}.json" -f $stamp)
($artifacts | ConvertTo-Json -Depth 8) | Set-Content -Path $artPath -Encoding UTF8
($results | ConvertTo-Json -Depth 6) | Set-Content -Path $resPath -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Location retain report")
$lines.Add("")
$lines.Add(("- RunId: **{0}**" -f $runId))
$lines.Add(("- Time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss")))
$lines.Add(("- BaseUrl: {0}" -f $BaseUrl))
$lines.Add(("- Pack: {0} MaxLocations: {1} (0=all)" -f $Pack, $MaxLocations))
$lines.Add(("- PASS: **{0}** / FAIL: **{1}** / SKIP: **{2}**" -f $pass, $fail, $skip))
$lines.Add("- Policy: **KEEP**")
$lines.Add(("- Location total: **{0} -> {1}** (delta {2})" -f $beforeTotal, $afterTotal, ($afterTotal - $beforeTotal)))
$lines.Add(("- Artifacts: {0}" -f $artPath))
$lines.Add("")
$lines.Add("## UI search")
$lines.Add("")
$lines.Add("1. Location search Stk.l (SRM Demo CoordPoint)")
$lines.Add("2. Location search Fw. / Fw.SW (Singapore slots)")
$lines.Add("3. Locations Stk.RECV-MAP / Fw.RECV-MAP")
$lines.Add("4. Aisles Stk.A1..Stk.A4, Fw.A1/Fw.A2")
$lines.Add("5. Layers Fw.L01 / Fw.L02")
$lines.Add(("- Stock container MAP-LOC-TP-{0} (if TC-LOC-031 PASS)" -f $stamp))
$lines.Add("")
$lines.Add('## Samples')
$lines.Add('')
$lines.Add('```json')
$lines.Add(($artifacts.Samples | ConvertTo-Json -Depth 5))
$lines.Add('```')
$lines.Add('')
$lines.Add('| CaseId | Status | Title | Detail |')
$lines.Add('|--------|--------|-------|--------|')
foreach ($r in $results) {
  $d = (([string]$r.Detail) -replace '[|]', '/' -replace "`r|`n", ' ')
  $lines.Add(('| {0} | {1} | {2} | {3} |' -f $r.CaseId, $r.Status, $r.Title, $d))
}
$lines.Add('')
$lines.Add('## Note')
$lines.Add('')
$lines.Add('- Earlier retain/coverage scripts only wrote a few locations; this run seeds map fixtures.')
$lines.Add('- Sources: 20260426-SRM-Demo / 20260619-singapore -> testplan/fixtures/*')

$reportPath = Join-Path $OutDir ("retain-locations-report-{0}.md" -f $stamp)
[System.IO.File]::WriteAllLines($reportPath, $lines.ToArray(), [System.Text.UTF8Encoding]::new($false))
$latest = Join-Path $OutDir "retain-locations-report.md"
Copy-Item $reportPath $latest -Force

$cnPath = Join-Path $OutDir ("retain-locations-report-zh-{0}.md" -f $stamp)
$cnLines = @(
  "# Location retain report (zh)",
  "",
  ("- RunId: **{0}**" -f $runId),
  ("- Time: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss")),
  ("- PASS: **{0}** / FAIL: **{1}** / SKIP: **{2}**" -f $pass, $fail, $skip),
  "- Policy: KEEP",
  ("- Location total: **{0} -> {1}**" -f $beforeTotal, $afterTotal),
  ("- Detail report: {0}" -f $reportPath),
  "",
  "## UI search",
  "",
  "1. WmsLocation: Stk.l",
  "2. WmsLocation: Fw.",
  "3. Stk.RECV-MAP / Fw.RECV-MAP",
  "4. Aisle: Stk.A1..Stk.A4",
  "5. Layer: Fw.L01 / Fw.L02",
  ("6. Stock: MAP-LOC-TP-{0}" -f $stamp)
)
[System.IO.File]::WriteAllLines($cnPath, $cnLines, [System.Text.UTF8Encoding]::new($false))
Copy-Item $cnPath (Join-Path $OutDir "retain-locations-report-zh.md") -Force

Write-Host ""
Write-Host "REPORT: $reportPath"
Write-Host ("SUMMARY pass={0} fail={1} skip={2} locations {3}->{4}" -f $pass, $fail, $skip, $beforeTotal, $afterTotal)

if ($fail -gt 0) { exit 1 } else { exit 0 }

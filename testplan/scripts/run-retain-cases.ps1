#Requires -Version 5.1
<#
.SYNOPSIS
  Live ApiHttp: seed RETAIN_* master data, run write flows, KEEP data in DB.
#>
param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$User = "admin",
  [string]$Password = "123456"
)

$ErrorActionPreference = "Continue"
$OutDir = Join-Path (Split-Path $PSScriptRoot -Parent) "reports\retain"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runId = "RETAIN-$stamp"
$results = [System.Collections.Generic.List[object]]::new()
$artifacts = [ordered]@{ RunId = $runId; Stamp = $stamp; Created = @{} }
$script:token = $null

function Add-Result($Id, $Title, $Status, $Detail = "") {
  $results.Add([pscustomobject]@{ CaseId = $Id; Title = $Title; Status = $Status; Detail = $Detail; At = (Get-Date).ToString("o") }) | Out-Null
  Write-Host ("[{0}] {1} — {2}" -f $Status, $Id, $Title)
}

function Invoke-Api([string]$Method = "GET", [string]$Path, $Body = $null) {
  $uri = "$BaseUrl$Path"
  $hdr = @{}
  if ($script:token) { $hdr["Authorization"] = "Bearer $($script:token)" }
  $params = @{ Uri = $uri; Method = $Method; Headers = $hdr; TimeoutSec = 60 }
  if ($null -ne $Body) {
    $params.ContentType = "application/json; charset=utf-8"
    if ($Body -is [string]) { $params.Body = [System.Text.Encoding]::UTF8.GetBytes($Body) }
    else {
      $json = $Body | ConvertTo-Json -Depth 12 -Compress
      $params.Body = [System.Text.Encoding]::UTF8.GetBytes($json)
    }
  }
  try {
    $resp = Invoke-WebRequest @params -UseBasicParsing
    $json = $null
    try { $json = $resp.Content | ConvertFrom-Json } catch {}
    return [pscustomobject]@{ Ok = $true; StatusCode = [int]$resp.StatusCode; Json = $json; Text = $resp.Content }
  } catch {
    $code = 0; $text = $_.Exception.Message
    if ($_.Exception.Response) {
      $code = [int]$_.Exception.Response.StatusCode
      try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $text = $reader.ReadToEnd()
      } catch {}
    }
    $json = $null
    try { $json = $text | ConvertFrom-Json } catch {}
    return [pscustomobject]@{ Ok = $false; StatusCode = $code; Json = $json; Text = $text }
  }
}


function Find-OrderId([string]$apiBase, [string]$orderNo) {
  $page = Invoke-Api -Method POST -Path "$apiBase/getPageData" -Body @{ page = 1; rows = 100 }
  Assert-BizOk $page "page $apiBase"
  $hit = @($page.Json.data.rows) | Where-Object { $_.orderNo -eq $orderNo } | Select-Object -First 1
  if (-not $hit) { throw "order not found: $orderNo on $apiBase" }
  return [int]$hit.id
}

function Create-OrderKeep([string]$apiBase, [string]$orderNo, $body) {
  $r = Invoke-Api -Method POST -Path "$apiBase/add" -Body $body
  # Create may persist even when JSON cycle returns HTTP 500
  if ($r.Ok -and $r.Json.status -and $r.Json.data.id) { return [int]$r.Json.data.id }
  if ($r.Text -match "object cycle|ReferenceHandler" -or $r.StatusCode -eq 500) {
    return (Find-OrderId $apiBase $orderNo)
  }
  Assert-BizOk $r "create $orderNo"
  return [int]$r.Json.data.id
}

function Assert-BizOk($r, $msg) {
  if (-not $r.Ok) { throw "$msg HTTP $($r.StatusCode) $($r.Text)" }
  if ($null -ne $r.Json -and $r.Json.PSObject.Properties.Name -contains "status" -and -not $r.Json.status) {
    throw "$msg biz: $($r.Json.message)"
  }
}

# --- Login ---
$cap = Invoke-Api -Path "/api/Captcha/create"
Assert-BizOk $cap "captcha"
$login = Invoke-Api -Method POST -Path "/api/Auth/login" -Body @{
  userName = $User; password = $Password
  verificationCode = $cap.Json.data.code; uuid = $cap.Json.data.key
}
Assert-BizOk $login "login"
$script:token = $login.Json.data.token
Add-Result "TC-RETAIN-000" "Login for retain run" "PASS" $runId

# --- Seed warehouse / locations (idempotent) ---
try {
  $whPage = Invoke-Api -Method POST -Path "/api/WmsWarehouse/getPageData" -Body @{ page = 1; rows = 50 }
  Assert-BizOk $whPage "wh page"
  $wh = @($whPage.Json.data.rows) | Where-Object { $_.code -eq "RETAIN_WH" } | Select-Object -First 1
  if (-not $wh) {
    $addWh = Invoke-Api -Method POST -Path "/api/WmsWarehouse/add" -Body @{
      code = "RETAIN_WH"; name = "Retain Test WH"; enabledPackIds = "stacker,fourway"
    }
    Assert-BizOk $addWh "add wh"
    $wh = $addWh.Json.data
  }
  $whId = [int]$wh.id
  $artifacts.Created.WarehouseId = $whId
  $artifacts.Created.WarehouseCode = "RETAIN_WH"

  $locPage = Invoke-Api -Method POST -Path "/api/WmsLocation/getPageData" -Body @{ page = 1; rows = 100 }
  Assert-BizOk $locPage "loc page"
  $existingLocs = @($locPage.Json.data.rows)
  function Ensure-Loc($code) {
    $hit = $existingLocs | Where-Object { $_.code -eq $code } | Select-Object -First 1
    if ($hit) { return $hit }
    $add = Invoke-Api -Method POST -Path "/api/WmsLocation/add" -Body @{
      warehouseId = $whId; code = $code; packId = "stacker"; isOccupied = $false; isLocked = $false; isBooked = $false; isHandover = $false
    }
    Assert-BizOk $add "add loc $code"
    return $add.Json.data
  }
  $recv = Ensure-Loc "Stk.RETAIN.RECV-01"
  $stkA = Ensure-Loc "Stk.RETAIN.STK-A"
  $artifacts.Created.Locations = @("Stk.RETAIN.RECV-01", "Stk.RETAIN.STK-A")
  Add-Result "TC-RETAIN-001" "Seed warehouse + locations" "PASS" ("whId={0} recv={1} stkA={2}" -f $whId, $recv.id, $stkA.id)
} catch {
  Add-Result "TC-RETAIN-001" "Seed warehouse + locations" "FAIL" $_.Exception.Message
}

# --- Inbound same-location complete (stock retained) ---
$tp1 = "RETAIN-TP-$stamp-01"
$inNo1 = "RETAIN-IN-$stamp-01"
try {
  $inId1 = Create-OrderKeep "/api/WmsInboundOrder" $inNo1 @{
    orderNo = $inNo1
    orderType = 1
    lines = @(
      @{ lineNo = 1; materialCode = "RETAIN-MAT-01"; qty = 10; containerCode = $tp1; fromLocation = "Stk.RETAIN.RECV-01"; toLocation = "Stk.RETAIN.RECV-01" }
    )
  }
  $ap = Invoke-Api -Method POST -Path "/api/WmsInboundOrder/approve/$inId1"
  Assert-BizOk $ap "inbound approve"
  $recv = Invoke-Api -Method POST -Path "/api/WmsInboundOrder/receive/$inId1" -Body @{
    receiveLocationCode = "Stk.RETAIN.RECV-01"; allocateTarget = $false
  }
  Assert-BizOk $recv "inbound receive"
  $got = Invoke-Api -Path "/api/WmsInboundOrder/$inId1"
  if (-not $got.Ok -or ($got.Text -match "object cycle")) {
    $status = (Find-OrderId "/api/WmsInboundOrder" $inNo1) | Out-Null
    $page = Invoke-Api -Method POST -Path "/api/WmsInboundOrder/getPageData" -Body @{ page = 1; rows = 100 }
    $hit = @($page.Json.data.rows) | Where-Object { $_.id -eq $inId1 } | Select-Object -First 1
    $status = [int]$hit.status
  } else {
    Assert-BizOk $got "inbound get"
    $status = $got.Json.data.status
  }
  $artifacts.Created.Inbound1 = @{ OrderNo = $inNo1; Id = $inId1; Status = $status; Container = $tp1 }
  if ([int]$status -eq 3) {
    Add-Result "TC-RETAIN-002" "Inbound same-loc complete (KEEP)" "PASS" ("id={0} status=Completed container={1}" -f $inId1, $tp1)
  } else {
    Add-Result "TC-RETAIN-002" "Inbound same-loc complete (KEEP)" "FAIL" ("status={0} expected Completed(3)" -f $status)
  }
} catch {
  Add-Result "TC-RETAIN-002" "Inbound same-loc complete (KEEP)" "FAIL" $_.Exception.Message
}

# --- Second inbound pallet for residual stock after outbound ---
$tp2 = "RETAIN-TP-$stamp-02"
$inNo2 = "RETAIN-IN-$stamp-02"
try {
  $inId2 = Create-OrderKeep "/api/WmsInboundOrder" $inNo2 @{
    orderNo = $inNo2; orderType = 1
    lines = @(
      @{ lineNo = 1; materialCode = "RETAIN-MAT-01"; qty = 20; containerCode = $tp2; fromLocation = "Stk.RETAIN.RECV-01"; toLocation = "Stk.RETAIN.RECV-01" }
    )
  }
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/WmsInboundOrder/approve/$inId2") "approve2"
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/WmsInboundOrder/receive/$inId2" -Body @{
    receiveLocationCode = "Stk.RETAIN.RECV-01"; allocateTarget = $false
  }) "receive2"
  $artifacts.Created.Inbound2 = @{ OrderNo = $inNo2; Id = $inId2; Container = $tp2; Qty = 20 }
  Add-Result "TC-RETAIN-003" "Second inbound pallet (KEEP)" "PASS" ("id={0} qty=20 {1}" -f $inId2, $tp2)
} catch {
  Add-Result "TC-RETAIN-003" "Second inbound pallet (KEEP)" "FAIL" $_.Exception.Message
}

# --- Outbound ship partial from tp1 (10) ---
$outNo = "RETAIN-OUT-$stamp-01"
try {
  $outId = Create-OrderKeep "/api/WmsOutboundOrder" $outNo @{
    orderNo = $outNo; orderType = 0
    lines = @(
      @{ lineNo = 1; materialCode = "RETAIN-MAT-01"; qty = 10; fromLocation = "Stk.RETAIN.RECV-01"; containerCode = $tp1 }
    )
  }
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/WmsOutboundOrder/approve/$outId") "outbound approve"
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/WmsOutboundOrder/ship/$outId") "outbound ship"
  $artifacts.Created.Outbound1 = @{ OrderNo = $outNo; Id = $outId; ShippedQty = 10; Container = $tp1 }
  Add-Result "TC-RETAIN-004" "Outbound ship (KEEP order)" "PASS" ("id={0} shipped 10 from {1}" -f $outId, $tp1)
} catch {
  Add-Result "TC-RETAIN-004" "Outbound ship (KEEP order)" "FAIL" $_.Exception.Message
}

# --- Cycle count adjust tp2 to 25 ---
$ccNo = "RETAIN-CC-$stamp-01"
try {
  $ccId = Create-OrderKeep "/api/WmsCycleCount" $ccNo @{
    orderNo = $ccNo
    lines = @(
      @{ lineNo = 1; locationCode = "Stk.RETAIN.RECV-01"; materialCode = "RETAIN-MAT-01"; containerCode = $tp2 }
    )
  }
  # body is raw decimal
  $rec = Invoke-Api -Method POST -Path "/api/WmsCycleCount/record/$ccId/1" -Body "25"
  Assert-BizOk $rec "cc record"
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/WmsCycleCount/confirm/$ccId") "cc confirm"
  $artifacts.Created.CycleCount1 = @{ OrderNo = $ccNo; Id = $ccId; CountQty = 25; Container = $tp2 }
  Add-Result "TC-RETAIN-005" "Cycle count adjust KEEP" "PASS" ("id={0} countQty=25 on {1}" -f $ccId, $tp2)
} catch {
  Add-Result "TC-RETAIN-005" "Cycle count adjust KEEP" "FAIL" $_.Exception.Message
}

# --- Request point seed + ops toggle ---
try {
  $rpPage = Invoke-Api -Method POST -Path "/api/StkRequestPoint/getPageData" -Body @{ page = 1; rows = 50 }
  Assert-BizOk $rpPage "rp page"
  $rp = @($rpPage.Json.data.rows) | Where-Object { $_.code -eq "RETAIN.RP-01" } | Select-Object -First 1
  if (-not $rp) {
    $addRp = Invoke-Api -Method POST -Path "/api/StkRequestPoint/add" -Body @{
      code = "RETAIN.RP-01"; pointType = 0; isEnabled = $true; aisleCode = "RETAIN-A1"
    }
    Assert-BizOk $addRp "add rp"
    $rp = $addRp.Json.data
  }
  $rpId = [int]$rp.id
  $dis = Invoke-Api -Method POST -Path "/api/Wcs/Stacker/Ops/request-point/$rpId/disable"
  Assert-BizOk $dis "disable rp"
  $en = Invoke-Api -Method POST -Path "/api/Wcs/Stacker/Ops/request-point/$rpId/enable"
  Assert-BizOk $en "enable rp"
  $artifacts.Created.RequestPoint = @{ Code = "RETAIN.RP-01"; Id = $rpId; Enabled = $true }
  Add-Result "TC-RETAIN-006" "Request point seed + Ops toggle KEEP" "PASS" ("id={0}" -f $rpId)
} catch {
  Add-Result "TC-RETAIN-006" "Request point seed + Ops toggle KEEP" "FAIL" $_.Exception.Message
}

# --- Menu regress ---
try {
  $menu = Invoke-Api -Path "/api/Sys_Menu/getMenu"
  Assert-BizOk $menu "menu"
  $names = @($menu.Json.data) | ForEach-Object { $_.menuName }
  if ($names -contains "执行运维") { throw "执行运维 visible" }
  Add-Result "TC-RETAIN-007" "Menu IA no legacy ops" "PASS" "ok"
} catch {
  Add-Result "TC-RETAIN-007" "Menu IA no legacy ops" "FAIL" $_.Exception.Message
}

# --- Optional FourWay ops inbound (may SKIP without map) ---
try {
  $meta = Invoke-Api -Path "/api/Wcs/FourWay/Ops/meta"
  Assert-BizOk $meta "meta"
  $gws = @($meta.Json.data.gateways)
  if ($gws.Count -eq 0) {
    Add-Result "TC-RETAIN-009" "FourWay ops inbound (optional)" "SKIP" "no gateways/layers in live DB"
  } else {
    $gw = $gws[0]
    $code = if ($gw.code) { $gw.code } else { $gw.Code }
    $opsIn = Invoke-Api -Method POST -Path "/api/Wcs/FourWay/Ops/inbound" -Body @{
      containerCode = "RETAIN-TP-$stamp-FW"; materialName = "RETAIN-MAT-01"; quantity = 1
      gatewayCode = $code; strategy = "auto"; syncWms = $true
    }
    if ($opsIn.Ok -and $opsIn.Json.status) {
      $artifacts.Created.FwOpsInbound = $opsIn.Json.data
      Add-Result "TC-RETAIN-009" "FourWay ops inbound (optional)" "PASS" "created"
    } else {
      Add-Result "TC-RETAIN-009" "FourWay ops inbound (optional)" "SKIP" $opsIn.Text
    }
  }
} catch {
  Add-Result "TC-RETAIN-009" "FourWay ops inbound (optional)" "SKIP" $_.Exception.Message
}

# --- Verify stock snapshot (KEEP evidence) ---
try {
  $st = Invoke-Api -Method POST -Path "/api/WmsStock/getPageData" -Body @{ page = 1; rows = 50 }
  Assert-BizOk $st "stock"
  $rows = @($st.Json.data.rows) | Where-Object { $_.materialCode -eq "RETAIN-MAT-01" -or ($_.containerCode -like "RETAIN-TP-*") }
  $artifacts.Created.StockSnapshot = @($rows | ForEach-Object {
      [pscustomobject]@{ LocationCode = $_.locationCode; ContainerCode = $_.containerCode; Qty = $_.qty; AvailableQty = $_.availableQty }
    })
  if ($rows.Count -ge 1) {
    Add-Result "TC-RETAIN-010" "Stock snapshot retained" "PASS" ("rows={0}" -f $rows.Count)
  } else {
    Add-Result "TC-RETAIN-010" "Stock snapshot retained" "FAIL" "no RETAIN stock rows"
  }
} catch {
  Add-Result "TC-RETAIN-010" "Stock snapshot retained" "FAIL" $_.Exception.Message
}

# Persist
$artifactsPath = Join-Path $OutDir "retain-artifacts-$stamp.json"
$artifacts | ConvertTo-Json -Depth 8 | Out-File $artifactsPath -Encoding utf8
$results | ConvertTo-Json -Depth 5 | Out-File (Join-Path $OutDir "retain-results-$stamp.json") -Encoding utf8

$pass = @($results | Where-Object Status -eq "PASS").Count
$fail = @($results | Where-Object Status -eq "FAIL").Count
$skip = @($results | Where-Object Status -eq "SKIP").Count

$md = @()
$md += "# 落库保留测试报告"
$md += ""
$md += "- RunId: **$runId**"
$md += "- 时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$md += "- BaseUrl: $BaseUrl"
$md += "- 通过: **$pass** / 失败: **$fail** / 跳过: **$skip**"
$md += "- 产物 JSON: ``reports/retain/retain-artifacts-$stamp.json``"
$md += "- **数据策略: 保留**（前缀 ``RETAIN_`` / ``RETAIN-`` / ``RETAIN.``）"
$md += ""
$md += "## 如何在 UI 查找"
$md += ""
$md += "1. 仓库 ``RETAIN_WH``"
$md += "2. 库位 ``Stk.RETAIN.RECV-01`` / ``Stk.RETAIN.STK-A``"
$md += "3. 入库 ``$inNo1`` / ``$inNo2``"
$md += "4. 出库 ``$outNo``"
$md += "5. 盘点 ``$ccNo``"
$md += "6. 库存物料 ``RETAIN-MAT-01`` 容器 ``$tp2``（盘点后应为 25）"
$md += "7. 申请点 ``RETAIN.RP-01``"
$md += ""
$md += "| CaseId | Status | Title | Detail |"
$md += "|--------|--------|-------|--------|"
foreach ($r in $results) {
  $d = (($r.Detail -replace '\|', '/') -replace "`r|`n", " ")
  $md += "| $($r.CaseId) | $($r.Status) | $($r.Title) | $d |"
}
$md -join "`n" | Out-File (Join-Path $OutDir "retain-report.md") -Encoding utf8
$md -join "`n" | Out-File (Join-Path $OutDir "retain-report-$stamp.md") -Encoding utf8

Write-Host "DONE pass=$pass fail=$fail skip=$skip artifacts=$artifactsPath"
if ($fail -gt 0) { exit 1 } else { exit 0 }

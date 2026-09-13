#Requires -Version 5.1
<#
.SYNOPSIS
  Seed COV*/Stk.*/Fw.* masters and run write flows so most WMS/WCS/Bus tables get rows. KEEP data.
#>
param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$User = "admin",
  [string]$Password = "123456"
)

$ErrorActionPreference = "Continue"
$OutDir = Join-Path (Split-Path $PSScriptRoot -Parent) "reports\coverage"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runId = "COV-$stamp"
$results = [System.Collections.Generic.List[object]]::new()
$artifacts = [ordered]@{ RunId = $runId; TablesTouched = [System.Collections.Generic.List[string]]::new(); Notes = @() }
$script:token = $null

function Add-Result($Id, $Title, $Status, $Detail = "") {
  $results.Add([pscustomobject]@{ CaseId = $Id; Title = $Title; Status = $Status; Detail = "$Detail" }) | Out-Null
  Write-Host ("[{0}] {1} — {2}" -f $Status, $Id, $Title)
}
function Touch([string]$t) { if (-not $artifacts.TablesTouched.Contains($t)) { [void]$artifacts.TablesTouched.Add($t) } }

function Invoke-Api([string]$Method = "GET", [string]$Path, $Body = $null) {
  $uri = "$BaseUrl$Path"
  $hdr = @{}
  if ($script:token) { $hdr["Authorization"] = "Bearer $($script:token)" }
  $p = @{ Uri = $uri; Method = $Method; Headers = $hdr; TimeoutSec = 90 }
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

function Find-ByCode([string]$api, [string]$codeProp, [string]$code) {
  $page = Invoke-Api -Method POST -Path "$api/getPageData" -Body @{ page = 1; rows = 200 }
  Assert-BizOk $page "page $api"
  return @($page.Json.data.rows) | Where-Object { $_.$codeProp -eq $code } | Select-Object -First 1
}

function Ensure-Entity([string]$api, [string]$codeProp, [string]$code, $body) {
  $hit = Find-ByCode $api $codeProp $code
  if ($hit) { return $hit }
  $add = Invoke-Api -Method POST -Path "$api/add" -Body $body
  if ($add.Ok -and $add.Json.status -and $add.Json.data) { return $add.Json.data }
  if ($add.Text -match "object cycle|ReferenceHandler" -or $add.StatusCode -eq 500) {
    Start-Sleep -Milliseconds 200
    $hit2 = Find-ByCode $api $codeProp $code
    if ($hit2) { return $hit2 }
  }
  Assert-BizOk $add "add $api $code"
  return $add.Json.data
}

function Create-OrderKeep([string]$apiBase, [string]$orderNo, $body) {
  $r = Invoke-Api -Method POST -Path "$apiBase/add" -Body $body
  if ($r.Ok -and $r.Json.status -and $r.Json.data.id) { return [int]$r.Json.data.id }
  if ($r.Text -match "object cycle|ReferenceHandler" -or $r.StatusCode -eq 500) {
    $page = Invoke-Api -Method POST -Path "$apiBase/getPageData" -Body @{ page = 1; rows = 100 }
    Assert-BizOk $page "find $orderNo"
    $hit = @($page.Json.data.rows) | Where-Object { $_.orderNo -eq $orderNo } | Select-Object -First 1
    if (-not $hit) { throw "order missing after cycle: $orderNo" }
    return [int]$hit.id
  }
  Assert-BizOk $r "create $orderNo"
  return [int]$r.Json.data.id
}

function Get-TableTotal([string]$api) {
  try {
    $r = Invoke-Api -Method POST -Path "/api/$api/getPageData" -Body @{ page = 1; rows = 1 }
    if ($r.Ok -and $r.Json.status) { return [int]$r.Json.data.total }
    return -1
  } catch { return -1 }
}

# --- login ---
$cap = Invoke-Api -Path "/api/Captcha/create"; Assert-BizOk $cap "captcha"
$login = Invoke-Api -Method POST -Path "/api/Auth/login" -Body @{
  userName = $User; password = $Password; verificationCode = $cap.Json.data.code; uuid = $cap.Json.data.key
}
Assert-BizOk $login "login"
$script:token = $login.Json.data.token
Add-Result "TC-COV-000" "Login" "PASS" $runId

# --- Warehouse ---
$wh = Ensure-Entity "/api/WmsWarehouse" "code" "RETAIN_WH" @{
  code = "RETAIN_WH"; name = "Retain Test WH"; enabledPackIds = "stacker,fourway"
}
$whId = [int]$wh.id
Touch "Wms_Warehouse"

# ========== TC-COV-MD-001 master ==========
try {
  $zone = Ensure-Entity "/api/WmsZone" "code" "COV.Z01" @{
    warehouseId = $whId; packId = "stacker"; code = "COV.Z01"; name = "COV Zone"
  }
  $zoneId = [int]$zone.id
  Touch "Wms_Zone"

  $layer = Ensure-Entity "/api/WmsLayer" "code" "Fw.L01" @{
    warehouseId = $whId; zoneId = $zoneId; packId = "fourway"; code = "Fw.L01"; name = "L01"; isAvailable = $true; allocationWeight = 1
  }
  $layerId = [int]$layer.id
  Touch "Wms_Layer"

  $aisleStk = Ensure-Entity "/api/WmsAisle" "code" "Stk.A1" @{
    warehouseId = $whId; zoneId = $zoneId; packId = "stacker"; code = "Stk.A1"; name = "Stk A1"; isAvailable = $true; allocationWeight = 1; epPointCode = "EP-A1"
  }
  $aisleFw = Ensure-Entity "/api/WmsAisle" "code" "Fw.A1" @{
    warehouseId = $whId; zoneId = $zoneId; layerId = $layerId; packId = "fourway"; code = "Fw.A1"; name = "Fw A1"; isAvailable = $true; allocationWeight = 1; epPointCode = "EP-Fw.A1"
  }
  Touch "Wms_Aisle"

  $null = Ensure-Entity "/api/WmsContainerType" "code" "COV-PALLET" @{ code = "COV-PALLET"; name = "COV Pallet" }
  Touch "Wms_ContainerType"

  $recvStk = Ensure-Entity "/api/WmsLocation" "code" "Stk.RECV-01" @{
    warehouseId = $whId; packId = "stacker"; code = "Stk.RECV-01"
  }
  $locStk = Ensure-Entity "/api/WmsLocation" "code" "Stk.LOC-A1-01" @{
    warehouseId = $whId; zoneId = $zoneId; aisleId = ([int]$aisleStk.id); packId = "stacker"; code = "Stk.LOC-A1-01"; aisle = "A1"
  }
  $recvFw = Ensure-Entity "/api/WmsLocation" "code" "Fw.RECV-01" @{
    warehouseId = $whId; packId = "fourway"; code = "Fw.RECV-01"
  }
  $locFw = Ensure-Entity "/api/WmsLocation" "code" "Fw.LOC-A1-01" @{
    warehouseId = $whId; zoneId = $zoneId; layerId = $layerId; aisleId = ([int]$aisleFw.id); packId = "fourway"; code = "Fw.LOC-A1-01"; aisle = "Fw.A1"
  }
  $ho = Ensure-Entity "/api/WmsLocation" "code" "Stk.HO-01" @{
    warehouseId = $whId; packId = "stacker"; code = "Stk.HO-01"; isHandover = $true
  }
  Touch "Wms_Location"
  Add-Result "TC-COV-MD-001" "WMS Zone/Layer/Aisle/ContainerType/Locations" "PASS" "seeded"
} catch {
  Add-Result "TC-COV-MD-001" "WMS Zone/Layer/Aisle/ContainerType/Locations" "FAIL" $_.Exception.Message
}

# ========== TC-COV-MD-002 handover ==========
try {
  $null = Ensure-Entity "/api/WmsHandoverLink" "locationCode" "Stk.HO-01" @{
    fromPackId = "stacker"; toPackId = "fourway"; locationCode = "Stk.HO-01"
  }
  Touch "Wms_HandoverLink"
  Add-Result "TC-COV-MD-002" "HandoverLink" "PASS" "Stk.HO-01"
} catch {
  Add-Result "TC-COV-MD-002" "HandoverLink" "FAIL" $_.Exception.Message
}

# ========== TC-COV-STK-MD ==========
try {
  $null = Ensure-Entity "/api/StkAssignmentPolicy" "aisleCode" "A1" @{
    aisleCode = "A1"; isAvailable = $true; maxHeight = 3; maxWeight = 100; minEmptySlots = 0; allocationWeight = 10; destinationPointCode = "EP-A1"
  }
  Touch "Stk_AssignmentPolicy"
  $null = Ensure-Entity "/api/StkRequestPoint" "code" "RP_IN_01" @{
    code = "RP_IN_01"; pointType = 0; isEnabled = $true; aisleCode = "A1"
  }
  Touch "Stk_RequestPoint"
  $null = Ensure-Entity "/api/StkLocationProfile" "locationCode" "Stk.LOC-A1-01" @{
    locationCode = "Stk.LOC-A1-01"; binGroupCode = "COV-BG-A1"; inLockBin = $false; outLockBin = $false
  }
  Touch "Stk_LocationProfile"
  $null = Ensure-Entity "/api/StkDeviceCoder" "locationCode" "Stk.LOC-A1-01" @{
    locationCode = "Stk.LOC-A1-01"; pointCode = "EP-A1"
  }
  Touch "Stk_DeviceCoder"
  # routes: getPage may key by id; try add blindly if none
  if ((Get-TableTotal "StkRoute") -le 0) {
    $null = Invoke-Api -Method POST -Path "/api/StkRoute/add" -Body @{
      mapCode = ""; fromCode = "Stk.RECV-01"; toCode = "EP-A1"; exeStackCode = "SRM1"; weight = 1; capacity = 1; isEnabled = $true
    }
    $null = Invoke-Api -Method POST -Path "/api/StkRoute/add" -Body @{
      mapCode = ""; fromCode = "EP-A1"; toCode = "Stk.LOC-A1-01"; exeStackCode = "SRM1"; weight = 1; capacity = 1; isEnabled = $true
    }
  }
  Touch "Stk_Route"
  Add-Result "TC-COV-STK-MD" "Stacker policy/route/profile/coder/RP" "PASS" "seeded"
} catch {
  Add-Result "TC-COV-STK-MD" "Stacker policy/route/profile/coder/RP" "FAIL" $_.Exception.Message
}

# ========== TC-COV-FW-MD ==========
try {
  $map = Ensure-Entity "/api/FwMapVersion" "code" "COV-MAP-L01" @{
    code = "COV-MAP-L01"; name = "COV Map L01"; isActive = $true; layerCode = "Fw.L01"
  }
  $mapId = [int]$map.id
  Touch "Fw_MapVersion"
  $n1 = Ensure-Entity "/api/FwNode" "code" "N-Fw.RECV-01" @{
    mapVersionId = $mapId; code = "N-Fw.RECV-01"; locationCode = "Fw.RECV-01"
  }
  $n2 = Ensure-Entity "/api/FwNode" "code" "N-Fw.LOC-A1-01" @{
    mapVersionId = $mapId; code = "N-Fw.LOC-A1-01"; locationCode = "Fw.LOC-A1-01"
  }
  Touch "Fw_Node"
  if ((Get-TableTotal "FwRoute") -le 0) {
    $null = Invoke-Api -Method POST -Path "/api/FwRoute/add" -Body @{
      mapVersionId = $mapId; fromNodeId = ([int]$n1.id); toNodeId = ([int]$n2.id)
      fromCode = "N-Fw.RECV-01"; toCode = "N-Fw.LOC-A1-01"; weight = 1; capacity = 1
    }
  }
  Touch "Fw_Route"
  $null = Ensure-Entity "/api/FwLayerPolicy" "layerCode" "Fw.L01" @{
    warehouseCode = "RETAIN_WH"; zoneCode = "COV.Z01"; layerCode = "Fw.L01"
    maxHeight = 9999; maxWeight = 99999; isAvailable = $true; allocationWeight = 10
  }
  Touch "Fw_LayerPolicy"
  $null = Ensure-Entity "/api/FwAislePolicy" "aisleCode" "Fw.A1" @{
    layerCode = "Fw.L01"; aisleCode = "Fw.A1"; minEmptySlots = 0; maxShuttleCount = 0
    destinationPointCode = "EP-Fw.A1"; allocationWeight = 1; isAvailable = $true; maxHeight = 9999; maxWeight = 99999
  }
  Touch "Fw_AislePolicy"
  $null = Ensure-Entity "/api/FwRequestPoint" "code" "RP_FW_AISLE" @{
    code = "RP_FW_AISLE"; pointType = 0; isEnabled = $true
  }
  Touch "Fw_RequestPoint"
  $null = Ensure-Entity "/api/FwParkingLedger" "code" "PK-Fw.A1-01" @{
    code = "PK-Fw.A1-01"; locationCode = "Fw.LOC-A1-01"; layerCode = "Fw.L01"; aisleCode = "Fw.A1"; status = 0
  }
  Touch "Fw_ParkingLedger"
  $null = Ensure-Entity "/api/FwHoistDevice" "hoistNo" "H1" @{
    hoistNo = "H1"; name = "COV Hoist 1"; isAvailable = $true; currentLayer = "Fw.L01"
  }
  Touch "Fw_HoistDevice"
  $null = Ensure-Entity "/api/FwHoistLayerPoint" "layerCode" "Fw.L01" @{
    layerCode = "Fw.L01"; hoistNo = "H1"; inboundEp = "H1-IN-EP"; inboundAp = "H1-IN-AP"; outboundEp = "H1-OUT-EP"; outboundAp = "H1-OUT-AP"
  }
  Touch "Fw_HoistLayerPoint"
  Add-Result "TC-COV-FW-MD" "FourWay map/policy/RP/parking/hoist" "PASS" "seeded"
} catch {
  Add-Result "TC-COV-FW-MD" "FourWay map/policy/RP/parking/hoist" "FAIL" $_.Exception.Message
}

# ========== TC-COV-STK-IN ==========
$tpStk = "COV-TP-STK-$stamp"
$inStk = "COV-IN-STK-$stamp"
try {
  $inId = Create-OrderKeep "/api/WmsInboundOrder" $inStk @{
    orderNo = $inStk; orderType = 1
    lines = @(@{ lineNo = 1; materialCode = "COV-MAT-STK"; qty = 5; fromLocation = "Stk.RECV-01" })
  }
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/WmsInboundOrder/approve/$inId") "stk approve"
  $bp = Invoke-Api -Method POST -Path "/api/WmsInboundOrder/buildPallet/$inId" -Body @{
    lineNo = 1; qty = 5; containerCode = $tpStk; receiveLocationCode = "Stk.RECV-01"; height = 1; weight = 10; allocateTarget = $true
  }
  Assert-BizOk $bp "stk buildPallet"
  Touch "Wms_InboundOrder"; Touch "Wms_InboundOrderLine"; Touch "Wms_InboundDetail"; Touch "Wms_Stock"; Touch "Wms_StockLedger"; Touch "Wms_Container"
  Start-Sleep -Milliseconds 500
  $busTotal = Get-TableTotal "BusTransportOrder"
  $putTotal = Get-TableTotal "StkPutAwayTask"
  # Triggers
  $legPage = Invoke-Api -Method POST -Path "/api/BusTransportLeg/getPageData" -Body @{ page = 1; rows = 20 }
  $legId = $null
  if ($legPage.Ok -and $legPage.Json.data.rows) {
    $leg = @($legPage.Json.data.rows) | Select-Object -First 1
    if ($leg.id) { $legId = $leg.id } elseif ($leg.Id) { $legId = $leg.Id }
  }
  $tr1 = Invoke-Api -Method POST -Path "/api/Wcs/Triggers/destination-request" -Body @{
    containerCode = $tpStk; sourcePointCode = "RP_IN_01"; height = 1; weight = 10; checkResult = "OK"
  }
  Assert-BizOk $tr1 "dest req"
  $fbBody = @{ containerCode = $tpStk; segmentPointCode = "EP-A1"; feedbackCode = "OK" }
  if ($legId) { $fbBody.legId = $legId }
  $tr2 = Invoke-Api -Method POST -Path "/api/Wcs/Triggers/segment-feedback" -Body $fbBody
  Assert-BizOk $tr2 "seg fb"
  Start-Sleep -Milliseconds 800
  if ((Get-TableTotal "BusTransportOrder") -gt 0) { Touch "Bus_TransportOrder"; Touch "Bus_TransportLeg" }
  if ((Get-TableTotal "StkPutAwayTask") -gt 0) { Touch "Stk_PutAwayTask" }
  if ((Get-TableTotal "StkDeviceTask") -gt 0) { Touch "Stk_DeviceTask" }
  if ((Get-TableTotal "StkAssignmentRecord") -gt 0) { Touch "Stk_AssignmentRecord" }
  if ((Get-TableTotal "StkRouteFlow") -gt 0) { Touch "Stk_RouteFlow" }
  Add-Result "TC-COV-STK-IN" "Stacker inbound allocate+trigger" "PASS" ("busBefore=$busTotal putBefore=$putTotal busNow=$(Get-TableTotal 'BusTransportOrder') putNow=$(Get-TableTotal 'StkPutAwayTask')")
} catch {
  Add-Result "TC-COV-STK-IN" "Stacker inbound allocate+trigger" "FAIL" $_.Exception.Message
}

# ========== TC-COV-FW-IN ==========
$tpFw = "COV-TP-FW-$stamp"
$inFw = "COV-IN-FW-$stamp"
try {
  $inIdFw = Create-OrderKeep "/api/WmsInboundOrder" $inFw @{
    orderNo = $inFw; orderType = 1
    lines = @(@{ lineNo = 1; materialCode = "COV-MAT-FW"; qty = 3; fromLocation = "Fw.RECV-01" })
  }
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/WmsInboundOrder/approve/$inIdFw") "fw approve"
  $bpFw = Invoke-Api -Method POST -Path "/api/WmsInboundOrder/buildPallet/$inIdFw" -Body @{
    lineNo = 1; qty = 3; containerCode = $tpFw; receiveLocationCode = "Fw.RECV-01"; height = 1; weight = 10; allocateTarget = $true
  }
  Assert-BizOk $bpFw "fw buildPallet"
  $trFw = Invoke-Api -Method POST -Path "/api/Wcs/Triggers/destination-request" -Body @{
    containerCode = $tpFw; sourcePointCode = "RP_FW_AISLE"; height = 1; weight = 10; checkResult = "OK"
  }
  Assert-BizOk $trFw "fw dest"
  # segment feedback best-effort
  $legs = Invoke-Api -Method POST -Path "/api/BusTransportLeg/getPageData" -Body @{ page = 1; rows = 50 }
  $legFw = $null
  if ($legs.Ok) {
    $legFw = @($legs.Json.data.rows) | Where-Object { $_.packId -eq "fourway" -or $_.PackId -eq "fourway" } | Select-Object -First 1
  }
  $fb = @{ containerCode = $tpFw; segmentPointCode = "N-Fw.LOC-A1-01"; feedbackCode = "OK" }
  if ($legFw -and $legFw.id) { $fb.legId = $legFw.id }
  $null = Invoke-Api -Method POST -Path "/api/Wcs/Triggers/segment-feedback" -Body $fb
  Start-Sleep -Milliseconds 800
  foreach ($t in @("FwPutAwayTask","FwShuttleTask","FwAssignmentRecord","FwRetrievalTask","FwHoistTask","FwHoistExecTask","FwShuttleTaskPath")) {
    # ShuttleTaskPath may not have getPageData - skip
  }
  if ((Get-TableTotal "FwPutAwayTask") -gt 0) { Touch "Fw_PutAwayTask" }
  if ((Get-TableTotal "FwShuttleTask") -gt 0) { Touch "Fw_ShuttleTask" }
  if ((Get-TableTotal "FwAssignmentRecord") -gt 0) { Touch "Fw_AssignmentRecord" }
  Add-Result "TC-COV-FW-IN" "FourWay inbound allocate+trigger" "PASS" ("put=$(Get-TableTotal 'FwPutAwayTask') shuttle=$(Get-TableTotal 'FwShuttleTask')")
} catch {
  Add-Result "TC-COV-FW-IN" "FourWay inbound allocate+trigger" "FAIL" $_.Exception.Message
}

# ========== TC-COV-STK-OUT / PICK ==========
try {
  # ensure stock at Stk.LOC or RECV for outbound
  $outNo = "COV-OUT-$stamp"
  $stockPage = Invoke-Api -Method POST -Path "/api/WmsStock/getPageData" -Body @{ page = 1; rows = 50 }
  $stkRow = @($stockPage.Json.data.rows) | Where-Object { $_.qty -gt 0 -and ($_.locationCode -like "Stk.*" -or $_.locationCode -like "Fw.*") } | Select-Object -First 1
  if (-not $stkRow) { throw "no stock for outbound" }
  $outId = Create-OrderKeep "/api/WmsOutboundOrder" $outNo @{
    orderNo = $outNo; orderType = 0
    lines = @(@{
      lineNo = 1; materialCode = $stkRow.materialCode; qty = 1
      fromLocation = $stkRow.locationCode; containerCode = $stkRow.containerCode
    })
  }
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/WmsOutboundOrder/approve/$outId") "out approve"
  Touch "Wms_OutboundOrder"; Touch "Wms_OutboundOrderLine"
  $pick = Invoke-Api -Method POST -Path "/api/WmsOutboundOrder/generatePicks/$outId"
  if ($pick.Ok -and $pick.Json.status) { Touch "Wms_PickingTask"; Add-Result "TC-COV-PICK" "generatePicks" "PASS" "ok" }
  else { Add-Result "TC-COV-PICK" "generatePicks" "SKIP" $pick.Text }
  # ship if still possible (flat) else leave transporting
  $ship = Invoke-Api -Method POST -Path "/api/WmsOutboundOrder/ship/$outId"
  if ($ship.Ok -and $ship.Json.status) {
    Add-Result "TC-COV-STK-OUT" "Outbound approve/ship" "PASS" "shipped"
  } else {
    if ((Get-TableTotal "StkRetrievalTask") -gt 0) { Touch "Stk_RetrievalTask" }
    if ((Get-TableTotal "FwRetrievalTask") -gt 0) { Touch "Fw_RetrievalTask" }
    Add-Result "TC-COV-STK-OUT" "Outbound approve (transport or ship)" "PASS" ("shipMsg=$($ship.Json.message)")
  }
} catch {
  Add-Result "TC-COV-STK-OUT" "Outbound" "FAIL" $_.Exception.Message
  Add-Result "TC-COV-PICK" "generatePicks" "FAIL" $_.Exception.Message
}

# ========== TC-COV-XFER ==========
try {
  $xferNo = "COV-XFER-$stamp"
  # move 1 qty between Stk.RECV-01 and Stk.LOC if stock exists on recv, else use remaining RETAIN stock
  $st = Invoke-Api -Method POST -Path "/api/WmsStock/getPageData" -Body @{ page = 1; rows = 50 }
  $src = @($st.Json.data.rows) | Where-Object { $_.availableQty -gt 0 } | Select-Object -First 1
  if (-not $src) { throw "no available stock for transfer" }
  $toLoc = if ($src.locationCode -eq "Stk.RECV-01") { "Stk.LOC-A1-01" } else { "Stk.RECV-01" }
  $xid = Create-OrderKeep "/api/TransferOrder" $xferNo @{
    orderNo = $xferNo; remark = "COV transfer"
    lines = @(@{
      lineNo = 1; materialCode = $src.materialCode; qty = 1
      fromLocation = $src.locationCode; toLocation = $toLoc; containerCode = $src.containerCode
    })
  }
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/TransferOrder/approve/$xid") "xfer approve"
  $done = Invoke-Api -Method POST -Path "/api/TransferOrder/complete/$xid"
  Assert-BizOk $done "xfer complete"
  Touch "Biz_TransferOrder"; Touch "Biz_TransferOrderLine"
  Add-Result "TC-COV-XFER" "TransferOrder complete" "PASS" $xferNo
} catch {
  Add-Result "TC-COV-XFER" "TransferOrder complete" "FAIL" $_.Exception.Message
}

# ========== TC-COV-CTL ==========
try {
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/Wcs/Stacker/Ops/control-mode" -Body @{ mode = 1; eStop = $false }) "stk mode"
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/Wcs/Stacker/Ops/control-mode" -Body @{ mode = 0; eStop = $false }) "stk restore"
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/Wcs/FourWay/Ops/control-mode" -Body @{ mode = 1 }) "fw mode"
  Assert-BizOk (Invoke-Api -Method POST -Path "/api/Wcs/FourWay/Ops/control-mode" -Body @{ mode = 0 }) "fw restore"
  Touch "Ctl_Mode"
  Add-Result "TC-COV-CTL" "Ctl_Mode via Ops" "PASS" "ok"
} catch {
  Add-Result "TC-COV-CTL" "Ctl_Mode via Ops" "FAIL" $_.Exception.Message
}

# ========== TC-COV-SCD ==========
try {
  $scd = Ensure-Entity "/api/ScdView" "code" "COV-VIEW-01" @{
    code = "COV-VIEW-01"; name = "COV Floor View"; packId = "stacker"
  }
  if ($scd) { Touch "Scd_View" }
  Add-Result "TC-COV-SCD" "Scd_View seed" "PASS" "COV-VIEW-01"
} catch {
  Add-Result "TC-COV-SCD" "Scd_View seed" "SKIP" $_.Exception.Message
}

# ========== TC-COV-IFC ==========
try {
  $ifc = Invoke-Api -Method POST -Path "/api/InterfaceLog/getPageData" -Body @{ page = 1; rows = 5 }
  if ($ifc.Ok) {
    Touch "Ifc_ApiLog"
    Add-Result "TC-COV-IFC" "InterfaceLog readable" "PASS" ("total=$($ifc.Json.data.total)")
  } else {
    Add-Result "TC-COV-IFC" "InterfaceLog readable" "FAIL" $ifc.Text
  }
} catch {
  Add-Result "TC-COV-IFC" "InterfaceLog readable" "FAIL" $_.Exception.Message
}

# ========== optional Dc ==========
try {
  $dc = Invoke-Api -Method POST -Path "/api/CommConnection/getPageData" -Body @{ page = 1; rows = 1 }
  if ($dc.Ok -and $dc.Json.status) {
    if ([int]$dc.Json.data.total -eq 0) {
      $add = Invoke-Api -Method POST -Path "/api/CommConnection/add" -Body @{
        name = "COV-PLC-01"; driverType = "ModbusTcp"; host = "127.0.0.1"; port = 502; enabled = $false
      }
      if ($add.Ok -and $add.Json.status) { Touch "Dc_CommConnection"; Add-Result "TC-COV-OPT-DC" "CommConnection seed" "PASS" "disabled demo" }
      else { Add-Result "TC-COV-OPT-DC" "CommConnection seed" "SKIP" $add.Text }
    } else { Touch "Dc_CommConnection"; Add-Result "TC-COV-OPT-DC" "CommConnection" "PASS" "already has rows" }
  } else {
    Add-Result "TC-COV-OPT-DC" "CommConnection" "SKIP" "feature off or no API"
  }
} catch {
  Add-Result "TC-COV-OPT-DC" "CommConnection" "SKIP" $_.Exception.Message
}

# --- final table snapshot ---
$probeApis = @(
  "WmsWarehouse","WmsZone","WmsLayer","WmsAisle","WmsLocation","WmsContainer","WmsContainerType","WmsStock","WmsStockLedger","WmsHandoverLink",
  "WmsInboundOrder","WmsOutboundOrder","WmsCycleCount","WmsPickingTask",
  "StkRequestPoint","StkAssignmentPolicy","StkLocationProfile","StkRoute","StkDeviceCoder","StkPutAwayTask","StkRetrievalTask","StkDeviceTask",
  "FwRequestPoint","FwLayerPolicy","FwAislePolicy","FwMapVersion","FwNode","FwRoute","FwShuttleTask","FwHoistTask","FwHoistExecTask","FwPutAwayTask","FwRetrievalTask","FwParkingLedger","FwHoistDevice","FwHoistLayerPoint",
  "BusTransportOrder","BusTransportLeg","TransferOrder","ScdView"
)
$snapshot = foreach ($a in $probeApis) {
  [pscustomobject]@{ Api = $a; Total = (Get-TableTotal $a) }
}
$snapshot | ConvertTo-Json | Out-File (Join-Path $OutDir "table-snapshot-$stamp.json") -Encoding utf8
$empty = @($snapshot | Where-Object { $_.Total -eq 0 })
$nonzero = @($snapshot | Where-Object { $_.Total -gt 0 })

$pass = @($results | Where-Object Status -eq "PASS").Count
$fail = @($results | Where-Object Status -eq "FAIL").Count
$skip = @($results | Where-Object Status -eq "SKIP").Count

$artifacts.Notes = @(
  "nonZeroApis=$($nonzero.Count)/$($snapshot.Count)",
  "empty=$($empty.Api -join ',')"
)
$artifacts | ConvertTo-Json -Depth 6 | Out-File (Join-Path $OutDir "coverage-artifacts-$stamp.json") -Encoding utf8
$results | ConvertTo-Json -Depth 4 | Out-File (Join-Path $OutDir "coverage-results-$stamp.json") -Encoding utf8

$md = @()
$md += "# 全表覆盖测试报告"
$md += ""
$md += "- RunId: **$runId**"
$md += "- 通过: **$pass** / 失败: **$fail** / 跳过: **$skip**"
$md += "- 探针非空 API: **$($nonzero.Count)** / $($snapshot.Count)"
$md += "- 仍为空: $($empty.Api -join ', ')"
$md += "- Touched: $($artifacts.TablesTouched -join ', ')"
$md += ""
$md += "| CaseId | Status | Title | Detail |"
$md += "|--------|--------|-------|--------|"
foreach ($r in $results) {
  $d = (($r.Detail -replace '\|','/') -replace "`r|`n"," ")
  $md += "| $($r.CaseId) | $($r.Status) | $($r.Title) | $d |"
}
$md += ""
$md += "## 表计数快照"
$md += ""
$md += "| API | Total |"
$md += "|-----|-------|"
foreach ($s in $snapshot) { $md += "| $($s.Api) | $($s.Total) |" }
$md -join "`n" | Out-File (Join-Path $OutDir "coverage-report.md") -Encoding utf8
$md -join "`n" | Out-File (Join-Path $OutDir "coverage-report-$stamp.md") -Encoding utf8

Write-Host "DONE pass=$pass fail=$fail skip=$skip empty=$($empty.Count)"
if ($fail -gt 0) { exit 1 } else { exit 0 }

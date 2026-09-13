#Requires -Version 5.1
<#
.SYNOPSIS
  Live ApiHttp runner against localhost:5000, mapped to testplan CaseIds.
.OUTPUTS
  reports/api/api-results.json, api-report.md
#>
param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$User = "admin",
  [string]$Password = "123456",
  [string]$OutDir = ""
)

$ErrorActionPreference = "Continue"
if (-not $OutDir) {
  $OutDir = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) "Seven.Master\testplan\reports\api"
  # Fix: script lives in Seven.Master/testplan/scripts
  $OutDir = Join-Path (Split-Path $PSScriptRoot -Parent) "reports\api"
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$results = [System.Collections.Generic.List[object]]::new()
$script:token = $null

function Add-Result {
  param($Id, $Title, $Status, $Automation = "ApiHttp", $Detail = "", $Evidence = "")
  $results.Add([pscustomobject]@{
    CaseId = $Id; Title = $Title; Status = $Status; Automation = $Automation
    Detail = $Detail; Evidence = $Evidence; At = (Get-Date).ToString("o")
  }) | Out-Null
  Write-Host ("[{0}] {1} — {2}" -f $Status, $Id, $Title)
}

function Invoke-Api {
  param(
    [string]$Method = "GET",
    [string]$Path,
    [object]$Body = $null,
    [hashtable]$Headers = @{},
    [switch]$Raw
  )
  $uri = "$BaseUrl$Path"
  $hdr = @{} + $Headers
  if ($script:token) { $hdr["Authorization"] = "Bearer $($script:token)" }
  $params = @{ Uri = $uri; Method = $Method; Headers = $hdr; TimeoutSec = 30 }
  if ($null -ne $Body) {
    $params.ContentType = "application/json"
    $params.Body = if ($Body -is [string]) { $Body } else { $Body | ConvertTo-Json -Depth 10 -Compress }
  }
  try {
    $resp = Invoke-WebRequest @params -UseBasicParsing
    $json = $null
    try { $json = $resp.Content | ConvertFrom-Json } catch {}
    return [pscustomobject]@{ Ok = $true; StatusCode = [int]$resp.StatusCode; Json = $json; Text = $resp.Content }
  } catch {
    $code = 0
    $text = $_.Exception.Message
    if ($_.Exception.Response) {
      $code = [int]$_.Exception.Response.StatusCode
      try {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $text = $reader.ReadToEnd()
      } catch {}
    }
    $json = $null
    try { $json = $text | ConvertFrom-Json } catch {}
    return [pscustomobject]@{ Ok = $false; StatusCode = $code; Json = $json; Text = $text }
  }
}

function Assert-True($cond, $msg) {
  if (-not $cond) { throw $msg }
}

# --- Auth ---
$health = Invoke-Api -Path "/api/Health"
if ($health.Ok -and $health.Json.status) {
  Add-Result "TC-AUTH-API-001" "Health 探针" "PASS" "ApiHttp" "Seven API is running"
} else {
  Add-Result "TC-AUTH-API-001" "Health 探针" "FAIL" "ApiHttp" $health.Text
}

$cap = Invoke-Api -Path "/api/Captcha/create"
$capOk = $cap.Ok -and $cap.Json.data.key -and $cap.Json.data.code
if ($capOk) {
  Add-Result "TC-AUTH-API-002" "Captcha/create 返回 key+code" "PASS" "ApiHttp" ("key={0} code={1}" -f $cap.Json.data.key, $cap.Json.data.code)
} else {
  Add-Result "TC-AUTH-API-002" "Captcha/create 返回 key+code" "FAIL" "ApiHttp" $cap.Text
}

$loginBody = @{
  userName = $User; password = $Password
  verificationCode = $cap.Json.data.code; uuid = $cap.Json.data.key
}
$login = Invoke-Api -Method POST -Path "/api/Auth/login" -Body $loginBody
if ($login.Ok -and $login.Json.status -and $login.Json.data.token) {
  $script:token = $login.Json.data.token
  Add-Result "TC-AUTH-API-003" "登录 admin+验证码" "PASS" "ApiHttp" "token length=$($script:token.Length)"
} else {
  Add-Result "TC-AUTH-API-003" "登录 admin+验证码" "FAIL" "ApiHttp" $login.Text
  # fallback without captcha
  $login2 = Invoke-Api -Method POST -Path "/api/Auth/login" -Body @{ userName = $User; password = $Password }
  if ($login2.Ok -and $login2.Json.data.token) {
    $script:token = $login2.Json.data.token
    Add-Result "TC-AUTH-API-003b" "登录无验证码回退" "PASS" "ApiHttp" "used fallback"
  }
}

$bad = Invoke-Api -Method POST -Path "/api/Auth/login" -Body @{ userName = $User; password = "wrong-password-xyz" }
if ($bad.Ok -and $bad.Json -and -not $bad.Json.status) {
  Add-Result "TC-AUTH-API-004" "错误密码拒绝" "PASS" "ApiHttp" $bad.Json.message
} else {
  Add-Result "TC-AUTH-API-004" "错误密码拒绝" "FAIL" "ApiHttp" $bad.Text
}

# --- Features ---
$feat = Invoke-Api -Path "/api/config/features"
try {
  Assert-True ($feat.Ok -and $feat.Json.status) "features failed"
  $d = $feat.Json.data
  Assert-True ($d.wms -eq $true) "wms off"
  Assert-True ($d.orchestrationBus -eq $true) "bus off"
  Assert-True ($d.wcsPacks.stacker -eq $true) "stacker off"
  Assert-True ($d.wcsPacks.fourWay -eq $true) "fourWay off"
  Assert-True ($d.captcha -eq $true) "captcha expected on in live"
  Add-Result "TC-X-013" "Features API 与期望包开关" "PASS" "ApiHttp" ($d | ConvertTo-Json -Compress -Depth 5)
} catch {
  Add-Result "TC-X-013" "Features API 与期望包开关" "FAIL" "ApiHttp" $_.Exception.Message
}

# --- Menu IA ---
$menu = Invoke-Api -Path "/api/Sys_Menu/getMenu"
$menuList = Invoke-Api -Path "/api/Sys_Menu/getMenuList"
try {
  Assert-True $menu.Ok "getMenu fail"
  $items = @($menu.Json.data)
  $tns = $items | ForEach-Object { $_.tableName }
  Assert-True ($tns -contains "StkOpsFolder") "missing StkOpsFolder"
  Assert-True ($tns -contains "FwOpsFolder") "missing FwOpsFolder"
  Assert-True ($tns -contains "StkOpsMonitor") "missing StkOpsMonitor"
  Assert-True ($tns -contains "FwOpsMonitor") "missing FwOpsMonitor"
  Assert-True ($tns -contains "FwOpsInbound") "missing FwOpsInbound"
  Assert-True ($tns -contains "FwOpsShuttle") "missing FwOpsShuttle"
  Assert-True ($tns -contains "FwOpsHoist") "missing FwOpsHoist"
  Assert-True ($tns -contains "FwOpsCtlMode") "missing FwOpsCtlMode"
  Assert-True ($tns -contains "StkOpsSrm") "missing StkOpsSrm"
  Assert-True ($tns -contains "StkOpsRequest") "missing StkOpsRequest"
  Assert-True ($tns -contains "StkOpsCtlMode") "missing StkOpsCtlMode"
  Assert-True ($tns -notcontains "WcsOpsFolder") "WcsOpsFolder still in getMenu"
  $names = $items | ForEach-Object { $_.menuName }
  Assert-True ($names -notcontains "执行运维") "执行运维 still visible in getMenu"
  Add-Result "TC-X-012" "Dual：双边运维 + 无执行运维(getMenu)" "PASS" "ApiHttp" "ops folders present; 执行运维 hidden"
  Add-Result "TC-OPS-FW-020" "五页菜单可见(API)" "PASS" "ApiHttp" "FwOps* present"
  Add-Result "TC-OPS-STK-008" "运维四页菜单可见(API)" "PASS" "ApiHttp" "StkOps* present"
} catch {
  Add-Result "TC-X-012" "Dual：双边运维 + 无执行运维(getMenu)" "FAIL" "ApiHttp" $_.Exception.Message
  Add-Result "TC-OPS-FW-020" "五页菜单可见(API)" "FAIL" "ApiHttp" $_.Exception.Message
  Add-Result "TC-OPS-STK-008" "运维四页菜单可见(API)" "FAIL" "ApiHttp" $_.Exception.Message
}

try {
  Assert-True $menuList.Ok "getMenuList fail"
  $all = @($menuList.Json.data)
  $legacy = $all | Where-Object { $_.menuName -eq "执行运维" -or $_.tableName -eq "WcsOpsFolder" }
  Assert-True ($null -ne $legacy -and @($legacy).Count -ge 1) "legacy row missing"
  foreach ($row in @($legacy)) {
    Assert-True ([int]$row.enable -eq 0) ("legacy enable!=0 id={0}" -f $row.menu_Id)
  }
  Add-Result "TC-X-040" "DbSeeder 退役执行运维(enable=0)" "PASS" "ApiHttp" ("rows={0}" -f @($legacy).Count)
} catch {
  Add-Result "TC-X-040" "DbSeeder 退役执行运维(enable=0)" "FAIL" "ApiHttp" $_.Exception.Message
}

try {
  $ifc = @($menu.Json.data) | Where-Object { $_.tableName -eq "IfcApiLog" } | Select-Object -First 1
  Assert-True ($null -ne $ifc) "IfcApiLog missing"
  $sys = @($menu.Json.data) | Where-Object { $_.menu_Id -eq $ifc.parentId } | Select-Object -First 1
  Assert-True ($null -ne $sys -and $sys.menuName -eq "系统管理") "Ifc not under 系统管理"
  Add-Result "TC-WMS-041" "接口日志菜单在系统管理" "PASS" "ApiHttp" ("parentId={0}" -f $ifc.parentId)
} catch {
  Add-Result "TC-WMS-041" "接口日志菜单在系统管理" "FAIL" "ApiHttp" $_.Exception.Message
}

# Permission presence for ops
try {
  $perms = @($login.Json.data.permissions)
  $need = @(
    "FwOpsMonitor.Search","FwOpsInbound.Search","FwOpsShuttle.Search","FwOpsHoist.Search","FwOpsCtlMode.Search",
    "StkOpsMonitor.Search","StkOpsSrm.Search","StkOpsRequest.Search","StkOpsCtlMode.Search"
  )
  $missing = $need | Where-Object { $perms -notcontains $_ }
  if ($missing.Count -eq 0) {
    Add-Result "TC-X-041" "权限码 Search 可进运维页" "PASS" "ApiHttp" "all Search perms present"
  } else {
    Add-Result "TC-X-041" "权限码 Search 可进运维页" "FAIL" "ApiHttp" ("missing: {0}" -f ($missing -join ","))
  }
} catch {
  Add-Result "TC-X-041" "权限码 Search 可进运维页" "FAIL" "ApiHttp" $_.Exception.Message
}

# --- FourWay Ops APIs ---
function Test-GetOk($id, $title, $path) {
  $r = Invoke-Api -Path $path
  if ($r.Ok -and $r.StatusCode -eq 200 -and ($null -eq $r.Json -or $r.Json.status -ne $false)) {
    Add-Result $id $title "PASS" "ApiHttp" ("HTTP {0}" -f $r.StatusCode)
    return $r
  } else {
    Add-Result $id $title "FAIL" "ApiHttp" ("HTTP {0} {1}" -f $r.StatusCode, $r.Text)
    return $r
  }
}

$meta = Test-GetOk "TC-OPS-FW-001" "meta 与 canAcceptLegs" "/api/Wcs/FourWay/Ops/meta"
$boardFw = Test-GetOk "TC-OPS-FW-002" "board 活动集" "/api/Wcs/FourWay/Ops/board"
Test-GetOk "TC-OPS-FW-006" "指定货位 map" "/api/Wcs/FourWay/Ops/inbound/pickable-map" | Out-Null
$cmFw = Test-GetOk "TC-OPS-FW-013" "联锁 scope=FourWay GET" "/api/Wcs/FourWay/Ops/control-mode"

# task-tree without id may error — acceptable
$tt = Invoke-Api -Path "/api/Wcs/FourWay/Ops/task-tree"
if ($tt.Ok) {
  if ($tt.Json.status) {
    Add-Result "TC-OPS-FW-003" "task-tree 无参/空树" "PASS" "ApiHttp" "ok empty/tree"
  } else {
    Add-Result "TC-OPS-FW-003" "task-tree 无参返回业务错误" "PASS" "ApiHttp" $tt.Json.message
  }
} else {
  Add-Result "TC-OPS-FW-003" "task-tree 可达" "FAIL" "ApiHttp" $tt.Text
}

# Safe negative: force-complete empty body
$fc = Invoke-Api -Method POST -Path "/api/Wcs/FourWay/Ops/force-complete" -Body @{ targetType = "shuttle"; id = [guid]::Empty }
if ($fc.Ok -and $fc.Json -and -not $fc.Json.status) {
  Add-Result "TC-OPS-FW-010" "强制完成空 GUID 拒批" "PASS" "ApiHttp" $fc.Json.message
} elseif (-not $fc.Ok -and $fc.StatusCode -ge 400) {
  Add-Result "TC-OPS-FW-010" "强制完成空 GUID 拒批" "PASS" "ApiHttp" ("HTTP {0}" -f $fc.StatusCode)
} else {
  Add-Result "TC-OPS-FW-010" "强制完成空 GUID 拒批" "FAIL" "ApiHttp" $fc.Text
}

# Control mode round-trip (restore) — mode is numeric enum (0=Auto,1=Manual,...)
try {
  $before = $cmFw.Json.data
  $set = Invoke-Api -Method POST -Path "/api/Wcs/FourWay/Ops/control-mode" -Body @{ mode = 1; eStop = $false; globalEStop = $false }
  Assert-True ($set.Ok -and $set.Json.status) "set manual fail"
  $get2 = Invoke-Api -Path "/api/Wcs/FourWay/Ops/control-mode"
  Assert-True ([int]$get2.Json.data.mode -eq 1) "mode not Manual after set"
  $restoreMode = 0
  if ($null -ne $before.mode) { $restoreMode = [int]$before.mode }
  $null = Invoke-Api -Method POST -Path "/api/Wcs/FourWay/Ops/control-mode" -Body @{ mode = $restoreMode; eStop = $false; globalEStop = $false }
  Add-Result "TC-X-021" "包专属模式可写 FourWay" "PASS" "ApiHttp" ("set Manual(1) then restore {0}" -f $restoreMode)
} catch {
  Add-Result "TC-X-021" "包专属模式可写 FourWay" "FAIL" "ApiHttp" $_.Exception.Message
}

# Global e-stop toggle restore
try {
  $estopOn = Invoke-Api -Method POST -Path "/api/Wcs/FourWay/Ops/control-mode" -Body @{ globalEStop = $true }
  Assert-True ($estopOn.Ok -and $estopOn.Json.status) "estop on fail"
  $estopOff = Invoke-Api -Method POST -Path "/api/Wcs/FourWay/Ops/control-mode" -Body @{ globalEStop = $false }
  Assert-True ($estopOff.Ok -and $estopOff.Json.status) "estop off fail"
  Add-Result "TC-X-020" "仓级急停可开关(经 FourWay control-mode)" "PASS" "ApiHttp" "toggled globalEStop"
} catch {
  Add-Result "TC-X-020" "仓级急停可开关(经 FourWay control-mode)" "FAIL" "ApiHttp" $_.Exception.Message
}

# --- Stacker Ops ---
Test-GetOk "TC-OPS-STK-001" "board 返回活动任务" "/api/Wcs/Stacker/Ops/board" | Out-Null
$ttStk = Invoke-Api -Path "/api/Wcs/Stacker/Ops/task-tree"
if ($ttStk.Ok) {
  Add-Result "TC-OPS-STK-002" "task-tree 可达" "PASS" "ApiHttp" ($(if ($ttStk.Json.status) { "ok" } else { $ttStk.Json.message }))
} else {
  Add-Result "TC-OPS-STK-002" "task-tree 可达" "FAIL" "ApiHttp" $ttStk.Text
}
$rp = Test-GetOk "TC-OPS-STK-006" "申请点列表" "/api/Wcs/Stacker/Ops/request-points"
Test-GetOk "TC-OPS-STK-007" "联锁 GET Stacker" "/api/Wcs/Stacker/Ops/control-mode" | Out-Null

$fcStk = Invoke-Api -Method POST -Path "/api/Wcs/Stacker/Ops/force-complete" -Body @{ targetType = "device"; id = [guid]::Empty }
if ($fcStk.Ok -and $fcStk.Json -and -not $fcStk.Json.status) {
  Add-Result "TC-OPS-STK-003" "强制完成空 GUID 拒批" "PASS" "ApiHttp" $fcStk.Json.message
} elseif (-not $fcStk.Ok -and $fcStk.StatusCode -ge 400) {
  Add-Result "TC-OPS-STK-003" "强制完成空 GUID 拒批" "PASS" "ApiHttp" ("HTTP {0} validation" -f $fcStk.StatusCode)
} else {
  Add-Result "TC-OPS-STK-003" "强制完成空 GUID 拒批" "FAIL" "ApiHttp" $fcStk.Text
}

# Request point enable/disable if any
try {
  $points = @($rp.Json.data)
  if ($points.Count -gt 0) {
    $pid = $points[0].id; if (-not $pid) { $pid = $points[0].Id }
    $dis = Invoke-Api -Method POST -Path "/api/Wcs/Stacker/Ops/request-point/$pid/disable"
    $en = Invoke-Api -Method POST -Path "/api/Wcs/Stacker/Ops/request-point/$pid/enable"
    if ($dis.Ok -and $en.Ok) {
      Add-Result "TC-OPS-STK-006b" "申请点停用/启用往返" "PASS" "ApiHttp" ("id={0}" -f $pid)
    } else {
      Add-Result "TC-OPS-STK-006b" "申请点停用/启用往返" "FAIL" "ApiHttp" ("dis={0} en={1}" -f $dis.Text, $en.Text)
    }
  } else {
    Add-Result "TC-OPS-STK-006b" "申请点停用/启用往返" "SKIP" "ApiHttp" "no request points in DB"
  }
} catch {
  Add-Result "TC-OPS-STK-006b" "申请点停用/启用往返" "FAIL" "ApiHttp" $_.Exception.Message
}

# --- WMS list smoke ---
function Test-WmsPage($id, $title, $path) {
  $r = Invoke-Api -Method POST -Path $path -Body @{ page = 1; rows = 5 }
  if ($r.Ok -and $r.StatusCode -eq 200) {
    Add-Result $id $title "PASS" "ApiHttp" ("HTTP 200")
  } else {
    Add-Result $id $title "FAIL" "ApiHttp" ("HTTP {0} {1}" -f $r.StatusCode, $r.Text)
  }
}
Test-WmsPage "TC-WMS-API-001" "入库单分页" "/api/WmsInboundOrder/getPageData"
Test-WmsPage "TC-WMS-API-002" "出库单分页" "/api/WmsOutboundOrder/getPageData"
Test-WmsPage "TC-WMS-API-003" "盘点单分页" "/api/WmsCycleCount/getPageData"
Test-WmsPage "TC-WMS-API-004" "库存分页" "/api/WmsStock/getPageData"

# Unauthorized without token
$old = $script:token
$script:token = $null
$unauth = Invoke-Api -Path "/api/Wcs/FourWay/Ops/board"
$script:token = $old
if ($unauth.StatusCode -eq 401) {
  Add-Result "TC-AUTH-API-005" "Ops 无 Token → 401" "PASS" "ApiHttp" "401"
} else {
  Add-Result "TC-AUTH-API-005" "Ops 无 Token → 401" "FAIL" "ApiHttp" ("got {0}" -f $unauth.StatusCode)
}

# Persist
$jsonPath = Join-Path $OutDir "api-results.json"
$results | ConvertTo-Json -Depth 6 | Out-File $jsonPath -Encoding utf8

$pass = @($results | Where-Object Status -eq "PASS").Count
$fail = @($results | Where-Object Status -eq "FAIL").Count
$skip = @($results | Where-Object Status -eq "SKIP").Count
$md = @()
$md += "# ApiHttp 测试报告"
$md += ""
$md += "- 时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$md += "- BaseUrl: $BaseUrl"
$md += "- 通过: **$pass** / 失败: **$fail** / 跳过: **$skip** / 合计: $($results.Count)"
$md += ""
$md += "| CaseId | Status | Title | Detail |"
$md += "|--------|--------|-------|--------|"
foreach ($r in $results) {
  $d = ($r.Detail -replace '\|','/').Replace("`n"," ")
  $md += "| $($r.CaseId) | $($r.Status) | $($r.Title) | $d |"
}
$md -join "`n" | Out-File (Join-Path $OutDir "api-report.md") -Encoding utf8

Write-Host "DONE pass=$pass fail=$fail skip=$skip -> $jsonPath"
if ($fail -gt 0) { exit 1 } else { exit 0 }

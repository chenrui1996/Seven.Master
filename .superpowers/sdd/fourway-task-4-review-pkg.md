# Task 4 review
See report for file list. Key files:

### Seven.Net8/Seven.Tests/Wcs/InboundToFourWayE2ETests.cs

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wcs.Triggers;
using Seven.Infrastructure.Wms;

namespace Seven.Tests.Wcs;

public class InboundToFourWayE2ETests
{
    [Fact]
    public async Task BuildPallet_Allocate_ThenFourWaySimulate_ShouldPutStockAtFwLocation()
    {
        var db = CreateDb();
        SeedWarehouseAndLocations(db);
        SeedFourWayMaster(db);
        await db.SaveChangesAsync();

        var port = new InMemoryEquipmentTriggerPort();
        var stock = new StockService(db);
        var control = new ControlModeService(db);
        var fwPack = new FourWayWcsPack(db, control);
        var stkPack = new StackerWcsPack(db, control);
        var completion = new WmsTransportCompletionHandler(db, stock);
        var bus = new OrchestrationBus(db, new WcsPackResolver([fwPack]), completion);
        var allocator = new FourWayInboundAllocator(db);
        var fwDest = new FourWayDestinationService(db, port, allocator, bus);
        // 鍙屽寘鍏变韩 TriggerPort锛歋tacker 涓嶅緱鍥?Fw 鐢宠鐐?Reject
        var stkDest = new StackerDestinationService(
            db,
            port,
            new StackerAisleAllocator(db),
            new StackerLocationAllocator(db),
            new StackerPathDispatcher(db, port),
            bus);
        fwDest.Subscribe();
        stkDest.Subscribe();

        var resolver = new WcsLocationAllocatorResolver(
            [new FourWayLocationSchema()],
            [allocator]);
        var inbound = new InboundOrderService(db, stock, new BusTransportOrderRequest(bus), resolver);

        var order = await inbound.CreateAsync(new CreateInboundOrderRequest(
            "IN-FW-E2E-001",
            WmsOrderType.Purchase,
            [new InboundLineInput(1, "MAT-FW-01", 10m, FromLocation: "Fw.RECV-01")]));

        await inbound.ApproveAsync(order.Id);
        var detail = await inbound.BuildPalletAsync(order.Id, new BuildPalletRequest(
            LineNo: 1,
            Qty: 10m,
            ContainerCode: "TP-FW-E2E",
            ReceiveLocationCode: "Fw.RECV-01",
            Height: 1,
            Weight: 10));

        detail.TargetLocationCode.Should().Be("Fw.LOC-A1-01");
        detail.AssignedAisle.Should().Be("Fw.A1");
        detail.AssignedLayer.Should().Be("Fw.L01");
        detail.Status.Should().Be(WmsInboundDetailStatus.Transporting);
        detail.PackId.Should().Be(WcsPackIds.FourWay);

        var booked = await db.WmsLocations.SingleAsync(x => x.Code == "Fw.LOC-A1-01");
        booked.IsBooked.Should().BeTrue();

        var afterReceive = await db.WmsInboundOrders.Include(x => x.Lines).SingleAsync();
        afterReceive.Status.Should().Be(WmsOrderStatus.Executing);
        var recvStock = await db.WmsStocks.SingleAsync(x => x.Qty > 0);

### doc/24-WCS四向分配与入库.md

# WCS 鍥涘悜鍒嗛厤涓庡叆搴擄紙F2/F3锛?

鏈枃鎻忚堪鍥涘悜杞﹀寘 **灞傗啋宸封啋浣嶅垎閰?* 涓?**鍏ュ簱 PutAway + SUDR 浠跨湡闂幆**銆? 
鎬荤嚎涓?WMS 绔栧垏瑙?[20-WMS杩佸叆瀹屾暣瀹炵幇](./20-WMS杩佸叆瀹屾暣瀹炵幇.md)锛涘寘寮€鍏宠 [19-WMS涓嶹CS鍖匽(./19-WMS涓嶹CS鍖?md)銆? 
璁捐鍘熺锛歔`design/shuttle-wcs/`](../design/shuttle-wcs/)銆乕`docs/superpowers/specs/2026-08-29-fourway-wcs-f2-f5-design.md`](../docs/superpowers/specs/2026-08-29-fourway-wcs-f2-f5-design.md)銆?

**鐘舵€?*锛氬啿鍒衡憼 F2+F3 宸茶惤鍦帮紙2026-08-29锛夈€傛湭鍚細灞傚唴澶氭瀵昏矾锛團4锛夈€佸嚭搴?鍋滆溅/Hoist锛團5锛夈€丳hase H 鐪熸満閫氳銆?

---

## 1. LES / RCS 鈫?Seven

| RCS / LES | Seven |
|-----------|-------|
| SelectLayer 鈫?SelectAisle 鈫?SelectLocation | `FourWayInboundAllocator` |
| Layer/Aisle 绛栫暐 | `Fw_LayerPolicy` / `Fw_AislePolicy` |
| 鍒嗛厤杞浆 | `Fw_AssignmentRecord`锛堝眰 / `Layer/Aisle`锛?|
| SUDR + DestinationRequest | `IEquipmentTriggerPort` 鈫?`FourWayDestinationService` |
| RequestPoint | `Fw_RequestPoint`锛圠ayer / Aisle / Location锛?|
| 鍏ュ簱浠诲姟 | `Fw_PutAwayTask`锛堟簮锛? 杞婚噺 `Fw_ShuttleTask`锛堣澶囪浇浣擄級 |
| SUDS 涓嬪彂 | `DispatchDestinationAsync`锛涘褰?NG 鈫?`RejectDestinationAsync` |

鐮佸墠缂€ `Fw.*`锛沗PackId=fourway`銆傜瓥鐣ヨ〃鍓嶇紑 `Fw_`锛?*涓?*澶嶇敤 `Stk_AssignmentPolicy`銆?

---

## 2. 杩愯鏃跺簭锛堝叆搴?F3锛?

```text
WMS BuildPallet锛團ourWayInboundAllocator 鈫?Fw.* + Booking锛?
  鈫?Bus Leg 鈫?FourWayWcsPack.AcceptLeg
       路 CanHandle锛氫袱绔?Fw. 鎴栦竴绔凡鐭ヤ氦鎺ワ紱绾?Stk.* 鎷掓帴
       路 鍐?Fw_PutAwayTask(Accepted) + Fw_ShuttleTask(Accepted)
  鈫?SUDR @ LayerRequest / AisleRequest
       路 CheckResult鈮燨K 鈫?Reject + PutAway=Failed
       路 SelectLayer 鈫?SelectAisle 鈫?Dispatch(Ep)
  鈫?锛堝彲閫夛級SUDR @ LocationRequest 鈫?Book 鈫?Dispatch(Bin)
  鈫?SegmentFeedback锛團3 鍗曟鍗冲畬鎴愶級
       路 PutAway/Shuttle Completed 鈫?Bus Leg Complete 鈫?WMS 钀借处
```

鍙屽寘鍏变韩 TriggerPort锛歚StackerDestinationService` 閬囧埌宸插惎鐢ㄧ殑 `Fw_RequestPoint` **闈欓粯蹇界暐**锛堜笉 Reject锛夛紝閬垮厤鍐叉帀 FourWay 宸?Dispatch銆?

---

## 3. 鍒嗛厤瑙勫垯锛團2锛?

### 3.1 灞?`SelectLayer`

- `Fw_LayerPolicy`锛氬彲鐢ㄣ€侀珮閲嶈繃婊わ紱鍙€?Zone
- 瀛樺湪 `Wms_Layer` 涓绘暟鎹椂鎸夊眰鍙敤鎬ц繃婊?
- 鎺掑簭锛歚AllocationWeight` 闄嶅簭 鈫?`Fw_AssignmentRecord.LastAssignedAt` 鍗囧簭 鈫?LayerCode

### 3.2 宸?`SelectAisle`

- `Fw_AislePolicy`锛氬悓灞傘€佸彲鐢ㄣ€侀珮閲嶏紱`MinEmptySlots`锛堟寜**鏈眰**绌洪棽浣嶈鏁帮紝闃茶法灞備覆鐮侊級
- `MaxShuttleCount`锛欶5 鍋滆溅璐︽湰鍓嶄笉纭嫤
- 鎺掑簭锛氭潈閲?鈫?宸疯疆杞褰?鈫?AisleCode
- 缁撴灉 Ep锛歚DestinationPointCode`

### 3.3 璐т綅 `SelectLocation`

- 鍚屼粨銆乣PackId=fourway`銆佸悓宸枫€佹湭鍗犵敤/鏈攣/鏈绾︼紱鍙€?`LayerId` 杩囨护
- 鎺掑簭锛歚Code`锛汢ooking锛歚IsBooked=true`

---

## 4. 琛ㄤ笌杩佺Щ

| 瀵硅薄 | 瑕佺偣 |
|------|------|
| `Fw_LayerPolicy` | 鍚?`AllocationWeight` |
| `Fw_AislePolicy` | Layer+Aisle銆丮inEmpty銆丒p銆佹潈閲?|
| `Fw_AssignmentRecord` | ScopeType=Layer/Aisle |
| `Fw_PutAwayTask` | AssignedLayer/Aisle/Location锛涚姸鎬佹満 |
| `Fw_RequestPoint` | Layer/Aisle/Location 鐢宠 |

杩佺Щ锛歚AddFourWayPack`锛堝強鍚庣画绛栫暐瀛楁澧為噺锛夈€?

---

  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:78:        {
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:79:            // 双包共享 TriggerPort：本包未
知但属已启用四向申请点时静默忽略，避免冲掉 FourWay 已 Dispatch
> Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:80:            var isFourWayPoint = aw
ait _db.FwRequestPoints.AsNoTracking()
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:81:                .AnyAsync(x => x.Is
Enabled && x.Code == trigger.SourcePointCode, ct);
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:82:            if (isFourWayPoint)
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:83:                return;
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:84:
> Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:85:            await RejectAsync(trigg
er, "未知或未启用申请点: " + trigger.SourcePointCode, markFailed: false, ct);
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:86:            return;
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:87:        }
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:88:
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:89:        var putaway = await FindAct
ivePutAwayAsync(trigger.ContainerCode, ct);
  Seven.Net8\Seven.Infrastructure\Wcs\Packs\Stacker\StackerDestinationService.cs:90:        if (putaway == null)




design\shuttle-wcs\02-migration-plan.md:3:状态：F0/F1 已落地（Wms_Layer + 前缀/同仓共存）；**F2/F3 已落地**（Allocator + PutAway/SUDR 入库 E
2E，见 `doc/24`）；F4+ 待实施  
design\shuttle-wcs\02-migration-plan.md:71:| **F2** | `Fw_*Policy` + Allocator 三阶段 | 与 RCS 金样一致 | ✅ |
design\shuttle-wcs\02-migration-plan.md:72:| **F3** | PutAway 任务 + Bus Leg + 仿真 Trigger/地图 Deploy 写 Fw 码 | 入库 E2E | ✅ |




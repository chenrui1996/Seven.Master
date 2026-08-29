### Task 1: Features、表前缀常量与 DI 骨架

**Files:**
- Modify: `Seven.Net8/Seven.Infrastructure/Configuration/AppOptions.cs`
- Create: `Seven.Net8/Seven.Domain/Wcs/TablePrefixes.cs`
- Create: `Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs`
- Modify: `Seven.Net8/Seven.Infrastructure/DependencyInjection.cs`
- Modify: `Seven.Net8/Seven.WebApi/appsettings.json`（及 Development）
- Modify: `Seven.Vue3/src/stores/features.ts`
- Test: `Seven.Net8/Seven.Tests/Wcs/FeatureOptionsWcsTests.cs`

**Interfaces:**
- Consumes: 现有 `FeatureOptions.IsEnabled`
- Produces: `FeatureOptions.Wms`、`OrchestrationBus`、`WcsPacks`（`Stacker`/`FourWay`/`BoxSort`）；`AddSevenWcs(IConfiguration)`；建议增加 `IsWcsPackEnabled(string packName)`

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using Seven.Infrastructure.Configuration;
using Xunit;

namespace Seven.Tests.Wcs;

public class FeatureOptionsWcsTests
{
    [Fact]
    public void IsEnabled_ShouldRead_NestedPack_ViaFlatName_OrExplicit()
    {
        var f = new FeatureOptions
        {
            Wms = true,
            OrchestrationBus = true,
            WcsPacks = new WcsPackFeatureOptions { Stacker = true, FourWay = false }
        };
        f.Wms.Should().BeTrue();
        f.WcsPacks.Stacker.Should().BeTrue();
        f.WcsPacks.FourWay.Should().BeFalse();
    }

    [Fact]
    public void IsWcsPackEnabled_ShouldResolve_PackName()
    {
        var f = new FeatureOptions
        {
            WcsPacks = new WcsPackFeatureOptions { Stacker = true, FourWay = false }
        };
        f.IsWcsPackEnabled("Stacker").Should().BeTrue();
        f.IsWcsPackEnabled("FourWay").Should().BeFalse();
        f.IsWcsPackEnabled("Unknown").Should().BeFalse();
    }
}
```

- [ ] **Step 2: 跑测试确认需扩展类型**

```powershell
dotnet test Seven.Net8/Seven.Tests/Seven.Tests.csproj --filter "FullyQualifiedName~FeatureOptionsWcsTests"
```

Expected: 编译失败（缺少属性）

- [ ] **Step 3: 增加表前缀常量**

```csharp
namespace Seven.Domain.Wcs;

public static class TablePrefixes
{
    public const string Wms = "Wms_";
    public const string Bus = "Bus_";
    public const string Stacker = "Stk_";
    public const string FourWay = "Fw_";
    public const string External = "Ext_";
    public const string InterfaceLog = "Ifc_";
    public const string Control = "Ctl_";
    public const string Scada = "Scd_";
}
```

- [ ] **Step 4: 扩展 FeatureOptions**

在 `AppOptions.cs`：

```csharp
public bool Wms { get; set; }
public bool OrchestrationBus { get; set; }
public WcsPackFeatureOptions WcsPacks { get; set; } = new();

public bool IsWcsPackEnabled(string packName)
{
    if (string.IsNullOrWhiteSpace(packName) || WcsPacks is null) return false;
    var prop = typeof(WcsPackFeatureOptions).GetProperty(
        packName,
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
    if (prop?.PropertyType != typeof(bool)) return false;
    return (bool)(prop.GetValue(WcsPacks) ?? false);
}

public class WcsPackFeatureOptions
{
    public bool Stacker { get; set; }
    public bool FourWay { get; set; }
    public bool BoxSort { get; set; }
}
```

注意：不要把嵌套对象塞进 `IsEnabled`；包开关用 `IsWcsPackEnabled`。

- [ ] **Step 5: `AddSevenWcs` 空壳**

对齐 `AddSevenHotStore` 风格；Phase A 仅读 Features、暂不注册 HostedService。  
在 `AddSevenInfrastructure` 中 `AddSevenDeviceComm` 之后调用 `AddSevenWcs(configuration)`。

- [ ] **Step 6: appsettings + 前端 flags**

`Features` 增加 `Wms`/`OrchestrationBus`/`WcsPacks`（默认 false）。  
`features.ts`：`wms`、`orchestrationBus`、`wcsPacks: { stacker, fourWay, boxSort }`；`menuFeatureMap` 可先占位 `WmsFolder: 'wms'`（若类型需 flatten，用可选布尔 `wcsStacker` 亦可，但优先与后端 JSON 嵌套对齐，因 `GetFeatures` 直接返回 `FeatureOptions`）。

- [ ] **Step 7: 跑测试通过**

Expected: PASS

- [ ] **Step 8: Commit** — **跳过**（本仓库约定：用户未要求则不 commit）

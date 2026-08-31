using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Domain.Entities.Alarm;
using Seven.Domain.Entities.Core;
using Seven.Domain.Entities.System;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Security;

namespace Seven.Infrastructure.Persistence;

/// <summary>
/// 数据库种子数据，首次启动时初始化管理员、角色和基础菜单。
/// </summary>
public static class DbSeeder
{
    /// <summary>默认租户 Id（种子数据与演示账号归属）</summary>
    public const int DefaultTenantId = 1;

    /// <summary>执行种子数据初始化</summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SevenDbContext>>();
        var alarmOptions = scope.ServiceProvider.GetRequiredService<IOptions<AlarmOptions>>().Value;
        var databaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (db.Database.IsRelational())
        {
            if (databaseOptions.MigrateOnStartup)
            {
                logger.LogInformation("Database:MigrateOnStartup=true，正在应用 EF 迁移...");
                await db.Database.MigrateAsync();
            }
            else
            {
                logger.LogInformation(
                    "Database:MigrateOnStartup=false，跳过自动迁移（请在发布流水线执行 dotnet ef database update）");
            }
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        await SeedAlarmCodesAsync(db, alarmOptions, logger);
        await MigrateBoardToBusinessMetadataAsync(db, logger);
        await SeedDefaultTenantAsync(db, logger);
        await DisableBizDeviceDemoMenusAsync(db, logger);

        // 先确保有管理员角色，再种子业务菜单并补 RoleAuth。
        // 旧顺序会在「尚无角色」时创建 WMS/WCS 菜单，导致 getMenu 永远看不到这些项。
        if (!await db.Sys_Users.AnyAsync())
        {
            await SeedAdminUserAndSystemMenusAsync(db, hasher, logger);
        }

        await SeedExtraMenusAndJobsAsync(db, logger);
        await SeedDeviceCommMenusAsync(db, logger);
        await SeedWmsWcsMenusAsync(db, logger);
        await SeedBizMenusAsync(db, logger);
    }

    /// <summary>首次安装：超级管理员 + 系统管理菜单树</summary>
    static async Task SeedAdminUserAndSystemMenusAsync(SevenDbContext db, IPasswordHasher hasher, ILogger logger)
    {
        logger.LogInformation("正在初始化 Seven 种子数据...");

        var adminRole = new Sys_Role
        {
            RoleName = "超级管理员",
            ParentId = 0,
            Enable = 1,
            TenantId = DefaultTenantId,
            CreateDate = DateTime.Now,
        };
        db.Sys_Roles.Add(adminRole);
        await db.SaveChangesAsync();

        var adminUser = new Sys_User
        {
            UserName = "admin",
            UserTrueName = "管理员",
            PasswordHash = hasher.HashPassword("123456"),
            Role_Id = adminRole.Role_Id,
            RoleName = adminRole.RoleName,
            Enable = 1,
            TenantId = DefaultTenantId,
            CreateDate = DateTime.Now,
        };
        db.Sys_Users.Add(adminUser);

        var systemMenu = new Sys_Menu
        {
            ParentId = 0,
            MenuName = "系统管理",
            Icon = "Setting",
            OrderNo = 1,
            Url = null,
            TableName = null,
            Auth = null,
            Enable = 1,
            TenantId = DefaultTenantId,
            CreateDate = DateTime.Now,
        };
        db.Sys_Menus.Add(systemMenu);
        await db.SaveChangesAsync();

        var childMenuDefs = new[]
        {
            new { MenuName = "用户管理", OrderNo = 1, Url = "/Sys_User", TableName = "Sys_User", Auth = "Search,Add,Update,Delete,Import,Export" },
            new { MenuName = "角色管理", OrderNo = 2, Url = "/Sys_Role", TableName = "Sys_Role", Auth = "Search,Add,Update,Delete,Import,Export" },
            new { MenuName = "菜单管理", OrderNo = 3, Url = "/Sys_Menu", TableName = "Sys_Menu", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "部门管理", OrderNo = 4, Url = "/Sys_Department", TableName = "Sys_Department", Auth = "Search,Add,Update,Delete,Import,Export" },
            new { MenuName = "字典管理", OrderNo = 5, Url = "/Sys_Dictionary", TableName = "Sys_Dictionary", Auth = "Search,Add,Update,Delete,Import,Export" },
            new { MenuName = "日志管理", OrderNo = 6, Url = "/Sys_Log", TableName = "Sys_Log", Auth = "Search" },
            new { MenuName = "告警管理", OrderNo = 7, Url = "/Sys_Alarm", TableName = "Sys_Alarm", Auth = "Search,Acknowledge,Clear,Raise" },
            new { MenuName = "代码生成", OrderNo = 8, Url = "/coder", TableName = "Sys_TableInfo", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "工作流定义", OrderNo = 9, Url = "/Sys_WorkFlow", TableName = "Sys_WorkFlow", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "我的审批", OrderNo = 10, Url = "/Sys_WorkFlowTable", TableName = "Sys_WorkFlowTable", Auth = "Search,Audit" },
            new { MenuName = "定时任务", OrderNo = 11, Url = "/Sys_QuartzOptions", TableName = "Sys_QuartzOptions", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "任务日志", OrderNo = 12, Url = "/Sys_QuartzLog", TableName = "Sys_QuartzLog", Auth = "Search" },
            new { MenuName = "表单设计", OrderNo = 13, Url = "/FormDesignOptions", TableName = "FormDesignOptions", Auth = "Search,Add,Update,Delete" },
        };

        var menus = childMenuDefs
            .Select(m => new Sys_Menu
            {
                ParentId = systemMenu.Menu_Id,
                MenuName = m.MenuName,
                Url = m.Url,
                TableName = m.TableName,
                Auth = m.Auth,
                OrderNo = m.OrderNo,
                Enable = 1,
                TenantId = DefaultTenantId,
                CreateDate = DateTime.Now,
            })
            .ToList();
        db.Sys_Menus.AddRange(menus);
        await db.SaveChangesAsync();

        foreach (var menu in menus)
        {
            db.Sys_RoleAuths.Add(new Sys_RoleAuth
            {
                Role_Id = adminRole.Role_Id,
                Menu_Id = menu.Menu_Id,
                AuthValue = menu.Auth,
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("已初始化管理员账号 admin / 123456 与系统菜单");
    }

    /// <summary>给首个角色补菜单授权（已存在则跳过）</summary>
    static async Task EnsureRoleAuthAsync(SevenDbContext db, Sys_Role? adminRole, int menuId, string? authValue)
    {
        if (adminRole == null || menuId <= 0) return;
        var exists = await db.Sys_RoleAuths.AnyAsync(a => a.Role_Id == adminRole.Role_Id && a.Menu_Id == menuId);
        if (exists) return;
        db.Sys_RoleAuths.Add(new Sys_RoleAuth
        {
            Role_Id = adminRole.Role_Id,
            Menu_Id = menuId,
            AuthValue = authValue,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>写入默认租户（幂等）</summary>
    static async Task SeedDefaultTenantAsync(SevenDbContext db, ILogger logger)
    {
        if (await db.Sys_Tenants.AnyAsync())
            return;

        db.Sys_Tenants.Add(new Sys_Tenant
        {
            TenantId = DefaultTenantId,
            Code = "default",
            Name = "默认租户",
            Enable = 1,
            CreateDate = DateTime.Now,
        });
        await db.SaveChangesAsync();
        logger.LogInformation("已写入默认租户 {Code} (TenantId={Id})", "default", DefaultTenantId);
    }

    /// <summary>从 appsettings Alarm:Codes 同步报警码到数据库</summary>
    static async Task SeedAlarmCodesAsync(SevenDbContext db, AlarmOptions options, ILogger logger)
    {
        if (options.Codes.Count == 0)
            return;

        var existing = await db.Sys_AlarmCodes.Select(c => c.Code).ToListAsync();
        var added = 0;

        foreach (var def in options.Codes)
        {
            if (string.IsNullOrWhiteSpace(def.Code) || existing.Contains(def.Code))
                continue;

            db.Sys_AlarmCodes.Add(
                new Sys_AlarmCode
                {
                    Code = def.Code.Trim(),
                    Message = def.Message,
                    Level = def.Level,
                    Category = def.Category,
                    Remark = def.Remark,
                    Enable = 1,
                    CreateDate = DateTime.Now,
                    Creator = "system",
                }
            );
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("已同步 {Count} 条报警码配置", added);
        }
    }

    /// <summary>
    /// 将历史 Board 文件夹/命名空间元数据迁移为 Business（幂等）
    /// </summary>
    static async Task MigrateBoardToBusinessMetadataAsync(SevenDbContext db, ILogger logger)
    {
        var tables = await db.Sys_TableInfos
            .Where(t => t.FolderName == "Board"
                || (t.Namespace != null && t.Namespace.Contains("Entities.Board")))
            .ToListAsync();
        if (tables.Count == 0)
            return;

        foreach (var t in tables)
        {
            if (t.FolderName == "Board")
                t.FolderName = "Business";
            if (!string.IsNullOrEmpty(t.Namespace) && t.Namespace.Contains("Entities.Board"))
                t.Namespace = t.Namespace.Replace("Entities.Board", "Entities.Business");
        }

        await db.SaveChangesAsync();
        logger.LogInformation("已将 {Count} 条代码生成元数据从 Board 迁移为 Business", tables.Count);
    }

    /// <summary>下线 Biz Device/SubDevice Demo 菜单与主子表元数据（幂等）。</summary>
    static async Task DisableBizDeviceDemoMenusAsync(SevenDbContext db, ILogger logger)
    {
        var menus = await db.Sys_Menus
            .Where(m => m.TableName == "Device" || m.TableName == "SubDevice"
                || m.Url == "/Device" || m.Url == "/SubDevice")
            .ToListAsync();
        var disabled = 0;
        foreach (var m in menus.Where(x => x.Enable != 0))
        {
            m.Enable = 0;
            disabled++;
        }

        var details = await db.Sys_TableDetails
            .Where(d => d.ParentTable.ToLower() == "device" && d.ChildTable.ToLower() == "subdevice")
            .ToListAsync();
        if (details.Count > 0)
            db.Sys_TableDetails.RemoveRange(details);

        if (disabled > 0 || details.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation(
                "已下线 Device/SubDevice Demo：菜单 {Menus}，主子表配置 {Details}",
                disabled, details.Count);
        }
    }

    /// <summary>幂等补充工作流/定时任务菜单与示例 Job</summary>
    static async Task SeedExtraMenusAndJobsAsync(SevenDbContext db, ILogger logger)
    {
        var parent = await db.Sys_Menus.FirstOrDefaultAsync(m => m.ParentId == 0 && m.MenuName == "系统管理")
            ?? await db.Sys_Menus.FirstOrDefaultAsync(m => m.ParentId == 0);
        if (parent == null) return;

        var extras = new[]
        {
            new { MenuName = "工作流定义", Url = "/Sys_WorkFlow", TableName = "Sys_WorkFlow", Auth = "Search,Add,Update,Delete", OrderNo = 11 },
            new { MenuName = "我的审批", Url = "/Sys_WorkFlowTable", TableName = "Sys_WorkFlowTable", Auth = "Search,Audit", OrderNo = 12 },
            new { MenuName = "定时任务", Url = "/Sys_QuartzOptions", TableName = "Sys_QuartzOptions", Auth = "Search,Add,Update,Delete", OrderNo = 13 },
            new { MenuName = "任务日志", Url = "/Sys_QuartzLog", TableName = "Sys_QuartzLog", Auth = "Search", OrderNo = 14 },
            new { MenuName = "表单设计", Url = "/FormDesignOptions", TableName = "FormDesignOptions", Auth = "Search,Add,Update,Delete", OrderNo = 15 },
        };

        var adminRole = await db.Sys_Roles.OrderBy(r => r.Role_Id).FirstOrDefaultAsync();
        var added = 0;
        foreach (var m in extras)
        {
            var existing = await db.Sys_Menus.FirstOrDefaultAsync(x => x.Url == m.Url || x.TableName == m.TableName);
            if (existing != null)
            {
                await EnsureRoleAuthAsync(db, adminRole, existing.Menu_Id, m.Auth);
                continue;
            }
            var menu = new Sys_Menu
            {
                ParentId = parent.Menu_Id,
                MenuName = m.MenuName,
                Url = m.Url,
                TableName = m.TableName,
                Auth = m.Auth,
                OrderNo = m.OrderNo,
                Enable = 1,
                CreateDate = DateTime.Now,
            };
            db.Sys_Menus.Add(menu);
            await db.SaveChangesAsync();
            await EnsureRoleAuthAsync(db, adminRole, menu.Menu_Id, m.Auth);
            added++;
        }

        if (!await db.Sys_QuartzOptions.AnyAsync(x => x.TaskName == "cleanup-refresh"))
        {
            db.Sys_QuartzOptions.Add(new Domain.Entities.Quartz.Sys_QuartzOptions
            {
                TaskName = "cleanup-refresh",
                GroupName = "DEFAULT",
                CronExpression = "0 0/30 * * * ?",
                ApiUrl = "cleanup-refresh",
                Enable = 1,
                CreateDate = DateTime.Now,
                Creator = "system",
            });
            await db.SaveChangesAsync();
            logger.LogInformation("已写入示例定时任务 cleanup-refresh");
        }

        if (added > 0)
            logger.LogInformation("已补充菜单 {Count} 项（工作流/审批/定时任务）", added);
    }

    /// <summary>幂等：顶级「设备通讯」目录 + 子菜单；已挂到系统管理下的会迁移过来</summary>
    static async Task SeedDeviceCommMenusAsync(SevenDbContext db, ILogger logger)
    {
        var folder = await db.Sys_Menus.FirstOrDefaultAsync(m =>
            m.ParentId == 0 && (m.MenuName == "设备通讯" || m.TableName == "DeviceCommFolder"));
        if (folder == null)
        {
            folder = new Sys_Menu
            {
                ParentId = 0,
                MenuName = "设备通讯",
                Icon = "Connection",
                OrderNo = 5,
                Url = null,
                TableName = "DeviceCommFolder",
                Auth = null,
                Enable = 1,
                CreateDate = DateTime.Now,
            };
            db.Sys_Menus.Add(folder);
            await db.SaveChangesAsync();
            logger.LogInformation("已创建顶级菜单目录「设备通讯」");
        }
        else if (folder.ParentId != 0 || folder.MenuName != "设备通讯")
        {
            folder.ParentId = 0;
            folder.MenuName = "设备通讯";
            folder.Icon ??= "Connection";
            folder.TableName = "DeviceCommFolder";
            folder.Url = null;
            folder.OrderNo = folder.OrderNo == 0 ? 5 : folder.OrderNo;
            await db.SaveChangesAsync();
        }

        var items = new[]
        {
            new { MenuName = "通讯连接", Url = "/DeviceComm/CommConnection", TableName = "CommConnection", Auth = "Search,Add,Update,Delete", OrderNo = 1 },
            new { MenuName = "通讯点位", Url = "/DeviceComm/CommPoint", TableName = "CommPoint", Auth = "Search,Add,Update,Delete", OrderNo = 2 },
            new { MenuName = "通讯规则", Url = "/DeviceComm/CommRule", TableName = "CommRule", Auth = "Search,Add,Update,Delete", OrderNo = 3 },
            new { MenuName = "通讯运行态", Url = "/DeviceComm/Runtime", TableName = "DeviceComm", Auth = "Search,Update", OrderNo = 4 },
        };

        var adminRole = await db.Sys_Roles.OrderBy(r => r.Role_Id).FirstOrDefaultAsync();
        var added = 0;
        var moved = 0;
        foreach (var m in items)
        {
            var existing = await db.Sys_Menus.FirstOrDefaultAsync(x =>
                x.Url == m.Url || x.TableName == m.TableName);
            if (existing != null)
            {
                var dirty = false;
                if (existing.ParentId != folder.Menu_Id)
                {
                    existing.ParentId = folder.Menu_Id;
                    dirty = true;
                    moved++;
                }
                if (existing.OrderNo != m.OrderNo)
                {
                    existing.OrderNo = m.OrderNo;
                    dirty = true;
                }
                if (dirty)
                    await db.SaveChangesAsync();
                await EnsureRoleAuthAsync(db, adminRole, existing.Menu_Id, m.Auth);
                continue;
            }

            var menu = new Sys_Menu
            {
                ParentId = folder.Menu_Id,
                MenuName = m.MenuName,
                Url = m.Url,
                TableName = m.TableName,
                Auth = m.Auth,
                OrderNo = m.OrderNo,
                Enable = 1,
                CreateDate = DateTime.Now,
            };
            db.Sys_Menus.Add(menu);
            await db.SaveChangesAsync();
            await EnsureRoleAuthAsync(db, adminRole, menu.Menu_Id, m.Auth);
            added++;
        }

        if (added > 0 || moved > 0)
            logger.LogInformation("设备通讯菜单：新增 {Added}，迁移归类 {Moved}", added, moved);
    }

    /// <summary>幂等：WMS / WCS / 平台 / SCADA 菜单目录（按 Features 过滤 TableName）</summary>
    static async Task SeedWmsWcsMenusAsync(SevenDbContext db, ILogger logger)
    {
        async Task<Sys_Menu> EnsureFolderAsync(
            string name, string tableName, string icon, int orderNo, Sys_Menu? parent = null)
        {
            var parentId = parent?.Menu_Id ?? 0;
            var folder = await db.Sys_Menus.FirstOrDefaultAsync(m =>
                m.TableName == tableName || (m.ParentId == parentId && m.MenuName == name));
            if (folder == null)
            {
                folder = new Sys_Menu
                {
                    ParentId = parentId,
                    MenuName = name,
                    Icon = icon,
                    OrderNo = orderNo,
                    Url = null,
                    TableName = tableName,
                    Auth = null,
                    Enable = 1,
                    CreateDate = DateTime.Now,
                };
                db.Sys_Menus.Add(folder);
                await db.SaveChangesAsync();
                logger.LogInformation("已创建菜单目录「{Name}」(ParentId={ParentId})", name, parentId);
            }
            else
            {
                folder.ParentId = parentId;
                folder.MenuName = name;
                folder.TableName = tableName;
                folder.Url = null;
                folder.Icon = icon;
                folder.OrderNo = orderNo;
                await db.SaveChangesAsync();
            }
            return folder;
        }

        var wms = await EnsureFolderAsync("仓储WMS", "WmsFolder", "Box", 6);
        var wcs = await EnsureFolderAsync("立库WCS", "WcsFolder", "Cpu", 7);
        var fw = await EnsureFolderAsync("四向车WCS", "FourWayFolder", "Van", 8);
        // OrderNo≥90：前端双轨菜单钉在轨底部
        var plat = await EnsureFolderAsync("执行运维", "WcsOpsFolder", "Tools", 99);

        var wmsMaster = await EnsureFolderAsync("主数据", "WmsMasterFolder", "OfficeBuilding", 1, wms);
        var wmsStockFolder = await EnsureFolderAsync("库存容器", "WmsStockFolder", "TakeawayBox", 2, wms);
        var wmsOrder = await EnsureFolderAsync("单据作业", "WmsOrderFolder", "Document", 3, wms);

        var wcsSim = await EnsureFolderAsync("仿真与监控", "WcsSimFolder", "Monitor", 1, wcs);
        var wcsPolicy = await EnsureFolderAsync("策略", "WcsPolicyFolder", "Setting", 2, wcs);
        var wcsMaster = await EnsureFolderAsync("主数据", "WcsMasterFolder", "OfficeBuilding", 3, wcs);
        var wcsTask = await EnsureFolderAsync("任务", "WcsTaskFolder", "Collection", 4, wcs);

        var fwSim = await EnsureFolderAsync("仿真", "FwSimFolder", "Operation", 1, fw);
        var fwPolicy = await EnsureFolderAsync("策略", "FwPolicyFolder", "Setting", 2, fw);
        var fwMaster = await EnsureFolderAsync("主数据", "FwMasterFolder", "OfficeBuilding", 3, fw);
        var fwTask = await EnsureFolderAsync("任务", "FwTaskFolder", "Collection", 4, fw);

        const string AuthCrud = "Search,Add,Update,Delete,Import,Export";
        const string AuthRu = "Search,Update,Export";
        const string AuthR = "Search,Export";
        const string AuthOrder = "Search,Add,Update,Import,Export";

        var items = new (Sys_Menu Parent, string MenuName, string Url, string TableName, string Auth, int OrderNo)[]
        {
            (wmsMaster, "仓库", "/Wms/WmsWarehouse", "WmsWarehouse", AuthCrud, 1),
            (wmsMaster, "库区", "/Wms/WmsZone", "WmsZone", AuthCrud, 2),
            (wmsMaster, "层", "/Wms/WmsLayer", "WmsLayer", AuthCrud, 3),
            (wmsMaster, "巷道", "/Wms/WmsAisle", "WmsAisle", AuthCrud, 4),
            (wmsMaster, "库位", "/Wms/Location", "WmsLocation", AuthCrud, 5),

            (wmsStockFolder, "容器类型", "/Wms/WmsContainerType", "WmsContainerType", AuthCrud, 1),
            (wmsStockFolder, "容器", "/Wms/WmsContainer", "WmsContainer", AuthCrud, 2),
            (wmsStockFolder, "交接位", "/Wms/WmsHandoverLink", "WmsHandoverLink", AuthCrud, 3),
            (wmsStockFolder, "库存", "/Wms/Stock", "WmsStock", AuthRu, 4),
            (wmsStockFolder, "库存流水", "/Wms/WmsStockLedger", "WmsStockLedger", AuthR, 5),

            (wmsOrder, "入库单", "/Wms/WmsInboundOrder", "WmsInboundOrder", AuthOrder, 1),
            (wmsOrder, "出库单", "/Wms/WmsOutboundOrder", "WmsOutboundOrder", AuthOrder, 2),
            (wmsOrder, "盘点单", "/Wms/CycleCount", "WmsCycleCount", AuthOrder, 3),
            (wmsOrder, "拣选任务", "/Wms/WmsPickingTask", "WmsPickingTask", AuthRu, 4),
            (wmsOrder, "入库快捷", "/Wms/InboundOrder", "WmsInboundOrderOps", AuthOrder, 11),
            (wmsOrder, "出库快捷", "/Wms/OutboundOrder", "WmsOutboundOrderOps", AuthOrder, 12),

            (wcsSim, "堆垛仿真触发", "/Wcs/Stacker/Trigger", "StackerTrigger", AuthRu, 1),
            (wcsSim, "运输单监控", "/Wcs/Bus/TransportOrder", "BusTransportOrder", AuthR, 2),
            (wcsPolicy, "巷道策略", "/Wcs/Stacker/StkAssignmentPolicy", "StkAssignmentPolicy", AuthCrud, 1),
            (wcsPolicy, "双深配置", "/Wcs/Stacker/StkLocationProfile", "StkLocationProfile", AuthCrud, 2),
            (wcsMaster, "申请点", "/Wcs/Stacker/StkRequestPoint", "StkRequestPoint", AuthCrud, 1),
            (wcsMaster, "路网", "/Wcs/Stacker/StkRoute", "StkRoute", AuthCrud, 2),
            (wcsMaster, "点码映射", "/Wcs/Stacker/StkDeviceCoder", "StkDeviceCoder", AuthCrud, 3),
            (wcsTask, "上架任务", "/Wcs/Stacker/StkPutAwayTask", "StkPutAwayTask", AuthRu, 1),
            (wcsTask, "取货任务", "/Wcs/Stacker/StkRetrievalTask", "StkRetrievalTask", AuthRu, 2),
            (wcsTask, "设备段任务", "/Wcs/Stacker/StkDeviceTask", "StkDeviceTask", AuthRu, 3),

            (fwSim, "四向仿真触发", "/Wcs/FourWay/Trigger", "FourWayTrigger", AuthRu, 1),
            (fwPolicy, "层策略", "/Wcs/FourWay/FwLayerPolicy", "FwLayerPolicy", AuthCrud, 1),
            (fwPolicy, "巷策略", "/Wcs/FourWay/FwAislePolicy", "FwAislePolicy", AuthCrud, 2),
            (fwMaster, "申请点", "/Wcs/FourWay/FwRequestPoint", "FwRequestPoint", AuthCrud, 1),
            (fwMaster, "地图版本", "/Wcs/FourWay/FwMapVersion", "FwMapVersion", AuthCrud, 2),
            (fwMaster, "节点", "/Wcs/FourWay/FwNode", "FwNode", AuthCrud, 3),
            (fwMaster, "路网边", "/Wcs/FourWay/FwRoute", "FwRoute", AuthCrud, 4),
            (fwMaster, "停车账本", "/Wcs/FourWay/FwParkingLedger", "FwParkingLedger", AuthCrud, 5),
            (fwMaster, "提升机", "/Wcs/FourWay/FwHoistDevice", "FwHoistDevice", AuthCrud, 6),
            (fwMaster, "提升机层口", "/Wcs/FourWay/FwHoistLayerPoint", "FwHoistLayerPoint", AuthCrud, 7),
            (fwTask, "上架任务", "/Wcs/FourWay/FwPutAwayTask", "FwPutAwayTask", AuthRu, 1),
            (fwTask, "取货任务", "/Wcs/FourWay/FwRetrievalTask", "FwRetrievalTask", AuthRu, 2),
            (fwTask, "穿梭任务", "/Wcs/FourWay/FwShuttleTask", "FwShuttleTask", AuthRu, 3),
            (fwTask, "提升任务", "/Wcs/FourWay/FwHoistTask", "FwHoistTask", AuthRu, 4),
            (fwTask, "提升执行段", "/Wcs/FourWay/FwHoistExecTask", "FwHoistExecTask", AuthRu, 5),

            (plat, "运行模式/联锁", "/Platform/ControlMode", "CtlMode", AuthRu, 1),
            (plat, "接口日志", "/Platform/InterfaceLog", "IfcApiLog", AuthR, 2),
            (plat, "2D看板", "/Scada/Floor2d", "ScadaFolder", AuthR, 3),
        };

        var adminRole = await db.Sys_Roles.OrderBy(r => r.Role_Id).FirstOrDefaultAsync();
        var added = 0;
        var authFixed = 0;
        foreach (var m in items)
        {
            // Prefer TableName match so URL renames (e.g. InboundOrder → WmsInboundOrder) migrate in place.
            var existing = await db.Sys_Menus.FirstOrDefaultAsync(x => x.TableName == m.TableName)
                ?? await db.Sys_Menus.FirstOrDefaultAsync(x => x.Url == m.Url);
            if (existing != null)
            {
                var dirty = false;
                if (existing.ParentId != m.Parent.Menu_Id)
                {
                    existing.ParentId = m.Parent.Menu_Id;
                    dirty = true;
                }
                if (existing.MenuName != m.MenuName)
                {
                    existing.MenuName = m.MenuName;
                    dirty = true;
                }
                if (existing.Url != m.Url)
                {
                    existing.Url = m.Url;
                    dirty = true;
                }
                if (existing.TableName != m.TableName)
                {
                    existing.TableName = m.TableName;
                    dirty = true;
                }
                if (existing.Auth != m.Auth)
                {
                    existing.Auth = m.Auth;
                    dirty = true;
                }
                if (existing.OrderNo != m.OrderNo)
                {
                    existing.OrderNo = m.OrderNo;
                    dirty = true;
                }
                var leafIcon = ResolveWmsLeafIcon(m.MenuName);
                if (!string.IsNullOrEmpty(leafIcon) && !string.Equals(existing.Icon, leafIcon, StringComparison.Ordinal))
                {
                    existing.Icon = leafIcon;
                    dirty = true;
                }
                if (dirty)
                    await db.SaveChangesAsync();

                var before = await db.Sys_RoleAuths.CountAsync(a =>
                    adminRole != null && a.Role_Id == adminRole.Role_Id && a.Menu_Id == existing.Menu_Id);
                await EnsureRoleAuthAsync(db, adminRole, existing.Menu_Id, m.Auth);
                if (before == 0 && adminRole != null) authFixed++;
                continue;
            }

            var menu = new Sys_Menu
            {
                ParentId = m.Parent.Menu_Id,
                MenuName = m.MenuName,
                Url = m.Url,
                TableName = m.TableName,
                Auth = m.Auth,
                Icon = ResolveWmsLeafIcon(m.MenuName),
                OrderNo = m.OrderNo,
                Enable = 1,
                CreateDate = DateTime.Now,
            };
            db.Sys_Menus.Add(menu);
            await db.SaveChangesAsync();
            await EnsureRoleAuthAsync(db, adminRole, menu.Menu_Id, m.Auth);
            added++;
        }

        if (added > 0 || authFixed > 0)
            logger.LogInformation("WMS/WCS 菜单：新增 {Added}，补授权 {AuthFixed}", added, authFixed);

        static string? ResolveWmsLeafIcon(string menuName) => menuName switch
        {
            "仓库" or "部门管理" => "OfficeBuilding",
            "库区" or "菜单管理" or "双深配置" => "Grid",
            "层" or "角色管理" or "层策略" => "List",
            "巷道" or "巷道策略" or "巷策略" or "代码生成" => "Operation",
            "库位" or "申请点" or "节点" or "提升机层口" => "Location",
            "容器类型" or "库存" or "仓储WMS" => "Box",
            "容器" or "上架任务" => "TakeawayBox",
            "交接位" or "路网" or "路网边" or "通讯连接" => "Connection",
            "库存流水" or "运输单监控" or "接口日志" => "DataLine",
            "入库单" or "入库快捷" => "ShoppingCart",
            "出库单" or "出库快捷" or "取货任务" or "四向车WCS" => "Van",
            "盘点单" or "停车账本" => "Notebook",
            "拣选任务" => "Collection",
            "堆垛仿真触发" or "四向仿真触发" or "提升任务" => "Operation",
            "设备段任务" or "提升机" or "立库WCS" => "Cpu",
            "穿梭任务" => "Van",
            "提升执行段" => "Files",
            "运行模式/联锁" => "Setting",
            "2D看板" => "Monitor",
            "点码映射" => "List",
            "地图版本" => "Document",
            _ => null
        };
    }

    /// <summary>业务扩展菜单（doc/23 样板：仓内调拨）。独立轨，不塞进标准仓储WMS。</summary>
    /// <summary>
    /// 业务扩展菜单样板（doc/23）。默认独立轨 BizFolder；
    /// 项目可改 leaf 的 Parent 挂到仓储WMS/立库/四向/运维等中层（菜单自选）。
    /// </summary>
    static async Task SeedBizMenusAsync(SevenDbContext db, ILogger logger)
    {
        async Task<Sys_Menu> EnsureFolderAsync(string name, string tableName, string icon, int orderNo)
        {
            var folder = await db.Sys_Menus.FirstOrDefaultAsync(m =>
                m.TableName == tableName || (m.ParentId == 0 && m.MenuName == name));
            if (folder == null)
            {
                folder = new Sys_Menu
                {
                    ParentId = 0,
                    MenuName = name,
                    Icon = icon,
                    OrderNo = orderNo,
                    Url = null,
                    TableName = tableName,
                    Auth = null,
                    Enable = 1,
                    CreateDate = DateTime.Now,
                };
                db.Sys_Menus.Add(folder);
                await db.SaveChangesAsync();
                logger.LogInformation("已创建菜单目录「{Name}」", name);
            }
            else
            {
                folder.ParentId = 0;
                folder.MenuName = name;
                folder.TableName = tableName;
                folder.Url = null;
                folder.Icon = icon;
                folder.OrderNo = orderNo;
                await db.SaveChangesAsync();
            }
            return folder;
        }

        // OrderNo=9：排在四向车(8)之后、执行运维(99)之前
        var biz = await EnsureFolderAsync("业务扩展", "BizFolder", "Files", 9);
        const string AuthOrder = "Search,Add,Update,Delete,Import,Export";

        var leaf = await db.Sys_Menus.FirstOrDefaultAsync(x => x.TableName == "TransferOrder")
            ?? await db.Sys_Menus.FirstOrDefaultAsync(x => x.Url == "/Business/TransferOrder");

        if (leaf == null)
        {
            leaf = new Sys_Menu
            {
                ParentId = biz.Menu_Id,
                MenuName = "仓内调拨",
                Icon = "Sort",
                OrderNo = 1,
                Url = "/Business/TransferOrder",
                TableName = "TransferOrder",
                Auth = AuthOrder,
                Enable = 1,
                CreateDate = DateTime.Now,
            };
            db.Sys_Menus.Add(leaf);
            await db.SaveChangesAsync();
            logger.LogInformation("已创建业务扩展菜单「仓内调拨」");
        }
        else
        {
            leaf.ParentId = biz.Menu_Id;
            leaf.MenuName = "仓内调拨";
            leaf.Url = "/Business/TransferOrder";
            leaf.TableName = "TransferOrder";
            leaf.Auth = AuthOrder;
            leaf.Icon = "Sort";
            leaf.OrderNo = 1;
            leaf.Enable = 1;
            await db.SaveChangesAsync();
        }

        var adminRole = await db.Sys_Roles.OrderBy(r => r.Role_Id).FirstOrDefaultAsync();
        if (adminRole != null)
            await EnsureRoleAuthAsync(db, adminRole, leaf.Menu_Id, AuthOrder);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Domain.Entities.Alarm;
using Seven.Domain.Entities.Business;
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
        await SeedDeviceDetailDemoAsync(db, logger);
        await SeedDefaultTenantAsync(db, logger);

        // 先确保有管理员角色，再种子业务菜单并补 RoleAuth。
        // 旧顺序会在「尚无角色」时创建 WMS/WCS 菜单，导致 getMenu 永远看不到这些项。
        if (!await db.Sys_Users.AnyAsync())
        {
            await SeedAdminUserAndSystemMenusAsync(db, hasher, logger);
        }

        await SeedExtraMenusAndJobsAsync(db, logger);
        await SeedDeviceCommMenusAsync(db, logger);
        await SeedWmsWcsMenusAsync(db, logger);
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
            new { MenuName = "用户管理", OrderNo = 1, Url = "/Sys_User", TableName = "Sys_User", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "角色管理", OrderNo = 2, Url = "/Sys_Role", TableName = "Sys_Role", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "菜单管理", OrderNo = 3, Url = "/Sys_Menu", TableName = "Sys_Menu", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "部门管理", OrderNo = 4, Url = "/Sys_Department", TableName = "Sys_Department", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "字典管理", OrderNo = 5, Url = "/Sys_Dictionary", TableName = "Sys_Dictionary", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "日志管理", OrderNo = 6, Url = "/Sys_Log", TableName = "Sys_Log", Auth = "Search" },
            new { MenuName = "告警管理", OrderNo = 7, Url = "/Sys_Alarm", TableName = "Sys_Alarm", Auth = "Search,Acknowledge,Clear,Raise" },
            new { MenuName = "代码生成", OrderNo = 8, Url = "/coder", TableName = "Sys_TableInfo", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "设备管理", OrderNo = 9, Url = "/Device", TableName = "Device", Auth = "Search,Add,Update,Delete,Import,Export,BatchCustom,RowCustom" },
            new { MenuName = "子设备", OrderNo = 10, Url = "/SubDevice", TableName = "SubDevice", Auth = "Search,Add,Update,Delete,Import,Export" },
            new { MenuName = "工作流定义", OrderNo = 11, Url = "/Sys_WorkFlow", TableName = "Sys_WorkFlow", Auth = "Search,Add,Update,Delete" },
            new { MenuName = "我的审批", OrderNo = 12, Url = "/Sys_WorkFlowTable", TableName = "Sys_WorkFlowTable", Auth = "Search,Audit" },
            new { MenuName = "定时任务", OrderNo = 13, Url = "/Sys_QuartzOptions", TableName = "Sys_QuartzOptions", Auth = "Search,Add,Update,Delete" },
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

    /// <summary>
    /// Device ↔ SubDevice 主子表 Demo（幂等）：关系配置、菜单、示例数据
    /// </summary>
    static async Task SeedDeviceDetailDemoAsync(SevenDbContext db, ILogger logger)
    {
        // 1) Sys_TableDetail：主表 Device → 子表 SubDevice（Below）
        var detailExists = await db.Sys_TableDetails.AnyAsync(d =>
            d.ParentTable.ToLower() == "device"
            && d.ChildTable.ToLower() == "subdevice"
            && d.ForeignKey.ToLower() == "deviceid");
        if (!detailExists)
        {
            db.Sys_TableDetails.Add(new Sys_TableDetail
            {
                ParentTable = "Device",
                ChildTable = "SubDevice",
                ForeignKey = "DeviceId",
                MasterKey = "DeviceId",
                Enable = true,
                DisplayMode = "Below",
                OrderNo = 10,
                CnName = "子设备",
                CreateDate = DateTime.Now,
                Creator = "system",
            });
            await db.SaveChangesAsync();
            logger.LogInformation("已写入 Device→SubDevice 主子表配置");
        }

        // 2) 子设备菜单（已有库也能补）
        var subMenu = await db.Sys_Menus.FirstOrDefaultAsync(m => m.TableName == "SubDevice" || m.Url == "/SubDevice");
        if (subMenu == null)
        {
            var parent = await db.Sys_Menus.FirstOrDefaultAsync(m => m.ParentId == 0 && m.MenuName == "系统管理")
                ?? await db.Sys_Menus.FirstOrDefaultAsync(m => m.ParentId == 0);
            if (parent != null)
            {
                subMenu = new Sys_Menu
                {
                    ParentId = parent.Menu_Id,
                    MenuName = "子设备",
                    Url = "/SubDevice",
                    TableName = "SubDevice",
                    Auth = "Search,Add,Update,Delete,Import,Export",
                    OrderNo = 10,
                    Enable = 1,
                    CreateDate = DateTime.Now,
                };
                db.Sys_Menus.Add(subMenu);
                await db.SaveChangesAsync();

                var adminRole = await db.Sys_Roles.OrderBy(r => r.Role_Id).FirstOrDefaultAsync();
                if (adminRole != null)
                {
                    db.Sys_RoleAuths.Add(new Sys_RoleAuth
                    {
                        Role_Id = adminRole.Role_Id,
                        Menu_Id = subMenu.Menu_Id,
                        AuthValue = subMenu.Auth,
                    });
                    await db.SaveChangesAsync();
                }
                logger.LogInformation("已补充子设备菜单 /SubDevice");
            }
        }

        // 3) 示例子设备（若已有主设备且尚无子设备）
        if (await db.SubDevices.AnyAsync(x => !x.IsDeleted))
            return;

        var devices = await db.Devices.AsNoTracking().Where(d => !d.IsDeleted).Take(3).ToListAsync();
        if (devices.Count == 0)
            return;

        foreach (var d in devices)
        {
            db.SubDevices.Add(new SubDevice
            {
                DeviceId = d.DeviceId,
                SubDeviceName = $"{d.DeviceName}-单元A",
                SubDeviceCode = $"SUB-{d.DeviceId}-A",
                Status = d.Status,
                Remark = "主子表 Demo 示例",
                CreateDate = DateTime.Now,
                Creator = "system",
            });
            db.SubDevices.Add(new SubDevice
            {
                DeviceId = d.DeviceId,
                SubDeviceName = $"{d.DeviceName}-单元B",
                SubDeviceCode = $"SUB-{d.DeviceId}-B",
                Status = 0,
                Remark = "主子表 Demo 示例",
                CreateDate = DateTime.Now,
                Creator = "system",
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("已写入 SubDevice 示例数据 {Count} 条", devices.Count * 2);
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
        async Task<Sys_Menu> EnsureFolderAsync(string name, string tableName, string icon, int orderNo)
        {
            var folder = await db.Sys_Menus.FirstOrDefaultAsync(m =>
                m.ParentId == 0 && (m.MenuName == name || m.TableName == tableName));
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
                logger.LogInformation("已创建顶级菜单目录「{Name}」", name);
            }
            else
            {
                folder.ParentId = 0;
                folder.MenuName = name;
                folder.TableName = tableName;
                folder.Icon ??= icon;
                if (folder.OrderNo == 0) folder.OrderNo = orderNo;
                await db.SaveChangesAsync();
            }
            return folder;
        }

        var wms = await EnsureFolderAsync("仓储WMS", "WmsFolder", "Box", 6);
        var wcs = await EnsureFolderAsync("立库WCS", "WcsFolder", "Cpu", 7);
        var fw = await EnsureFolderAsync("四向车WCS", "FourWayFolder", "Van", 8);
        var plat = await EnsureFolderAsync("执行运维", "WcsOpsFolder", "Tools", 9);

        const string AuthCrud = "Search,Add,Update,Delete";
        const string AuthRu = "Search,Update";
        const string AuthR = "Search";

        var items = new (Sys_Menu Parent, string MenuName, string Url, string TableName, string Auth, int OrderNo)[]
        {
            (wms, "仓库", "/Wms/WmsWarehouse", "WmsWarehouse", AuthCrud, 1),
            (wms, "库区", "/Wms/WmsZone", "WmsZone", AuthCrud, 2),
            (wms, "层", "/Wms/WmsLayer", "WmsLayer", AuthCrud, 3),
            (wms, "巷道", "/Wms/WmsAisle", "WmsAisle", AuthCrud, 4),
            (wms, "库位", "/Wms/Location", "WmsLocation", "Search,Add,Update,Delete", 5),
            (wms, "容器类型", "/Wms/WmsContainerType", "WmsContainerType", AuthCrud, 6),
            (wms, "容器", "/Wms/WmsContainer", "WmsContainer", AuthCrud, 7),
            (wms, "交接位", "/Wms/WmsHandoverLink", "WmsHandoverLink", AuthCrud, 8),
            (wms, "库存", "/Wms/Stock", "WmsStock", "Search,Update", 9),
            (wms, "库存流水", "/Wms/WmsStockLedger", "WmsStockLedger", AuthR, 10),
            (wms, "入库单", "/Wms/InboundOrder", "WmsInboundOrder", "Search,Add,Update", 11),
            (wms, "出库单", "/Wms/OutboundOrder", "WmsOutboundOrder", "Search,Add,Update", 12),
            (wms, "盘点单", "/Wms/CycleCount", "WmsCycleCount", "Search,Add,Update", 13),

            (wcs, "堆垛仿真触发", "/Wcs/Stacker/Trigger", "StackerTrigger", AuthRu, 1),
            (wcs, "运输单监控", "/Wcs/Bus/TransportOrder", "BusTransportOrder", AuthR, 2),
            (wcs, "申请点", "/Wcs/Stacker/StkRequestPoint", "StkRequestPoint", AuthCrud, 3),
            (wcs, "巷道策略", "/Wcs/Stacker/StkAssignmentPolicy", "StkAssignmentPolicy", AuthCrud, 4),
            (wcs, "双深配置", "/Wcs/Stacker/StkLocationProfile", "StkLocationProfile", AuthCrud, 5),
            (wcs, "路网", "/Wcs/Stacker/StkRoute", "StkRoute", AuthCrud, 6),
            (wcs, "点码映射", "/Wcs/Stacker/StkDeviceCoder", "StkDeviceCoder", AuthCrud, 7),
            (wcs, "上架任务", "/Wcs/Stacker/StkPutAwayTask", "StkPutAwayTask", AuthRu, 8),
            (wcs, "取货任务", "/Wcs/Stacker/StkRetrievalTask", "StkRetrievalTask", AuthRu, 9),
            (wcs, "设备段任务", "/Wcs/Stacker/StkDeviceTask", "StkDeviceTask", AuthRu, 10),

            (fw, "四向仿真触发", "/Wcs/FourWay/Trigger", "FourWayTrigger", AuthRu, 1),
            (fw, "层策略", "/Wcs/FourWay/FwLayerPolicy", "FwLayerPolicy", AuthCrud, 2),
            (fw, "巷策略", "/Wcs/FourWay/FwAislePolicy", "FwAislePolicy", AuthCrud, 3),
            (fw, "申请点", "/Wcs/FourWay/FwRequestPoint", "FwRequestPoint", AuthCrud, 4),
            (fw, "地图版本", "/Wcs/FourWay/FwMapVersion", "FwMapVersion", AuthCrud, 5),
            (fw, "节点", "/Wcs/FourWay/FwNode", "FwNode", AuthCrud, 6),
            (fw, "路网边", "/Wcs/FourWay/FwRoute", "FwRoute", AuthCrud, 7),
            (fw, "停车账本", "/Wcs/FourWay/FwParkingLedger", "FwParkingLedger", AuthCrud, 8),
            (fw, "提升机", "/Wcs/FourWay/FwHoistDevice", "FwHoistDevice", AuthCrud, 9),
            (fw, "提升机层口", "/Wcs/FourWay/FwHoistLayerPoint", "FwHoistLayerPoint", AuthCrud, 10),
            (fw, "上架任务", "/Wcs/FourWay/FwPutAwayTask", "FwPutAwayTask", AuthRu, 11),
            (fw, "取货任务", "/Wcs/FourWay/FwRetrievalTask", "FwRetrievalTask", AuthRu, 12),
            (fw, "穿梭任务", "/Wcs/FourWay/FwShuttleTask", "FwShuttleTask", AuthRu, 13),
            (fw, "提升任务", "/Wcs/FourWay/FwHoistTask", "FwHoistTask", AuthRu, 14),
            (fw, "提升执行段", "/Wcs/FourWay/FwHoistExecTask", "FwHoistExecTask", AuthRu, 15),

            (plat, "运行模式/联锁", "/Platform/ControlMode", "CtlMode", AuthRu, 1),
            (plat, "接口日志", "/Platform/InterfaceLog", "IfcApiLog", AuthR, 2),
            (plat, "2D看板", "/Scada/Floor2d", "ScadaFolder", AuthR, 3),
        };

        var adminRole = await db.Sys_Roles.OrderBy(r => r.Role_Id).FirstOrDefaultAsync();
        var added = 0;
        var authFixed = 0;
        foreach (var m in items)
        {
            var existing = await db.Sys_Menus.FirstOrDefaultAsync(x =>
                x.Url == m.Url || x.TableName == m.TableName);
            if (existing != null)
            {
                if (existing.ParentId != m.Parent.Menu_Id)
                {
                    existing.ParentId = m.Parent.Menu_Id;
                    existing.OrderNo = m.OrderNo;
                    await db.SaveChangesAsync();
                }

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
    }
}

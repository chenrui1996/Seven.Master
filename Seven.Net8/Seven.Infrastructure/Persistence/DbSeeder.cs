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

        if (await db.Sys_Users.AnyAsync())
            return;

        logger.LogInformation("正在初始化 Seven 种子数据...");

        var adminRole = new Sys_Role
        {
            RoleName = "超级管理员",
            ParentId = 0,
            Enable = 1,
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
                CreateDate = DateTime.Now,
            })
            .ToList();
        db.Sys_Menus.AddRange(menus);
        await db.SaveChangesAsync();

        foreach (var menu in menus.Where(m => !string.IsNullOrEmpty(m.TableName)))
        {
            db.Sys_RoleAuths.Add(
                new Sys_RoleAuth
                {
                    Role_Id = adminRole.Role_Id,
                    Menu_Id = menu.Menu_Id,
                    AuthValue = menu.Auth,
                }
            );
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seven 种子数据初始化完成。默认账号 admin / 123456");
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
}

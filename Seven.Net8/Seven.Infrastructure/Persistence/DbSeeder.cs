using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Seven.Application.Interfaces;
using Seven.Domain.Entities.System;
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

        await db.Database.EnsureCreatedAsync();

        if (await db.Sys_Users.AnyAsync()) return;

        logger.LogInformation("正在初始化 Seven 种子数据...");

        var adminRole = new Sys_Role
        {
            RoleName = "超级管理员",
            ParentId = 0,
            Enable = 1,
            CreateDate = DateTime.Now
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
            CreateDate = DateTime.Now
        };
        db.Sys_Users.Add(adminUser);

        var menuDefs = new[]
        {
            new { ParentId = 0, MenuName = "系统管理", Icon = "Setting", OrderNo = 1, Url = "/system", TableName = (string?)null, Auth = (string?)null },
            new { ParentId = 0, MenuName = "用户管理", Icon = (string?)null, OrderNo = 1, Url = "/Sys_User", TableName = "Sys_User", Auth = "Search,Add,Update,Delete" },
            new { ParentId = 0, MenuName = "角色管理", Icon = (string?)null, OrderNo = 2, Url = "/Sys_Role", TableName = "Sys_Role", Auth = "Search,Add,Update,Delete" },
            new { ParentId = 0, MenuName = "菜单管理", Icon = (string?)null, OrderNo = 3, Url = "/Sys_Menu", TableName = "Sys_Menu", Auth = "Search,Add,Update,Delete" },
            new { ParentId = 0, MenuName = "部门管理", Icon = (string?)null, OrderNo = 4, Url = "/Sys_Department", TableName = "Sys_Department", Auth = "Search,Add,Update,Delete" },
            new { ParentId = 0, MenuName = "字典管理", Icon = (string?)null, OrderNo = 5, Url = "/Sys_Dictionary", TableName = "Sys_Dictionary", Auth = "Search,Add,Update,Delete" },
            new { ParentId = 0, MenuName = "日志管理", Icon = (string?)null, OrderNo = 6, Url = "/Sys_Log", TableName = "Sys_Log", Auth = "Search" },
            new { ParentId = 0, MenuName = "代码生成", Icon = (string?)null, OrderNo = 7, Url = "/coder", TableName = "Sys_TableInfo", Auth = "Search,Add" },
            new { ParentId = 0, MenuName = "设备管理", Icon = (string?)null, OrderNo = 8, Url = "/Device", TableName = "Device", Auth = "Search,Add,Update,Delete" }
        };

        var menus = menuDefs.Select(m => new Sys_Menu
        {
            ParentId = m.ParentId,
            MenuName = m.MenuName,
            Icon = m.Icon,
            Url = m.Url,
            TableName = m.TableName,
            Auth = m.Auth,
            OrderNo = m.OrderNo,
            Enable = 1,
            CreateDate = DateTime.Now
        }).ToList();
        db.Sys_Menus.AddRange(menus);
        await db.SaveChangesAsync();

        foreach (var menu in menus.Where(m => !string.IsNullOrEmpty(m.TableName)))
        {
            db.Sys_RoleAuths.Add(new Sys_RoleAuth
            {
                Role_Id = adminRole.Role_Id,
                Menu_Id = menu.Menu_Id,
                AuthValue = menu.Auth
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seven 种子数据初始化完成。默认账号 admin / 123456");
    }
}

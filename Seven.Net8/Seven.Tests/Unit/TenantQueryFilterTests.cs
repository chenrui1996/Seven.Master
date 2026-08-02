using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Domain.Entities.System;
using Seven.Infrastructure.Persistence;

namespace Seven.Tests.Unit;

/// <summary>
/// 租户全局过滤器行为（默认 Testing/种子场景 TenantFilterEnabled=false）
/// </summary>
public class TenantQueryFilterTests
{
    static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"TenantFilter_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public void WhenFilterDisabled_ShouldReturnAllTenants()
    {
        using var db = CreateDb();
        db.TenantFilterEnabled = false;
        db.Sys_Users.AddRange(
            new Sys_User { UserName = "a", UserTrueName = "A", PasswordHash = "x", Role_Id = 1, Enable = 1, TenantId = 1 },
            new Sys_User { UserName = "b", UserTrueName = "B", PasswordHash = "x", Role_Id = 1, Enable = 1, TenantId = 2 });
        db.SaveChanges();

        db.Sys_Users.Count().Should().Be(2);
    }

    [Fact]
    public void WhenFilterEnabled_ShouldIsolateCurrentTenantAndSharedZero()
    {
        using var db = CreateDb();
        db.TenantFilterEnabled = true;
        db.CurrentTenantId = 1;
        db.Sys_Users.AddRange(
            new Sys_User { UserName = "t1", UserTrueName = "T1", PasswordHash = "x", Role_Id = 1, Enable = 1, TenantId = 1 },
            new Sys_User { UserName = "t2", UserTrueName = "T2", PasswordHash = "x", Role_Id = 1, Enable = 1, TenantId = 2 },
            new Sys_User { UserName = "shared", UserTrueName = "S", PasswordHash = "x", Role_Id = 1, Enable = 1, TenantId = 0 });
        db.SaveChanges();

        var names = db.Sys_Users.Select(u => u.UserName).OrderBy(x => x).ToList();
        names.Should().Equal("shared", "t1");
    }

    [Fact]
    public void DefaultContext_ShouldNotFilterLikeProductionTenant()
    {
        using var db = CreateDb();
        db.Sys_Users.Add(new Sys_User { UserName = "only", UserTrueName = "O", PasswordHash = "x", Role_Id = 1, Enable = 1, TenantId = 99 });
        db.SaveChanges();

        db.TenantFilterEnabled.Should().BeFalse();
        db.Sys_Users.Count().Should().Be(1);
    }
}

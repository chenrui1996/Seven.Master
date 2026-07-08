using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Domain.Entities.System;
using Seven.Infrastructure.Caching;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Security;
using Seven.Infrastructure.Services;

namespace Seven.Tests.Unit;

/// <summary>
/// 认证服务单元测试（InMemory 数据库）
/// </summary>
public class AuthServiceTests
{
    private static (SevenDbContext Db, AuthService Service) CreateService()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"AuthTest_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();

        var hasher = new BcryptPasswordHasher();
        var cacheInner = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        var cache = new DelayedDoubleDeleteCacheService(cacheInner, Options.Create(new CacheOptions { DelayedDeleteMs = 10 }));
        var tokenService = new TokenService(Options.Create(new JwtOptions
        {
            Secret = "SevenMasterJwtSecretKey2026MustBe32Chars!",
            Issuer = "Seven",
            Audience = "Seven"
        }), cache);

        var role = new Sys_Role { RoleName = "测试角色", ParentId = 0, Enable = 1 };
        db.Sys_Roles.Add(role);
        db.SaveChanges();

        db.Sys_Users.Add(new Sys_User
        {
            UserName = "testuser",
            UserTrueName = "测试用户",
            PasswordHash = hasher.HashPassword("pass123"),
            Role_Id = role.Role_Id,
            Enable = 1
        });
        db.SaveChanges();

        return (db, new AuthService(db, hasher, tokenService));
    }

    [Fact]
    public async Task LoginAsync_WithValidUser_ShouldSucceed()
    {
        var (_, service) = CreateService();
        var result = await service.LoginAsync("testuser", "pass123", null, null);

        result.Status.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldFail()
    {
        var (_, service) = CreateService();
        var result = await service.LoginAsync("testuser", "badpass", null, null);

        result.Status.Should().BeFalse();
        result.Message.Should().Contain("密码");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldUpdateHash()
    {
        var (db, service) = CreateService();
        var user = await db.Sys_Users.FirstAsync();
        var hasher = new BcryptPasswordHasher();

        var result = await service.ChangePasswordAsync(user.User_Id, "pass123", "newpass456");
        result.Status.Should().BeTrue();

        await db.Entry(user).ReloadAsync();
        hasher.VerifyPassword("newpass456", user.PasswordHash).Should().BeTrue();
    }
}

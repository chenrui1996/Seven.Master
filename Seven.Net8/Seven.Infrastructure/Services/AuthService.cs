using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.System;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Services;

/// <summary>
/// 认证服务：登录、Token 刷新、登出、改密
/// </summary>
public class AuthService : IAuthService
{
    private readonly SevenDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;

    /// <summary>构造函数</summary>
    public AuthService(SevenDbContext db, IPasswordHasher hasher, ITokenService tokenService)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> LoginAsync(string userName, string password, string? captchaCode, string? captchaKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return WebResponseContent.Error("用户名或密码不能为空");

        var user = await _db.Sys_Users.FirstOrDefaultAsync(u => u.UserName == userName && u.Enable == 1, cancellationToken);
        if (user == null) return WebResponseContent.Error("用户名或密码错误");
        if (user.MustResetPassword) return WebResponseContent.Error("请重置密码后再登录");
        if (!_hasher.VerifyPassword(password, user.PasswordHash))
            return WebResponseContent.Error("用户名或密码错误");

        user.LastLoginDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);

        var (accessToken, refreshToken) = _tokenService.GenerateTokens(user.User_Id, user.UserName, user.Role_Id);
        var permissions = await GetPermissionsAsync(user.Role_Id, cancellationToken);

        return WebResponseContent.Ok("登录成功", new
        {
            token = accessToken,
            refreshToken,
            userId = user.User_Id,
            userName = user.UserName,
            userTrueName = user.UserTrueName,
            roleId = user.Role_Id,
            permissions,
            img = user.HeadImageUrl
        });
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var result = await _tokenService.RefreshTokenAsync(refreshToken, cancellationToken);
        if (result == null) return WebResponseContent.Error("Refresh Token 无效或已过期");
        return WebResponseContent.Ok("刷新成功", new { token = result.Value.AccessToken, refreshToken = result.Value.RefreshToken });
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        await _tokenService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
        return WebResponseContent.Ok("退出成功");
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _db.Sys_Users.FindAsync([userId], cancellationToken);
        if (user == null) return WebResponseContent.Error("用户不存在");
        if (!_hasher.VerifyPassword(oldPassword, user.PasswordHash)) return WebResponseContent.Error("原密码错误");

        user.PasswordHash = _hasher.HashPassword(newPassword);
        user.MustResetPassword = false;
        user.LastModifyPwdDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("密码修改成功");
    }

    private async Task<List<string>> GetPermissionsAsync(int roleId, CancellationToken cancellationToken)
    {
        var auths = await _db.Sys_RoleAuths.Where(a => a.Role_Id == roleId).ToListAsync(cancellationToken);
        var menuIds = auths.Select(a => a.Menu_Id).ToList();
        var menus = await _db.Sys_Menus.Where(m => menuIds.Contains(m.Menu_Id)).ToListAsync(cancellationToken);
        var permissions = new List<string>();
        foreach (var auth in auths)
        {
            var menu = menus.FirstOrDefault(m => m.Menu_Id == auth.Menu_Id);
            if (menu?.TableName == null) continue;
            foreach (var action in (auth.AuthValue ?? menu.Auth ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                permissions.Add($"{menu.TableName}.{action.Trim()}");
        }
        return permissions;
    }
}

/// <summary>分页辅助</summary>
public static class CrudHelper
{
    /// <summary>分页查询</summary>
    public static async Task<PageGridData<T>> PaginateAsync<T>(IQueryable<T> query, PageDataOptions options, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((options.Page - 1) * options.Rows).Take(options.Rows).ToListAsync(ct);
        return new PageGridData<T> { Total = total, Rows = rows };
    }
}

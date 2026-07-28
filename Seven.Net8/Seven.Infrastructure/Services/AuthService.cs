using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
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
    private readonly ICurrentUserService _currentUser;

    /// <summary>构造函数</summary>
    public AuthService(
        SevenDbContext db,
        IPasswordHasher hasher,
        ITokenService tokenService,
        ICurrentUserService currentUser)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
        _currentUser = currentUser;
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

    /// <inheritdoc />
    public async Task<WebResponseContent> GetMyPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var roleId = _currentUser.RoleId ?? 0;
        if (roleId <= 0) return WebResponseContent.Error("未登录");
        var permissions = await GetPermissionsAsync(roleId, cancellationToken);
        return WebResponseContent.Ok(data: permissions);
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
            if (string.IsNullOrWhiteSpace(menu?.TableName)) continue;
            // 只认角色已授权的 AuthValue，禁止回退到菜单全部 Auth（否则取消勾选仍有全量按钮权限）
            foreach (var action in (auth.AuthValue ?? "")
                         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.IsNullOrWhiteSpace(action)) continue;
                permissions.Add($"{menu!.TableName}.{action}");
            }
        }
        return permissions;
    }
}

/// <summary>分页辅助</summary>
public static class CrudHelper
{
    /// <summary>分页查询（支持 Wheres JSON 过滤）</summary>
    public static async Task<PageGridData<T>> PaginateAsync<T>(IQueryable<T> query, PageDataOptions options, CancellationToken ct)
    {
        query = ApplyWheres(query, options.Wheres);

        if (!string.IsNullOrWhiteSpace(options.Sort))
            query = ApplySort(query, options.Sort, options.Order);

        var total = await query.CountAsync(ct);
        var rows = await query.Skip((options.Page - 1) * options.Rows).Take(options.Rows).ToListAsync(ct);
        return new PageGridData<T> { Total = total, Rows = rows };
    }

    /// <summary>
    /// Wheres 格式：[{ "name":"prop","value":"...","displayType":"equal|like" }]
    /// name 与实体属性名匹配（忽略大小写）。
    /// </summary>
    public static IQueryable<T> ApplyWheres<T>(IQueryable<T> query, string? wheresJson)
    {
        if (string.IsNullOrWhiteSpace(wheresJson)) return query;
        try
        {
            using var doc = JsonDocument.Parse(wheresJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return query;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var nameEl)) continue;
                var name = nameEl.GetString();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var prop = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (prop == null) continue;

                if (!item.TryGetProperty("value", out var valueEl)) continue;
                var valueStr = valueEl.ValueKind switch
                {
                    JsonValueKind.String => valueEl.GetString(),
                    JsonValueKind.Number => valueEl.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => valueEl.ToString(),
                };
                if (string.IsNullOrWhiteSpace(valueStr)) continue;

                var displayType = item.TryGetProperty("displayType", out var dtEl)
                    ? dtEl.GetString()
                    : "equal";
                query = ApplyFilter(query, prop, valueStr!, displayType);
            }
        }
        catch
        {
            /* ignore invalid filter json */
        }

        return query;
    }

    static IQueryable<T> ApplyFilter<T>(IQueryable<T> query, PropertyInfo prop, string valueStr, string? displayType)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(param, prop);
        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        object? typed;
        try
        {
            typed = ConvertFilterValue(valueStr, targetType);
        }
        catch
        {
            return query;
        }

        Expression body;
        if (string.Equals(displayType, "like", StringComparison.OrdinalIgnoreCase) && targetType == typeof(string))
        {
            var constVal = Expression.Constant((string?)typed, typeof(string));
            var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
            var contains = Expression.Call(member, nameof(string.Contains), Type.EmptyTypes, constVal);
            body = Expression.AndAlso(notNull, contains);
        }
        else
        {
            var constVal = Expression.Constant(typed, prop.PropertyType);
            body = Expression.Equal(member, constVal);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(body, param);
        return query.Where(lambda);
    }

    static object? ConvertFilterValue(string valueStr, Type targetType)
    {
        if (targetType == typeof(string)) return valueStr;
        if (targetType == typeof(bool)) return bool.Parse(valueStr);
        if (targetType == typeof(DateTime)) return DateTime.Parse(valueStr, CultureInfo.InvariantCulture);
        if (targetType.IsEnum) return Enum.Parse(targetType, valueStr, true);
        return Convert.ChangeType(valueStr, targetType, CultureInfo.InvariantCulture);
    }

    static IQueryable<T> ApplySort<T>(IQueryable<T> query, string sort, string? order)
    {
        var prop = typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals(sort, StringComparison.OrdinalIgnoreCase));
        if (prop == null) return query;

        var param = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
        var body = System.Linq.Expressions.Expression.Property(param, prop);
        var keySelector = System.Linq.Expressions.Expression.Lambda(body, param);
        var method = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase)
            ? "OrderBy"
            : "OrderByDescending";
        var call = System.Linq.Expressions.Expression.Call(
            typeof(Queryable),
            method,
            [typeof(T), prop.PropertyType],
            query.Expression,
            System.Linq.Expressions.Expression.Quote(keySelector));
        return query.Provider.CreateQuery<T>(call);
    }
}

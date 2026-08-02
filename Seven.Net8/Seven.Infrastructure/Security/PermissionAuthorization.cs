using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Security;

/// <summary>权限要求</summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;
    public string Permission { get; }
}

/// <summary>权限 Handler：校验当前用户角色是否包含权限码</summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PermissionAuthorizationHandler(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true) return;

        var roleClaim = context.User.FindFirst("RoleId")?.Value;
        if (!int.TryParse(roleClaim, out var roleId)) return;
        if (roleId == 1)
        {
            context.Succeed(requirement);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        // 通过缓存/服务取权限：复用 GetMyPermissions 需要当前用户；改为直接查库
        var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
        var perms = await db.Sys_RoleAuths.AsNoTracking()
            .Where(a => a.Role_Id == roleId && !a.IsDeleted)
            .Select(a => a.AuthValue)
            .ToListAsync();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in perms)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            foreach (var p in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                set.Add(p);
            // AuthValue 也可能是 "Search,Add" 挂在菜单 TableName 上 — 查菜单拼权限
        }

        // 拼装 TableName.Action（与 AuthService 一致）
        var authRows = await (
            from a in db.Sys_RoleAuths.AsNoTracking()
            join m in db.Sys_Menus.AsNoTracking() on a.Menu_Id equals m.Menu_Id
            where a.Role_Id == roleId && !a.IsDeleted && !m.IsDeleted
            select new { m.TableName, a.AuthValue }
        ).ToListAsync();
        foreach (var row in authRows)
        {
            if (string.IsNullOrWhiteSpace(row.TableName) || string.IsNullOrWhiteSpace(row.AuthValue)) continue;
            foreach (var action in row.AuthValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                set.Add($"{row.TableName}.{action}");
        }

        if (set.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}

/// <summary>权限特性：同时注册 Policy</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    public PermissionAttribute(string permission) : base(policy: $"perm:{permission}")
    {
        Permission = permission;
    }

    public string Permission { get; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Policy 由动态注册处理；此处留空
    }
}

/// <summary>动态注册 perm:* Policy</summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("perm:", StringComparison.OrdinalIgnoreCase))
        {
            var perm = policyName["perm:".Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(perm))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}

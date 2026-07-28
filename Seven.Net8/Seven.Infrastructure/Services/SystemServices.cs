using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
using Seven.Application.Models;
using Seven.Domain.Common;
using Seven.Domain.Entities.System;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Services;

/// <summary>用户管理服务</summary>
public class SysUserService : ISysUserService
{
    private readonly SevenDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ICacheService _cache;

    /// <summary>构造函数</summary>
    public SysUserService(SevenDbContext db, IPasswordHasher hasher, ICacheService cache)
    {
        _db = db;
        _hasher = hasher;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<PageGridData<Sys_User>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Sys_Users.AsNoTracking().OrderByDescending(u => u.User_Id);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Sys_Users.AsNoTracking().FirstOrDefaultAsync(u => u.User_Id == id, cancellationToken);
        return user == null ? WebResponseContent.Error("用户不存在") : WebResponseContent.Ok(data: user);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> AddAsync(Sys_User entity, CancellationToken cancellationToken = default)
    {
        entity.PasswordHash = _hasher.HashPassword(entity.PasswordHash.Length > 0 ? entity.PasswordHash : "123456");
        entity.CreateDate = DateTime.Now;
        _db.Sys_Users.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync($"user:{entity.User_Id}");
        return WebResponseContent.Ok("添加成功", entity);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> UpdateAsync(Sys_User entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Sys_Users.FindAsync([entity.User_Id], cancellationToken);
        if (existing == null) return WebResponseContent.Error("用户不存在");

        existing.UserTrueName = entity.UserTrueName;
        existing.Role_Id = entity.Role_Id;
        existing.RoleName = entity.RoleName;
        existing.Enable = entity.Enable;
        existing.PhoneNo = entity.PhoneNo;
        existing.Email = entity.Email;
        existing.Remark = entity.Remark;
        existing.ModifyDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync($"user:{entity.User_Id}");
        return WebResponseContent.Ok("更新成功");
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var users = await _db.Sys_Users.Where(u => ids.Contains(u.User_Id)).ToListAsync(cancellationToken);
        foreach (var u in users) u.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var id in ids) await _cache.RemoveWithDelayedDoubleDeleteAsync($"user:{id}");
        return WebResponseContent.Ok("删除成功");
    }
}

/// <summary>角色管理服务</summary>
public class SysRoleService : ISysRoleService
{
    private const int SuperAdminRoleId = 1;

    private static readonly Dictionary<string, string> ActionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Search"] = "查询",
        ["Add"] = "新增",
        ["Update"] = "编辑",
        ["Delete"] = "删除",
        ["Export"] = "导出",
        ["Import"] = "导入",
        ["Upload"] = "上传",
        ["Audit"] = "审核",
        ["Acknowledge"] = "确认",
        ["Clear"] = "清除",
        ["Raise"] = "触发",
        ["BatchCustom"] = "批量处理",
        ["RowCustom"] = "行内处理",
    };

    private readonly SevenDbContext _db;
    private readonly ICacheService _cache;
    private readonly ICurrentUserService _currentUser;

    /// <summary>构造函数</summary>
    public SysRoleService(SevenDbContext db, ICacheService cache, ICurrentUserService currentUser)
    {
        _db = db;
        _cache = cache;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<PageGridData<Sys_Role>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Sys_Roles.AsNoTracking().OrderBy(r => r.OrderNo);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Sys_Roles.Include(r => r.RoleAuths).AsNoTracking().FirstOrDefaultAsync(r => r.Role_Id == id, cancellationToken);
        return role == null ? WebResponseContent.Error("角色不存在") : WebResponseContent.Ok(data: role);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> AddAsync(Sys_Role entity, CancellationToken cancellationToken = default)
    {
        entity.CreateDate = DateTime.Now;
        _db.Sys_Roles.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync("menu:tree");
        return WebResponseContent.Ok("添加成功", entity);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> UpdateAsync(Sys_Role entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Sys_Roles.FindAsync([entity.Role_Id], cancellationToken);
        if (existing == null) return WebResponseContent.Error("角色不存在");
        existing.RoleName = entity.RoleName;
        existing.ParentId = entity.ParentId;
        existing.Enable = entity.Enable;
        existing.OrderNo = entity.OrderNo;
        existing.ModifyDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync($"role:{entity.Role_Id}");
        return WebResponseContent.Ok("更新成功");
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var roles = await _db.Sys_Roles.Where(r => ids.Contains(r.Role_Id)).ToListAsync(cancellationToken);
        foreach (var r in roles) r.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetCurrentTreePermissionAsync(CancellationToken cancellationToken = default)
    {
        var currentRoleId = _currentUser.RoleId ?? 0;
        if (currentRoleId <= 0) return WebResponseContent.Error("未登录");

        var menus = await _db.Sys_Menus.AsNoTracking()
            .Where(m => m.Enable == 1 && !m.IsDeleted)
            .OrderByDescending(m => m.OrderNo)
            .ThenByDescending(m => m.ParentId)
            .ToListAsync(cancellationToken);

        var grantable = await BuildGrantableAuthMapAsync(currentRoleId, menus, cancellationToken);
        var tree = BuildPermissionNodes(menus, grantable, includeFolders: true, includeAllForSuperAdmin: currentRoleId == SuperAdminRoleId);
        return WebResponseContent.Ok(data: tree);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetUserTreePermissionAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var currentRoleId = _currentUser.RoleId ?? 0;
        if (currentRoleId <= 0) return WebResponseContent.Error("未登录");
        if (roleId <= 0) return WebResponseContent.Error("角色无效");

        var role = await _db.Sys_Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Role_Id == roleId && !r.IsDeleted, cancellationToken);
        if (role == null) return WebResponseContent.Error("角色不存在");

        if (!CanManageRole(currentRoleId, roleId))
            return WebResponseContent.Error("没有权限查看此角色的权限");

        var menus = await _db.Sys_Menus.AsNoTracking()
            .Where(m => m.Enable == 1 && !m.IsDeleted)
            .OrderBy(m => m.OrderNo)
            .ToListAsync(cancellationToken);

        var roleAuths = await _db.Sys_RoleAuths.AsNoTracking()
            .Where(a => a.Role_Id == roleId && !string.IsNullOrWhiteSpace(a.AuthValue))
            .ToListAsync(cancellationToken);

        var menuMap = menus.ToDictionary(m => m.Menu_Id);
        var assigned = roleAuths
            .Where(a => menuMap.ContainsKey(a.Menu_Id))
            .Select(a =>
            {
                var menu = menuMap[a.Menu_Id];
                var allowed = ParseAuthValues(a.AuthValue);
                return new MenuPermissionNodeDto
                {
                    Id = menu.Menu_Id,
                    Pid = menu.ParentId,
                    Text = menu.MenuName,
                    IsApp = menu.MenuType == 1,
                    Actions = allowed.Select(v => ToActionDto(v)).ToList(),
                };
            })
            .Where(x => x.Actions.Count > 0)
            .ToList();

        return WebResponseContent.Ok(data: assigned);
    }

    /// <inheritdoc />
    public Task<WebResponseContent> SavePermissionAsync(SaveRolePermissionRequest request, CancellationToken cancellationToken = default) =>
        SavePermissionInternalAsync(request.RoleId, request.Permissions, cancellationToken);

    /// <inheritdoc />
    public async Task<WebResponseContent> SavePermissionAsync(int roleId, int[] menuIds, Dictionary<int, string> authValues, CancellationToken cancellationToken = default)
    {
        var permissions = menuIds
            .Select(id => new RolePermissionSaveItem
            {
                Id = id,
                Actions = ParseAuthValues(authValues.GetValueOrDefault(id))
                    .Select(v => ToActionDto(v))
                    .ToList(),
            })
            .Where(x => x.Actions.Count > 0)
            .ToList();

        return await SavePermissionInternalAsync(roleId, permissions, cancellationToken);
    }

    private async Task<WebResponseContent> SavePermissionInternalAsync(int roleId, List<RolePermissionSaveItem> permissions, CancellationToken cancellationToken)
    {
        var currentRoleId = _currentUser.RoleId ?? 0;
        if (currentRoleId <= 0) return WebResponseContent.Error("未登录");
        if (roleId <= 0) return WebResponseContent.Error("角色无效");
        if (!CanManageRole(currentRoleId, roleId))
            return WebResponseContent.Error("没有权限修改此角色的权限");

        var menus = await _db.Sys_Menus.AsNoTracking()
            .Where(m => m.Enable == 1 && !m.IsDeleted)
            .ToListAsync(cancellationToken);
        var grantable = await BuildGrantableAuthMapAsync(currentRoleId, menus, cancellationToken);

        var authValues = new Dictionary<int, string>();
        foreach (var item in permissions)
        {
            if (!grantable.TryGetValue(item.Id, out var allowed)) continue;
            var values = item.Actions
                .Select(a => a.Value?.Trim())
                .Where(v => !string.IsNullOrEmpty(v) && allowed.Contains(v!, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (values.Length > 0)
                authValues[item.Id] = string.Join(",", values);
        }

        var menuIds = authValues.Keys.ToArray();
        var existing = await _db.Sys_RoleAuths.Where(a => a.Role_Id == roleId).ToListAsync(cancellationToken);
        _db.Sys_RoleAuths.RemoveRange(existing);

        foreach (var menuId in menuIds)
        {
            _db.Sys_RoleAuths.Add(new Sys_RoleAuth
            {
                Role_Id = roleId,
                Menu_Id = menuId,
                AuthValue = authValues[menuId],
                CreateDate = DateTime.Now,
                ModifyDate = DateTime.Now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync($"role:{roleId}");
        return WebResponseContent.Ok("权限保存成功");
    }

    private bool CanManageRole(int currentRoleId, int targetRoleId)
    {
        if (currentRoleId == SuperAdminRoleId) return true;
        if (targetRoleId == SuperAdminRoleId) return false;
        return true;
    }

    private async Task<Dictionary<int, HashSet<string>>> BuildGrantableAuthMapAsync(int roleId, List<Sys_Menu> menus, CancellationToken cancellationToken)
    {
        if (roleId == SuperAdminRoleId)
        {
            return menus
                .Where(m => !string.IsNullOrWhiteSpace(m.Auth))
                .ToDictionary(
                    m => m.Menu_Id,
                    m => ParseAuthValues(m.Auth).ToHashSet(StringComparer.OrdinalIgnoreCase));
        }

        var auths = await _db.Sys_RoleAuths.AsNoTracking()
            .Where(a => a.Role_Id == roleId)
            .ToListAsync(cancellationToken);

        var map = new Dictionary<int, HashSet<string>>();
        foreach (var auth in auths)
        {
            var menu = menus.FirstOrDefault(m => m.Menu_Id == auth.Menu_Id);
            var values = ParseAuthValues(auth.AuthValue ?? menu?.Auth);
            if (values.Length == 0) continue;
            map[auth.Menu_Id] = values.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        return map;
    }

    private static List<MenuPermissionNodeDto> BuildPermissionNodes(
        List<Sys_Menu> menus,
        Dictionary<int, HashSet<string>> grantable,
        bool includeFolders,
        bool includeAllForSuperAdmin = false)
    {
        var menuMap = menus.ToDictionary(m => m.Menu_Id);
        var menuIdsWithActions = grantable.Keys.ToHashSet();
        var visibleIds = includeAllForSuperAdmin
            ? menus.Select(m => m.Menu_Id).ToHashSet()
            : new HashSet<int>(menuIdsWithActions);

        if (includeFolders)
        {
            foreach (var menuId in menuIdsWithActions)
            {
                var current = menuMap.GetValueOrDefault(menuId)?.ParentId ?? 0;
                while (current > 0)
                {
                    if (!visibleIds.Add(current)) break;
                    current = menuMap.GetValueOrDefault(current)?.ParentId ?? 0;
                }
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var menu in menus)
                {
                    if (visibleIds.Contains(menu.Menu_Id)) continue;
                    if (menus.Any(c => c.ParentId == menu.Menu_Id && visibleIds.Contains(c.Menu_Id)))
                    {
                        visibleIds.Add(menu.Menu_Id);
                        changed = true;
                    }
                }
            }
        }

        return menus
            .Where(m => visibleIds.Contains(m.Menu_Id))
            .OrderByDescending(m => m.OrderNo)
            .ThenByDescending(m => m.ParentId)
            .Select(m =>
            {
                grantable.TryGetValue(m.Menu_Id, out var allowed);
                return new MenuPermissionNodeDto
                {
                    Id = m.Menu_Id,
                    Pid = m.ParentId,
                    Text = m.MenuName,
                    IsApp = m.MenuType == 1,
                    Actions = allowed == null
                        ? []
                        : allowed.Select(ToActionDto).ToList(),
                };
            })
            .ToList();
    }

    private static string[] ParseAuthValues(string? auth) =>
        (auth ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static PermissionActionDto ToActionDto(string value) =>
        new()
        {
            Value = value,
            Text = ActionLabels.TryGetValue(value, out var label) ? label : value,
        };
}

/// <summary>菜单管理服务</summary>
public class SysMenuService : ISysMenuService
{
    private readonly SevenDbContext _db;
    private readonly ICacheService _cache;
    private readonly ICurrentUserService _currentUser;

    /// <summary>构造函数</summary>
    public SysMenuService(SevenDbContext db, ICacheService cache, ICurrentUserService currentUser)
    {
        _db = db;
        _cache = cache;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<Sys_Menu>>("menu:tree", cancellationToken);
        if (cached != null) return WebResponseContent.Ok(data: cached);

        var menus = await _db.Sys_Menus.AsNoTracking()
            .Where(m => m.Enable == 1 && !m.IsDeleted)
            .OrderByDescending(m => m.OrderNo)
            .ThenByDescending(m => m.ParentId)
            .ToListAsync(cancellationToken);
        await _cache.SetAsync("menu:tree", menus, TimeSpan.FromHours(1), cancellationToken);
        return WebResponseContent.Ok(data: menus);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetMenuListAsync(CancellationToken cancellationToken = default)
    {
        var menus = await _db.Sys_Menus.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.OrderNo)
            .ThenByDescending(m => m.ParentId)
            .Select(m => new
            {
                m.Menu_Id,
                m.ParentId,
                m.MenuName,
                m.OrderNo,
                m.Enable,
                m.MenuType,
            })
            .ToListAsync(cancellationToken);
        return WebResponseContent.Ok(data: menus);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetTreeItemAsync(int menuId, CancellationToken cancellationToken = default)
    {
        if (menuId <= 0) return WebResponseContent.Error("菜单无效");
        var menu = await _db.Sys_Menus.AsNoTracking()
            .Where(m => m.Menu_Id == menuId && !m.IsDeleted)
            .Select(m => new
            {
                m.Menu_Id,
                m.ParentId,
                m.MenuName,
                m.Url,
                m.TableName,
                m.Auth,
                m.Icon,
                m.OrderNo,
                m.Enable,
                m.MenuType,
                m.CreateDate,
                m.ModifyDate,
            })
            .FirstOrDefaultAsync(cancellationToken);
        return menu == null ? WebResponseContent.Error("菜单不存在") : WebResponseContent.Ok(data: menu);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetMenuByRoleAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var menuIds = await _db.Sys_RoleAuths.Where(a => a.Role_Id == roleId).Select(a => a.Menu_Id).ToListAsync(cancellationToken);
        var allMenus = await _db.Sys_Menus.AsNoTracking().Where(m => m.Enable == 1 && !m.IsDeleted).ToListAsync(cancellationToken);
        var resultIds = new HashSet<int>(menuIds);

        foreach (var menu in allMenus.Where(m => menuIds.Contains(m.Menu_Id)))
        {
            var parentId = menu.ParentId;
            while (parentId > 0)
            {
                if (!resultIds.Add(parentId)) break;
                parentId = allMenus.FirstOrDefault(m => m.Menu_Id == parentId)?.ParentId ?? 0;
            }
        }

        var menus = allMenus.Where(m => resultIds.Contains(m.Menu_Id)).OrderBy(m => m.OrderNo).ToList();
        return WebResponseContent.Ok(data: menus);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetCurrentUserMenuAsync(CancellationToken cancellationToken = default)
    {
        var roleId = _currentUser.RoleId ?? 0;
        return await GetMenuByRoleAsync(roleId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WebResponseContent> SaveAsync(Sys_Menu entity, CancellationToken cancellationToken = default) =>
        SaveInternalAsync(entity, cancellationToken);

    /// <inheritdoc />
    public async Task<WebResponseContent> AddAsync(Sys_Menu entity, CancellationToken cancellationToken = default)
    {
        entity.Menu_Id = 0;
        return await SaveInternalAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> UpdateAsync(Sys_Menu entity, CancellationToken cancellationToken = default) =>
        await SaveInternalAsync(entity, cancellationToken);

    private async Task<WebResponseContent> SaveInternalAsync(Sys_Menu entity, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entity.MenuName))
            return WebResponseContent.Error("菜单名称不能为空");

        if (entity.Menu_Id > 0 && entity.Menu_Id == entity.ParentId)
            return WebResponseContent.Error("父级ID不能是当前菜单的ID");

        if (entity.Menu_Id > 0 &&
            await _db.Sys_Menus.AnyAsync(m => m.ParentId == entity.Menu_Id && m.Menu_Id == entity.ParentId && !m.IsDeleted, cancellationToken))
            return WebResponseContent.Error("不能选择此父级ID，会形成循环依赖");

        var tableName = entity.TableName?.Trim();
        if (!string.IsNullOrEmpty(tableName) && tableName is not "." and not "/")
        {
            var duplicate = await _db.Sys_Menus.AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.TableName == tableName &&
                    !m.IsDeleted &&
                    (m.MenuType ?? 0) == (entity.MenuType ?? 0) &&
                    (entity.Menu_Id <= 0 || m.Menu_Id != entity.Menu_Id),
                    cancellationToken);
            if (duplicate != null)
                return WebResponseContent.Error($"视图/表名【{tableName}】已被其他菜单使用");
        }

        if (entity.Menu_Id <= 0)
        {
            entity.CreateDate = DateTime.Now;
            entity.IsDeleted = false;
            _db.Sys_Menus.Add(entity);
        }
        else
        {
            var existing = await _db.Sys_Menus.FirstOrDefaultAsync(m => m.Menu_Id == entity.Menu_Id && !m.IsDeleted, cancellationToken);
            if (existing == null) return WebResponseContent.Error("菜单不存在");
            existing.MenuName = entity.MenuName;
            existing.ParentId = entity.ParentId;
            existing.Url = entity.Url;
            existing.TableName = entity.TableName;
            existing.Icon = entity.Icon;
            existing.Auth = entity.Auth;
            existing.OrderNo = entity.OrderNo;
            existing.Enable = entity.Enable;
            existing.MenuType = entity.MenuType;
            existing.ModifyDate = DateTime.Now;
            entity = existing;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync("menu:tree");
        return WebResponseContent.Ok("保存成功", entity);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return WebResponseContent.Error("菜单无效");

        var menu = await _db.Sys_Menus.FirstOrDefaultAsync(m => m.Menu_Id == id && !m.IsDeleted, cancellationToken);
        if (menu == null) return WebResponseContent.Error("菜单不存在");

        if (await _db.Sys_Menus.AnyAsync(m => m.ParentId == id && !m.IsDeleted, cancellationToken))
            return WebResponseContent.Error("当前菜单存在子菜单，请先删除子菜单");

        var roleAuths = await _db.Sys_RoleAuths.Where(a => a.Menu_Id == id).ToListAsync(cancellationToken);
        if (roleAuths.Count > 0)
            _db.Sys_RoleAuths.RemoveRange(roleAuths);

        menu.IsDeleted = true;
        menu.ModifyDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync("menu:tree");
        return WebResponseContent.Ok("删除成功");
    }
}

/// <summary>部门管理服务</summary>
public class SysDepartmentService : ISysDepartmentService
{
    private readonly SevenDbContext _db;
    private readonly ICacheService _cache;

    /// <summary>构造函数</summary>
    public SysDepartmentService(SevenDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var depts = await _db.Sys_Departments.AsNoTracking().Where(d => d.Enable == 1).OrderBy(d => d.OrderNo).ToListAsync(cancellationToken);
        return WebResponseContent.Ok(data: depts);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> AddAsync(Sys_Department entity, CancellationToken cancellationToken = default)
    {
        entity.CreateDate = DateTime.Now;
        _db.Sys_Departments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync("dept:tree");
        return WebResponseContent.Ok("添加成功", entity);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> UpdateAsync(Sys_Department entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Sys_Departments.FindAsync([entity.DepartmentId], cancellationToken);
        if (existing == null) return WebResponseContent.Error("部门不存在");
        existing.DepartmentName = entity.DepartmentName;
        existing.DepartmentCode = entity.DepartmentCode;
        existing.ParentId = entity.ParentId;
        existing.OrderNo = entity.OrderNo;
        existing.ModifyDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync("dept:tree");
        return WebResponseContent.Ok("更新成功");
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var dept = await _db.Sys_Departments.FindAsync([id], cancellationToken);
        if (dept == null) return WebResponseContent.Error("部门不存在");
        dept.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync("dept:tree");
        return WebResponseContent.Ok("删除成功");
    }
}

/// <summary>字典管理服务</summary>
public class SysDictionaryService : ISysDictionaryService
{
    private readonly SevenDbContext _db;
    private readonly ICacheService _cache;

    /// <summary>构造函数</summary>
    public SysDictionaryService(SevenDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Sys_Dictionaries.AsNoTracking().Include(d => d.DictionaryLists).OrderBy(d => d.OrderNo);
        var data = await CrudHelper.PaginateAsync(query, options, cancellationToken);
        return WebResponseContent.Ok(data: data);
    }

    /// <inheritdoc />
    public Task<WebResponseContent> GetBuilderDictionaryAsync(CancellationToken cancellationToken = default)
    {
        var data = new object[]
        {
            new { key = "searchType", data = new[] { new { dicName = "等于", dicValue = "equal" }, new { dicName = "模糊", dicValue = "like" } } },
            new { key = "editType", data = new[] { new { dicName = "文本", dicValue = "text" }, new { dicName = "下拉", dicValue = "select" }, new { dicName = "日期", dicValue = "date" }, new { dicName = "开关", dicValue = "switch" } } },
            new { key = "columnType", data = new[] { new { dicName = "字符串", dicValue = "nvarchar" }, new { dicName = "整型", dicValue = "int" }, new { dicName = "日期", dicValue = "datetime" }, new { dicName = "布尔", dicValue = "bool" } } },
        };
        return Task.FromResult(WebResponseContent.Ok(data: data));
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetVueDictionaryAsync(string[] dicNos, CancellationToken cancellationToken = default)
    {
        var result = new List<object>();
        foreach (var dicNo in dicNos)
        {
            var cacheKey = $"dict:{dicNo}";
            var cached = await _cache.GetAsync<List<Sys_DictionaryList>>(cacheKey, cancellationToken);
            if (cached == null)
            {
                var dict = await _db.Sys_Dictionaries.Include(d => d.DictionaryLists)
                    .FirstOrDefaultAsync(d => d.DicNo == dicNo, cancellationToken);
                cached = dict?.DictionaryLists.Where(l => l.Enable == 1).OrderBy(l => l.OrderNo).ToList() ?? [];
                await _cache.SetAsync(cacheKey, cached, TimeSpan.FromHours(2), cancellationToken);
            }
            result.Add(new { key = dicNo, data = cached });
        }
        return WebResponseContent.Ok(data: result);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> AddAsync(Sys_Dictionary entity, CancellationToken cancellationToken = default)
    {
        entity.CreateDate = DateTime.Now;
        _db.Sys_Dictionaries.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync($"dict:{entity.DicNo}");
        return WebResponseContent.Ok("添加成功", entity);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> UpdateAsync(Sys_Dictionary entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Sys_Dictionaries.FindAsync([entity.Dic_ID], cancellationToken);
        if (existing == null) return WebResponseContent.Error("字典不存在");
        existing.DicName = entity.DicName;
        existing.DicNo = entity.DicNo;
        existing.ModifyDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync($"dict:{entity.DicNo}");
        return WebResponseContent.Ok("更新成功");
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var dicts = await _db.Sys_Dictionaries.Where(d => ids.Contains(d.Dic_ID)).ToListAsync(cancellationToken);
        foreach (var d in dicts) { d.IsDeleted = true; await _cache.RemoveWithDelayedDoubleDeleteAsync($"dict:{d.DicNo}"); }
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }
}

/// <summary>日志查询服务</summary>
public class SysLogService : ISysLogService
{
    private readonly SevenDbContext _db;

    /// <summary>构造函数</summary>
    public SysLogService(SevenDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<PageGridData<Sys_Log>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Sys_Logs.AsNoTracking().OrderByDescending(l => l.CreateDate);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }
}

/// <summary>工作流服务</summary>
public class WorkFlowService : IWorkFlowService
{
    private readonly SevenDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>构造函数</summary>
    public WorkFlowService(SevenDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> SubmitAsync(string tableName, string tableKey, CancellationToken cancellationToken = default)
    {
        var flow = await _db.Sys_WorkFlows.FirstOrDefaultAsync(f => f.WorkTable == tableName && f.Enable == 1, cancellationToken);
        if (flow == null) return WebResponseContent.Error("未配置工作流");

        var instance = new Domain.Entities.Flow.Sys_WorkFlowTable
        {
            WorkFlow_Id = flow.WorkFlow_Id,
            WorkTable = tableName,
            WorkTableKey = tableKey,
            AuditStatus = 0,
            CreateDate = DateTime.Now
        };
        _db.Sys_WorkFlowTables.Add(instance);
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("提交审批成功", instance);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> AuditAsync(int workFlowTableId, int auditStatus, string? remark, CancellationToken cancellationToken = default)
    {
        var instance = await _db.Sys_WorkFlowTables.FindAsync([workFlowTableId], cancellationToken);
        if (instance == null) return WebResponseContent.Error("流程实例不存在");

        instance.AuditStatus = auditStatus;
        instance.ModifyDate = DateTime.Now;
        _db.Sys_WorkFlowTableAuditLogs.Add(new Domain.Entities.Flow.Sys_WorkFlowTableAuditLog
        {
            WorkFlowTable_Id = workFlowTableId,
            AuditStatus = auditStatus,
            Remark = remark,
            AuditUser = _currentUser.UserName,
            CreateDate = DateTime.Now
        });
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("审批完成");
    }

    /// <inheritdoc />
    public async Task<PageGridData<Domain.Entities.Flow.Sys_WorkFlowTable>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Sys_WorkFlowTables.AsNoTracking().OrderByDescending(w => w.CreateDate);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }
}

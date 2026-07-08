using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
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
    private readonly SevenDbContext _db;
    private readonly ICacheService _cache;

    /// <summary>构造函数</summary>
    public SysRoleService(SevenDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
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
    public async Task<WebResponseContent> SavePermissionAsync(int roleId, int[] menuIds, Dictionary<int, string> authValues, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Sys_RoleAuths.Where(a => a.Role_Id == roleId).ToListAsync(cancellationToken);
        _db.Sys_RoleAuths.RemoveRange(existing);
        foreach (var menuId in menuIds)
        {
            _db.Sys_RoleAuths.Add(new Sys_RoleAuth
            {
                Role_Id = roleId,
                Menu_Id = menuId,
                AuthValue = authValues.GetValueOrDefault(menuId)
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync($"role:{roleId}");
        return WebResponseContent.Ok("权限保存成功");
    }
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

        var menus = await _db.Sys_Menus.AsNoTracking().Where(m => m.Enable == 1).OrderBy(m => m.OrderNo).ToListAsync(cancellationToken);
        await _cache.SetAsync("menu:tree", menus, TimeSpan.FromHours(1), cancellationToken);
        return WebResponseContent.Ok(data: menus);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetMenuByRoleAsync(int roleId, CancellationToken cancellationToken = default)
    {
        var menuIds = await _db.Sys_RoleAuths.Where(a => a.Role_Id == roleId).Select(a => a.Menu_Id).ToListAsync(cancellationToken);
        var menus = await _db.Sys_Menus.AsNoTracking().Where(m => menuIds.Contains(m.Menu_Id) && m.Enable == 1).OrderBy(m => m.OrderNo).ToListAsync(cancellationToken);
        return WebResponseContent.Ok(data: menus);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetCurrentUserMenuAsync(CancellationToken cancellationToken = default)
    {
        var roleId = _currentUser.RoleId ?? 0;
        return await GetMenuByRoleAsync(roleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> AddAsync(Sys_Menu entity, CancellationToken cancellationToken = default)
    {
        entity.CreateDate = DateTime.Now;
        _db.Sys_Menus.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync("menu:tree");
        return WebResponseContent.Ok("添加成功", entity);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> UpdateAsync(Sys_Menu entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Sys_Menus.FindAsync([entity.Menu_Id], cancellationToken);
        if (existing == null) return WebResponseContent.Error("菜单不存在");
        existing.MenuName = entity.MenuName;
        existing.Url = entity.Url;
        existing.Icon = entity.Icon;
        existing.Auth = entity.Auth;
        existing.OrderNo = entity.OrderNo;
        existing.Enable = entity.Enable;
        existing.ModifyDate = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);
        await _cache.RemoveWithDelayedDoubleDeleteAsync("menu:tree");
        return WebResponseContent.Ok("更新成功");
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var menu = await _db.Sys_Menus.FindAsync([id], cancellationToken);
        if (menu == null) return WebResponseContent.Error("菜单不存在");
        menu.IsDeleted = true;
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

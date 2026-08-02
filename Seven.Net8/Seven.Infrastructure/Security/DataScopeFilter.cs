using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.System;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Security;

/// <summary>数据权限：按角色 DataScope 过滤 CreateId / 部门</summary>
public interface IDataScopeService
{
    Task<DataScope> GetCurrentScopeAsync(CancellationToken ct = default);
    Task<HashSet<int>> GetAllowedUserIdsAsync(CancellationToken ct = default);
    Task<IQueryable<T>> ApplyCreateIdScopeAsync<T>(IQueryable<T> query, CancellationToken ct = default) where T : BaseEntity;
}

public class DataScopeService : IDataScopeService
{
    private readonly SevenDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly FeatureOptions _features;

    public DataScopeService(SevenDbContext db, ICurrentUserService currentUser, IOptions<FeatureOptions> features)
    {
        _db = db;
        _currentUser = currentUser;
        _features = features.Value;
    }

    public async Task<DataScope> GetCurrentScopeAsync(CancellationToken ct = default)
    {
        if (!_features.DataScope) return DataScope.All;
        if (_currentUser.RoleId is not int rid) return DataScope.Self;
        if (rid == 1) return DataScope.All;
        var role = await _db.Sys_Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Role_Id == rid, ct);
        return (DataScope)(role?.DataScope ?? (int)DataScope.All);
    }

    public async Task<HashSet<int>> GetAllowedUserIdsAsync(CancellationToken ct = default)
    {
        var scope = await GetCurrentScopeAsync(ct);
        var uid = _currentUser.UserId ?? 0;
        if (scope == DataScope.All) return [];
        if (scope == DataScope.Self) return [uid];

        var me = await _db.Sys_Users.AsNoTracking().FirstOrDefaultAsync(u => u.User_Id == uid, ct);
        var myDepts = ParseIds(me?.DeptIds);
        if (scope == DataScope.DepartmentAndChildren)
        {
            var allDepts = await _db.Sys_Departments.AsNoTracking().Where(d => !d.IsDeleted).ToListAsync(ct);
            myDepts = ExpandDeptTree(myDepts, allDepts);
        }

        if (myDepts.Count == 0) return [uid];

        var users = await _db.Sys_Users.AsNoTracking()
            .Where(u => !u.IsDeleted)
            .Select(u => new { u.User_Id, u.DeptIds })
            .ToListAsync(ct);
        var set = users.Where(u => ParseIds(u.DeptIds).Overlaps(myDepts)).Select(u => u.User_Id).ToHashSet();
        set.Add(uid);
        return set;
    }

    public async Task<IQueryable<T>> ApplyCreateIdScopeAsync<T>(IQueryable<T> query, CancellationToken ct = default) where T : BaseEntity
    {
        var allowed = await GetAllowedUserIdsAsync(ct);
        return FilterByCreateIds(query, allowed);
    }

    public static IQueryable<T> FilterByCreateIds<T>(IQueryable<T> query, HashSet<int> allowed) where T : BaseEntity
    {
        if (allowed.Count == 0) return query; // All
        return query.Where(x => x.CreateId != null && allowed.Contains(x.CreateId.Value));
    }

    public static IQueryable<Sys_User> FilterUsersByAllowedIds(IQueryable<Sys_User> query, HashSet<int> allowed)
    {
        if (allowed.Count == 0) return query;
        return query.Where(u => allowed.Contains(u.User_Id));
    }

    public static IQueryable<Sys_Log> FilterLogsByUserIds(IQueryable<Sys_Log> query, HashSet<int> allowed)
    {
        if (allowed.Count == 0) return query;
        return query.Where(l => l.User_Id != null && allowed.Contains(l.User_Id.Value));
    }

    static HashSet<int> ParseIds(string? csv) =>
        (csv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0).Where(id => id > 0).ToHashSet();

    static HashSet<int> ExpandDeptTree(HashSet<int> roots, List<Domain.Entities.System.Sys_Department> all)
    {
        var set = new HashSet<int>(roots);
        bool changed;
        do
        {
            changed = false;
            foreach (var d in all)
            {
                if (set.Contains(d.ParentId) && set.Add(d.DepartmentId))
                    changed = true;
            }
        } while (changed);
        return set;
    }
}

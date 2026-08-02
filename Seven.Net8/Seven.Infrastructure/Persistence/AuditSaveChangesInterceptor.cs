using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Domain.Entities.System;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Persistence;

/// <summary>记录实体字段变更到 Sys_Log（可按 HotStore:AuditExcludeEntities 排除热表）</summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly HotStoreOptions _hotStoreOptions;

    public AuditSaveChangesInterceptor(
        ICurrentUserService currentUser,
        IOptions<HotStoreOptions> hotStoreOptions)
    {
        _currentUser = currentUser;
        _hotStoreOptions = hotStoreOptions.Value;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var exclude = _hotStoreOptions.AuditExcludeEntities;
        var logs = new List<Sys_Log>();
        foreach (var entry in ctx.ChangeTracker.Entries())
        {
            if (entry.Entity is Sys_Log) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var clrName = entry.Metadata.ClrType.Name;
            var tableName = entry.Metadata.GetTableName();
            if (IsExcluded(exclude, clrName, tableName))
                continue;

            var diffs = new Dictionary<string, object?>();
            if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (!prop.IsModified) continue;
                    diffs[prop.Metadata.Name] = new { old = prop.OriginalValue, @new = prop.CurrentValue };
                }
            }
            else if (entry.State == EntityState.Added)
            {
                diffs["_state"] = "Added";
            }
            else
            {
                diffs["_state"] = "Deleted";
            }

            if (diffs.Count == 0) continue;
            logs.Add(new Sys_Log
            {
                User_Id = _currentUser.UserId,
                UserName = _currentUser.UserName,
                Url = clrName,
                RequestParameter = JsonSerializer.Serialize(diffs),
                LogType = "Audit",
                CreateDate = DateTime.Now,
            });
        }

        if (logs.Count > 0)
            ctx.Set<Sys_Log>().AddRange(logs);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static bool IsExcluded(List<string>? exclude, string clrName, string? tableName)
    {
        if (exclude == null || exclude.Count == 0) return false;
        foreach (var item in exclude)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;
            if (string.Equals(item, clrName, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrEmpty(tableName) &&
                string.Equals(item, tableName, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}

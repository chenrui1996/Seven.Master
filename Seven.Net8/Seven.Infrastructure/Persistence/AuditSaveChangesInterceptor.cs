using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Seven.Application.Interfaces;
using Seven.Domain.Entities.System;

namespace Seven.Infrastructure.Persistence;

/// <summary>记录实体字段变更到 Sys_Log</summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditSaveChangesInterceptor(ICurrentUserService currentUser) => _currentUser = currentUser;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var logs = new List<Sys_Log>();
        foreach (var entry in ctx.ChangeTracker.Entries())
        {
            if (entry.Entity is Sys_Log) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
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
                Url = entry.Metadata.ClrType.Name,
                RequestParameter = JsonSerializer.Serialize(diffs),
                LogType = "Audit",
                CreateDate = DateTime.Now,
            });
        }

        if (logs.Count > 0)
            ctx.Set<Sys_Log>().AddRange(logs);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

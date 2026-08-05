using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.DeviceComm;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.DeviceComm;

public sealed class CommConnectionService : ICommConnectionService
{
    private readonly SevenDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDeviceCommGateway _gateway;

    public CommConnectionService(SevenDbContext db, ICurrentUserService currentUser, IDeviceCommGateway gateway)
    {
        _db = db;
        _currentUser = currentUser;
        _gateway = gateway;
    }

    public Task<PageGridData<CommConnection>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default) =>
        CrudHelper.PaginateAsync(_db.Set<CommConnection>().AsNoTracking(), options, cancellationToken);

    public async Task<WebResponseContent> AddAsync(CommConnection entity, CancellationToken cancellationToken = default)
    {
        NormalizePorts(entity);
        entity.CreateDate = DateTime.Now;
        entity.CreateId = _currentUser.UserId;
        entity.Creator = _currentUser.UserName;
        entity.IsDeleted = false;
        _db.Set<CommConnection>().Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _gateway.ReloadAsync(cancellationToken);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(CommConnection entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Set<CommConnection>()
            .FirstOrDefaultAsync(x => x.CommConnectionId == entity.CommConnectionId, cancellationToken);
        if (existing == null) return WebResponseContent.Error("数据不存在");
        NormalizePorts(entity);
        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.Now;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(cancellationToken);
        await _gateway.ReloadAsync(cancellationToken);
        return WebResponseContent.Ok("更新成功");
    }

    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var list = await _db.Set<CommConnection>().Where(x => ids.Contains(x.CommConnectionId)).ToListAsync(cancellationToken);
        foreach (var item in list)
        {
            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        await _gateway.ReloadAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }

    private static void NormalizePorts(CommConnection entity)
    {
        if (entity.Port <= 0)
            entity.Port = entity.Protocol == Domain.Enums.CommProtocol.ModbusTcp ? 502 : 102;
    }
}

public sealed class CommPointService : ICommPointService
{
    private readonly SevenDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICommRuleEngine _rules;

    public CommPointService(SevenDbContext db, ICurrentUserService currentUser, ICommRuleEngine rules)
    {
        _db = db;
        _currentUser = currentUser;
        _rules = rules;
    }

    public Task<PageGridData<CommPoint>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default) =>
        CrudHelper.PaginateAsync(_db.Set<CommPoint>().AsNoTracking(), options, cancellationToken);

    public async Task<WebResponseContent> AddAsync(CommPoint entity, CancellationToken cancellationToken = default)
    {
        entity.CreateDate = DateTime.Now;
        entity.CreateId = _currentUser.UserId;
        entity.Creator = _currentUser.UserName;
        entity.IsDeleted = false;
        _db.Set<CommPoint>().Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _rules.ReloadAsync(cancellationToken);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(CommPoint entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Set<CommPoint>()
            .FirstOrDefaultAsync(x => x.CommPointId == entity.CommPointId, cancellationToken);
        if (existing == null) return WebResponseContent.Error("数据不存在");
        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.Now;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(cancellationToken);
        await _rules.ReloadAsync(cancellationToken);
        return WebResponseContent.Ok("更新成功");
    }

    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var list = await _db.Set<CommPoint>().Where(x => ids.Contains(x.CommPointId)).ToListAsync(cancellationToken);
        foreach (var item in list)
        {
            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        await _rules.ReloadAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }
}

public sealed class CommRuleService : ICommRuleService
{
    private readonly SevenDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICommRuleEngine _rules;

    public CommRuleService(SevenDbContext db, ICurrentUserService currentUser, ICommRuleEngine rules)
    {
        _db = db;
        _currentUser = currentUser;
        _rules = rules;
    }

    public Task<PageGridData<CommRule>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default) =>
        CrudHelper.PaginateAsync(_db.Set<CommRule>().AsNoTracking(), options, cancellationToken);

    public async Task<WebResponseContent> AddAsync(CommRule entity, CancellationToken cancellationToken = default)
    {
        entity.CreateDate = DateTime.Now;
        entity.CreateId = _currentUser.UserId;
        entity.Creator = _currentUser.UserName;
        entity.IsDeleted = false;
        _db.Set<CommRule>().Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _rules.ReloadAsync(cancellationToken);
        return WebResponseContent.Ok("添加成功", entity);
    }

    public async Task<WebResponseContent> UpdateAsync(CommRule entity, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Set<CommRule>()
            .FirstOrDefaultAsync(x => x.CommRuleId == entity.CommRuleId, cancellationToken);
        if (existing == null) return WebResponseContent.Error("数据不存在");
        _db.Entry(existing).CurrentValues.SetValues(entity);
        existing.ModifyDate = DateTime.Now;
        existing.IsDeleted = false;
        await _db.SaveChangesAsync(cancellationToken);
        await _rules.ReloadAsync(cancellationToken);
        return WebResponseContent.Ok("更新成功");
    }

    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var list = await _db.Set<CommRule>().Where(x => ids.Contains(x.CommRuleId)).ToListAsync(cancellationToken);
        foreach (var item in list)
        {
            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        await _rules.ReloadAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }

    public Task<PageGridData<CommEventLog>> GetEventLogsAsync(PageDataOptions options, CancellationToken cancellationToken = default) =>
        CrudHelper.PaginateAsync(_db.Set<CommEventLog>().AsNoTracking().OrderByDescending(x => x.CommEventLogId), options, cancellationToken);
}

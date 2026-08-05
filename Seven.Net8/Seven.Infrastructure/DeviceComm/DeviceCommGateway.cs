using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Domain.Entities.DeviceComm;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.DeviceComm.Drivers;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.DeviceComm;

public sealed class LiveConnection
{
    public CommConnection Config { get; set; } = null!;
    public IPlcDriver Driver { get; set; } = null!;
    public SemaphoreSlim IoLock { get; } = new(1, 1);
    public CommConnectionState State { get; set; } = CommConnectionState.Disconnected;
    public string? LastError { get; set; }
    public DateTimeOffset? LastConnectedAt { get; set; }
    public DateTimeOffset? LastDisconnectedAt { get; set; }
    public int ReconnectCount { get; set; }
    public int IoFailCount { get; set; }
    public int CurrentReconnectIntervalMs { get; set; }
    public DateTimeOffset NextReconnectAt { get; set; }
    public bool WantConnected { get; set; }
}

/// <summary>进程内连接管理：串行 IO、重试、自动重连</summary>
public sealed class DeviceCommGateway : IDeviceCommGateway
{
    private readonly ConcurrentDictionary<int, LiveConnection> _lives = new();
    private readonly IPlcDriverFactory _driverFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DeviceCommOptions> _options;
    private readonly ILogger<DeviceCommGateway> _logger;
    private IDeviceCommPushService? _push;
    private readonly object _reloadGate = new();

    public DeviceCommGateway(
        IPlcDriverFactory driverFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<DeviceCommOptions> options,
        ILogger<DeviceCommGateway> logger)
    {
        _driverFactory = driverFactory;
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public bool IsEnabled => true;

    public void SetPushService(IDeviceCommPushService push) => _push = push;

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        List<CommConnection> configs;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
            configs = await db.Set<CommConnection>()
                .AsNoTracking()
                .Where(x => x.Enabled)
                .ToListAsync(cancellationToken);
        }

        lock (_reloadGate)
        {
            var ids = configs.Select(c => c.CommConnectionId).ToHashSet();
            foreach (var id in _lives.Keys.ToList())
            {
                if (!ids.Contains(id) && _lives.TryRemove(id, out var removed))
                {
                    removed.WantConnected = false;
                    try { removed.Driver.DisconnectAsync().GetAwaiter().GetResult(); } catch { /* ignore */ }
                    removed.Driver.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }

            foreach (var cfg in configs)
            {
                if (_lives.TryGetValue(cfg.CommConnectionId, out var live))
                {
                    live.Config = cfg;
                }
                else
                {
                    _lives[cfg.CommConnectionId] = new LiveConnection
                    {
                        Config = cfg,
                        Driver = _driverFactory.Create(cfg.Protocol),
                        WantConnected = cfg.AutoConnect,
                        CurrentReconnectIntervalMs = _options.Value.Reconnect.IntervalMs
                    };
                }
            }
        }
    }

    public async Task ConnectAsync(int connectionId, CancellationToken cancellationToken = default)
    {
        var live = await EnsureLiveAsync(connectionId, cancellationToken);
        live.WantConnected = true;
        await ConnectCoreAsync(live, cancellationToken);
    }

    public async Task DisconnectAsync(int connectionId, CancellationToken cancellationToken = default)
    {
        if (!_lives.TryGetValue(connectionId, out var live))
            return;
        live.WantConnected = false;
        await live.IoLock.WaitAsync(cancellationToken);
        try
        {
            await live.Driver.DisconnectAsync(cancellationToken);
            live.State = CommConnectionState.Disconnected;
            live.LastDisconnectedAt = DateTimeOffset.UtcNow;
            live.LastError = null;
            await NotifyStatusAsync(live);
        }
        finally
        {
            live.IoLock.Release();
        }
    }

    public async Task ReconnectAsync(int connectionId, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync(connectionId, cancellationToken);
        await ConnectAsync(connectionId, cancellationToken);
    }

    public Task<IReadOnlyList<CommConnectionStatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var list = _lives.Values.Select(ToDto).ToList();
        return Task.FromResult<IReadOnlyList<CommConnectionStatusDto>>(list);
    }

    public Task<CommConnectionStatusDto?> GetStatusAsync(int connectionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_lives.TryGetValue(connectionId, out var live) ? ToDto(live) : null);
    }

    public async Task<IReadOnlyList<CommPointValueDto>> ReadPointsAsync(IEnumerable<int> pointIds, CancellationToken cancellationToken = default)
    {
        var ids = pointIds.Distinct().ToList();
        List<CommPoint> points;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
            points = await db.Set<CommPoint>().AsNoTracking()
                .Where(p => ids.Contains(p.CommPointId))
                .ToListAsync(cancellationToken);
        }

        var results = new List<CommPointValueDto>();
        foreach (var group in points.GroupBy(p => p.CommConnectionId))
        {
            var live = await EnsureLiveAsync(group.Key, cancellationToken);
            if (live.State != CommConnectionState.Connected)
                await ConnectCoreAsync(live, cancellationToken);

            foreach (var point in group)
            {
                var dto = new CommPointValueDto { CommPointId = point.CommPointId, Code = point.Code };
                try
                {
                    dto.Value = await ExecuteWithRetryAsync(live, () => live.Driver.ReadAsync(point, cancellationToken), cancellationToken);
                }
                catch (Exception ex)
                {
                    dto.Error = ex.Message;
                    live.IoFailCount++;
                }
                results.Add(dto);
            }
        }
        return results;
    }

    public async Task WritePointsAsync(IEnumerable<CommWritePointRequest> writes, CancellationToken cancellationToken = default)
    {
        var writeList = writes.ToList();
        var ids = writeList.Select(w => w.CommPointId).Distinct().ToList();
        List<CommPoint> points;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
            points = await db.Set<CommPoint>().AsNoTracking()
                .Where(p => ids.Contains(p.CommPointId))
                .ToListAsync(cancellationToken);
        }

        var map = points.ToDictionary(p => p.CommPointId);
        foreach (var group in writeList.GroupBy(w => map[w.CommPointId].CommConnectionId))
        {
            var live = await EnsureLiveAsync(group.Key, cancellationToken);
            if (live.State != CommConnectionState.Connected)
                await ConnectCoreAsync(live, cancellationToken);

            foreach (var w in group)
            {
                var point = map[w.CommPointId];
                await ExecuteWithRetryAsync(live, async () =>
                {
                    await live.Driver.WriteAsync(point, w.Value, cancellationToken);
                    return true;
                }, cancellationToken);
            }
        }
    }

    public async Task<object?> ReadRawAsync(CommRawIoRequest request, CancellationToken cancellationToken = default)
    {
        var live = await EnsureLiveAsync(request.CommConnectionId, cancellationToken);
        if (live.State != CommConnectionState.Connected)
            await ConnectCoreAsync(live, cancellationToken);
        return await ExecuteWithRetryAsync(live,
            () => live.Driver.ReadRawAsync(request.Address, request.DataType, Math.Max(1, request.Quantity), cancellationToken),
            cancellationToken);
    }

    public async Task WriteRawAsync(CommRawIoRequest request, CancellationToken cancellationToken = default)
    {
        var live = await EnsureLiveAsync(request.CommConnectionId, cancellationToken);
        if (live.State != CommConnectionState.Connected)
            await ConnectCoreAsync(live, cancellationToken);
        await ExecuteWithRetryAsync(live, async () =>
        {
            await live.Driver.WriteRawAsync(request.Address, request.DataType, request.Value, cancellationToken);
            return true;
        }, cancellationToken);
    }

    /// <summary>宿主：处理需要自动重连的连接</summary>
    public async Task ProcessReconnectsAsync(CancellationToken cancellationToken)
    {
        var opt = _options.Value.Reconnect;
        if (!opt.Enabled) return;

        foreach (var live in _lives.Values.ToList())
        {
            if (!live.WantConnected) continue;
            if (live.State is CommConnectionState.Connected or CommConnectionState.Connecting) continue;
            if (DateTimeOffset.UtcNow < live.NextReconnectAt) continue;

            live.State = CommConnectionState.Reconnecting;
            live.ReconnectCount++;
            await NotifyStatusAsync(live);
            try
            {
                await ConnectCoreAsync(live, cancellationToken);
                live.CurrentReconnectIntervalMs = opt.IntervalMs;
            }
            catch (Exception ex)
            {
                live.State = CommConnectionState.Faulted;
                live.LastError = ex.Message;
                live.CurrentReconnectIntervalMs = Math.Min(
                    Math.Max(opt.IntervalMs, live.CurrentReconnectIntervalMs * 2),
                    opt.MaxIntervalMs);
                live.NextReconnectAt = DateTimeOffset.UtcNow.AddMilliseconds(live.CurrentReconnectIntervalMs);
                _logger.LogWarning(ex, "DeviceComm 重连失败 {Id} {Name}", live.Config.CommConnectionId, live.Config.Name);
                await NotifyStatusAsync(live);
                await RaiseDisconnectAlarmAsync(live);
            }
        }
    }

    public IEnumerable<LiveConnection> GetLiveConnections() => _lives.Values;

    private async Task<LiveConnection> EnsureLiveAsync(int connectionId, CancellationToken ct)
    {
        if (_lives.TryGetValue(connectionId, out var existing))
            return existing;

        CommConnection? cfg;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
            cfg = await db.Set<CommConnection>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.CommConnectionId == connectionId && x.Enabled, ct);
        }
        if (cfg == null)
            throw new InvalidOperationException($"连接 {connectionId} 不存在或未启用");

        var live = new LiveConnection
        {
            Config = cfg,
            Driver = _driverFactory.Create(cfg.Protocol),
            CurrentReconnectIntervalMs = _options.Value.Reconnect.IntervalMs
        };
        return _lives.GetOrAdd(connectionId, live);
    }

    private async Task ConnectCoreAsync(LiveConnection live, CancellationToken ct)
    {
        await live.IoLock.WaitAsync(ct);
        try
        {
            live.State = CommConnectionState.Connecting;
            await NotifyStatusAsync(live);
            await live.Driver.ConnectAsync(live.Config, ct);
            live.State = CommConnectionState.Connected;
            live.LastConnectedAt = DateTimeOffset.UtcNow;
            live.LastError = null;
            live.IoFailCount = 0;
            await NotifyStatusAsync(live);
        }
        catch (Exception ex)
        {
            live.State = CommConnectionState.Faulted;
            live.LastError = ex.Message;
            live.LastDisconnectedAt = DateTimeOffset.UtcNow;
            live.NextReconnectAt = DateTimeOffset.UtcNow.AddMilliseconds(live.CurrentReconnectIntervalMs);
            await NotifyStatusAsync(live);
            throw;
        }
        finally
        {
            live.IoLock.Release();
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(LiveConnection live, Func<Task<T>> action, CancellationToken ct)
    {
        var retry = _options.Value.Retry;
        Exception? last = null;
        for (var attempt = 1; attempt <= Math.Max(1, retry.MaxAttempts); attempt++)
        {
            await live.IoLock.WaitAsync(ct);
            try
            {
                if (!live.Driver.IsConnected)
                    await live.Driver.ConnectAsync(live.Config, ct);
                var result = await action();
                live.State = CommConnectionState.Connected;
                return result;
            }
            catch (Exception ex)
            {
                last = ex;
                live.IoFailCount++;
                live.LastError = ex.Message;
                live.State = CommConnectionState.Faulted;
                _logger.LogWarning(ex, "DeviceComm IO 失败 attempt={Attempt} conn={Id}", attempt, live.Config.CommConnectionId);
                try { await live.Driver.DisconnectAsync(ct); } catch { /* ignore */ }
            }
            finally
            {
                live.IoLock.Release();
            }

            if (attempt < retry.MaxAttempts)
                await Task.Delay(retry.BackoffMs * attempt, ct);
        }

        live.WantConnected = true;
        live.NextReconnectAt = DateTimeOffset.UtcNow;
        await NotifyStatusAsync(live);
        throw last ?? new InvalidOperationException("DeviceComm IO 失败");
    }

    private async Task NotifyStatusAsync(LiveConnection live)
    {
        if (_push == null) return;
        try { await _push.PushConnectionStatusAsync(ToDto(live)); }
        catch (Exception ex) { _logger.LogDebug(ex, "推送连接状态失败"); }
    }

    private async Task RaiseDisconnectAlarmAsync(LiveConnection live)
    {
        if (!_options.Value.EnableAlarmOnDisconnect) return;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var alarm = scope.ServiceProvider.GetService<IAlarmService>();
            if (alarm == null) return;
            await alarm.RaiseAsync(new RaiseAlarmRequest
            {
                Code = _options.Value.AlarmCodeDisconnect,
                Source = "DeviceComm",
                DeviceName = live.Config.Name,
                Params = new Dictionary<string, string> { ["DeviceName"] = live.Config.Name },
                ExtraData = live.LastError
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "断连告警失败");
        }
    }

    private static CommConnectionStatusDto ToDto(LiveConnection live) => new()
    {
        CommConnectionId = live.Config.CommConnectionId,
        Name = live.Config.Name,
        Protocol = live.Config.Protocol,
        State = live.State,
        LastError = live.LastError,
        LastConnectedAt = live.LastConnectedAt,
        LastDisconnectedAt = live.LastDisconnectedAt,
        ReconnectCount = live.ReconnectCount,
        IoFailCount = live.IoFailCount
    };
}

/// <summary>Features.DeviceComm=false 时的占位</summary>
public sealed class DisabledDeviceCommGateway : IDeviceCommGateway
{
    public bool IsEnabled => false;

    private static InvalidOperationException Off() =>
        new("DeviceComm 未启用（Features:DeviceComm=false）");

    public Task ConnectAsync(int connectionId, CancellationToken cancellationToken = default) => throw Off();
    public Task DisconnectAsync(int connectionId, CancellationToken cancellationToken = default) => throw Off();
    public Task ReconnectAsync(int connectionId, CancellationToken cancellationToken = default) => throw Off();
    public Task<IReadOnlyList<CommConnectionStatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommConnectionStatusDto>>([]);
    public Task<CommConnectionStatusDto?> GetStatusAsync(int connectionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<CommConnectionStatusDto?>(null);
    public Task<IReadOnlyList<CommPointValueDto>> ReadPointsAsync(IEnumerable<int> pointIds, CancellationToken cancellationToken = default) => throw Off();
    public Task WritePointsAsync(IEnumerable<CommWritePointRequest> writes, CancellationToken cancellationToken = default) => throw Off();
    public Task<object?> ReadRawAsync(CommRawIoRequest request, CancellationToken cancellationToken = default) => throw Off();
    public Task WriteRawAsync(CommRawIoRequest request, CancellationToken cancellationToken = default) => throw Off();
    public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

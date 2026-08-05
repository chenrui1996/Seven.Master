using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Domain.Entities.DeviceComm;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.DeviceComm;

/// <summary>声明式组合事件规则引擎</summary>
public sealed class CommRuleEngine : ICommRuleEngine
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly DeviceCommGateway _gateway;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DeviceCommOptions> _options;
    private readonly ILogger<CommRuleEngine> _logger;
    private IDeviceCommPushService? _push;
    private readonly ConcurrentDictionary<int, RuntimeRule> _rules = new();
    private readonly ConcurrentDictionary<string, object?> _lastValues = new(StringComparer.OrdinalIgnoreCase);

    public CommRuleEngine(
        IDeviceCommGateway gateway,
        IServiceScopeFactory scopeFactory,
        IOptions<DeviceCommOptions> options,
        ILogger<CommRuleEngine> logger)
    {
        _gateway = gateway as DeviceCommGateway
            ?? throw new InvalidOperationException("CommRuleEngine 需要真实 DeviceCommGateway");
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public void SetPushService(IDeviceCommPushService push) => _push = push;

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        List<CommRule> rules;
        Dictionary<string, CommPoint> pointsByCode;
        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
            rules = await db.Set<CommRule>().AsNoTracking().Where(r => r.Enabled).ToListAsync(cancellationToken);
            pointsByCode = await db.Set<CommPoint>().AsNoTracking()
                .ToDictionaryAsync(p => p.Code, p => p, StringComparer.OrdinalIgnoreCase, cancellationToken);
        }

        _rules.Clear();
        foreach (var rule in rules)
        {
            try
            {
                var def = JsonSerializer.Deserialize<CommRuleDefinition>(rule.DefinitionJson, JsonOpts)
                          ?? new CommRuleDefinition();
                _rules[rule.CommRuleId] = new RuntimeRule(rule, def, pointsByCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载规则失败 {RuleId} {Name}", rule.CommRuleId, rule.Name);
            }
        }
    }

    public async Task ScanOnceAsync(CancellationToken cancellationToken = default)
    {
        foreach (var runtime in _rules.Values.ToList())
        {
            try
            {
                await EvaluateAsync(runtime, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "规则扫描失败 {RuleId}", runtime.Rule.CommRuleId);
            }
        }
    }

    private async Task EvaluateAsync(RuntimeRule runtime, CancellationToken ct)
    {
        var def = runtime.Definition;
        var type = (def.Type ?? "Monitor").Trim();
        switch (type)
        {
            case "Monitor":
                await HandleMonitorAsync(runtime, readExtra: false, ct);
                break;
            case "MonitorThenRead":
                await HandleMonitorAsync(runtime, readExtra: true, ct);
                break;
            case "WriteThenAwait":
                // 由 API/步骤主动触发；扫描期仅检查已挂起的等待
                await HandlePendingAwaitAsync(runtime, ct);
                break;
            case "Sequence":
                // Sequence 通常由外部触发；扫描不自动跑
                break;
            default:
                await HandleMonitorAsync(runtime, readExtra: def.Read.Count > 0, ct);
                break;
        }
    }

    /// <summary>外部触发 WriteThenAwait / Sequence</summary>
    public async Task<CommRuleEventDto> TriggerAsync(int ruleId, CancellationToken cancellationToken = default)
    {
        if (!_rules.TryGetValue(ruleId, out var runtime))
            throw new InvalidOperationException($"规则 {ruleId} 未加载或未启用");

        var type = (runtime.Definition.Type ?? "").Trim();
        if (type.Equals("WriteThenAwait", StringComparison.OrdinalIgnoreCase))
            return await ExecuteWriteThenAwaitAsync(runtime, cancellationToken);
        if (type.Equals("Sequence", StringComparison.OrdinalIgnoreCase))
            return await ExecuteSequenceAsync(runtime, cancellationToken);

        await EvaluateAsync(runtime, cancellationToken);
        return new CommRuleEventDto
        {
            CommRuleId = runtime.Rule.CommRuleId,
            RuleName = runtime.Rule.Name,
            EventName = runtime.Rule.EventName ?? runtime.Definition.Emit?.EventName ?? runtime.Rule.Name,
            Success = true,
            Message = "已执行一次扫描评估"
        };
    }

    private async Task HandleMonitorAsync(RuntimeRule runtime, bool readExtra, CancellationToken ct)
    {
        var conditions = runtime.Definition.Monitor;
        if (conditions.Count == 0) return;

        var values = await ReadByCodesAsync(conditions.Select(c => c.Point), runtime, ct);
        if (!MatchConditions(conditions, values, runtime.Definition.ConditionMode))
        {
            UpdateLast(values);
            return;
        }

        // rising/falling/changed 需要上一拍；Match 已处理
        if (readExtra && runtime.Definition.Read.Count > 0)
        {
            var extra = await ReadByCodesAsync(runtime.Definition.Read, runtime, ct);
            foreach (var kv in extra) values[kv.Key] = kv.Value;
        }

        UpdateLast(values);
        await EmitAsync(runtime, values, success: true, message: null, ct);
    }

    private async Task HandlePendingAwaitAsync(RuntimeRule runtime, CancellationToken ct)
    {
        if (runtime.PendingAwaitUntil is null) return;
        if (DateTimeOffset.UtcNow > runtime.PendingAwaitUntil)
        {
            runtime.PendingAwaitUntil = null;
            await EmitAsync(runtime, runtime.PendingValues, success: false, message: "Await 超时", ct);
            return;
        }

        var values = await ReadByCodesAsync(runtime.Definition.Await.Select(c => c.Point), runtime, ct);
        foreach (var kv in values) runtime.PendingValues[kv.Key] = kv.Value;
        if (MatchConditions(runtime.Definition.Await, values, runtime.Definition.ConditionMode))
        {
            runtime.PendingAwaitUntil = null;
            await EmitAsync(runtime, runtime.PendingValues, success: true, message: null, ct);
        }
    }

    private async Task<CommRuleEventDto> ExecuteWriteThenAwaitAsync(RuntimeRule runtime, CancellationToken ct)
    {
        var writes = runtime.Definition.Write
            .Select(w => new CommWritePointRequest
            {
                CommPointId = ResolvePoint(runtime, w.Point).CommPointId,
                Value = w.Value
            }).ToList();
        if (writes.Count > 0)
            await _gateway.WritePointsAsync(writes, ct);

        var seed = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        runtime.PendingValues = seed;
        runtime.PendingAwaitUntil = DateTimeOffset.UtcNow.AddMilliseconds(
            runtime.Definition.AwaitTimeoutMs ?? 10000);

        // 立即检查一次
        await HandlePendingAwaitAsync(runtime, ct);
        return new CommRuleEventDto
        {
            CommRuleId = runtime.Rule.CommRuleId,
            RuleName = runtime.Rule.Name,
            EventName = runtime.Rule.EventName ?? runtime.Definition.Emit?.EventName ?? runtime.Rule.Name,
            Success = runtime.PendingAwaitUntil is null,
            Values = runtime.PendingValues,
            Message = runtime.PendingAwaitUntil is null ? "条件已满足或已发出事件" : "已写入，等待条件"
        };
    }

    private async Task<CommRuleEventDto> ExecuteSequenceAsync(RuntimeRule runtime, CancellationToken ct)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var steps = runtime.Definition.Steps ?? [];
        foreach (var step in steps)
        {
            switch ((step.Action ?? "").Trim())
            {
                case "Read":
                    var read = await ReadByCodesAsync(step.Points ?? [], runtime, ct);
                    foreach (var kv in read) values[kv.Key] = kv.Value;
                    break;
                case "Write":
                    var writes = (step.Writes ?? []).Select(w => new CommWritePointRequest
                    {
                        CommPointId = ResolvePoint(runtime, w.Point).CommPointId,
                        Value = w.Value
                    });
                    await _gateway.WritePointsAsync(writes, ct);
                    break;
                case "Delay":
                    await Task.Delay(step.DelayMs ?? 100, ct);
                    break;
                case "Condition":
                    if (!MatchConditions(step.Conditions ?? [], values, step.ConditionMode ?? "all"))
                        return await EmitAsync(runtime, values, false, "Sequence 条件未满足", ct);
                    break;
                case "Emit":
                    return await EmitAsync(runtime, values, true, null, ct, step.Emit);
            }
        }
        return await EmitAsync(runtime, values, true, null, ct);
    }

    private async Task<Dictionary<string, object?>> ReadByCodesAsync(IEnumerable<string> codes, RuntimeRule runtime, CancellationToken ct)
    {
        var list = codes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var ids = list.Select(c => ResolvePoint(runtime, c).CommPointId).ToList();
        var results = await _gateway.ReadPointsAsync(ids, ct);
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in results)
        {
            if (r.Error != null)
                throw new InvalidOperationException($"读点位 {r.Code} 失败: {r.Error}");
            map[r.Code] = r.Value;
        }
        return map;
    }

    private CommPoint ResolvePoint(RuntimeRule runtime, string code)
    {
        if (runtime.PointsByCode.TryGetValue(code, out var p)) return p;
        throw new InvalidOperationException($"点位编码不存在: {code}");
    }

    private bool MatchConditions(List<CommRuleCondition> conditions, Dictionary<string, object?> values, string mode)
    {
        if (conditions.Count == 0) return true;
        var results = conditions.Select(c => MatchOne(c, values)).ToList();
        return mode.Equals("any", StringComparison.OrdinalIgnoreCase)
            ? results.Any(x => x)
            : results.All(x => x);
    }

    private bool MatchOne(CommRuleCondition c, Dictionary<string, object?> values)
    {
        values.TryGetValue(c.Point, out var current);
        _lastValues.TryGetValue(c.Point, out var previous);
        var op = (c.Op ?? "eq").ToLowerInvariant();
        return op switch
        {
            "changed" => !EqualsValue(current, previous),
            "rising" => IsTruthy(current) && !IsTruthy(previous),
            "falling" => !IsTruthy(current) && IsTruthy(previous),
            "eq" => EqualsValue(current, c.Value),
            "ne" => !EqualsValue(current, c.Value),
            "gt" => Compare(current, c.Value) > 0,
            "gte" => Compare(current, c.Value) >= 0,
            "lt" => Compare(current, c.Value) < 0,
            "lte" => Compare(current, c.Value) <= 0,
            _ => EqualsValue(current, c.Value)
        };
    }

    private void UpdateLast(Dictionary<string, object?> values)
    {
        foreach (var kv in values)
            _lastValues[kv.Key] = kv.Value;
    }

    private async Task<CommRuleEventDto> EmitAsync(
        RuntimeRule runtime,
        Dictionary<string, object?> values,
        bool success,
        string? message,
        CancellationToken ct,
        CommRuleEmit? emitOverride = null)
    {
        var emit = emitOverride ?? runtime.Definition.Emit;
        var evt = new CommRuleEventDto
        {
            CommRuleId = runtime.Rule.CommRuleId,
            RuleName = runtime.Rule.Name,
            EventName = runtime.Rule.EventName ?? emit?.EventName ?? runtime.Rule.Name,
            Success = success,
            Values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase),
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        };

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
            db.Set<CommEventLog>().Add(new CommEventLog
            {
                CommRuleId = runtime.Rule.CommRuleId,
                EventName = evt.EventName,
                Success = success,
                PayloadJson = JsonSerializer.Serialize(values, JsonOpts),
                Message = message,
                CreateDate = DateTime.Now
            });
            await db.SaveChangesAsync(ct);

            if (emit?.RaiseAlarm == true)
            {
                var alarm = scope.ServiceProvider.GetService<IAlarmService>();
                if (alarm != null)
                {
                    await alarm.RaiseAsync(new RaiseAlarmRequest
                    {
                        Code = emit.AlarmCode ?? _options.Value.AlarmCodeDisconnect,
                        Source = "DeviceComm.Rule",
                        DeviceName = runtime.Rule.Name,
                        Params = new Dictionary<string, string> { ["DeviceName"] = runtime.Rule.Name },
                        ExtraData = JsonSerializer.Serialize(evt.Values, JsonOpts)
                    }, ct);
                }
            }

            if ((emit?.WriteHotStore == true || _options.Value.WriteHotStoreOnPointChange) && success)
            {
                var hot = scope.ServiceProvider.GetService<IHotStore>();
                if (hot is { IsReady: true })
                {
                    var prefix = emit?.HotStoreKeyPrefix ?? $"comm:rule:{runtime.Rule.CommRuleId}:";
                    foreach (var kv in values)
                        await hot.SetAsync(prefix + kv.Key, kv.Value, cancellationToken: ct);
                }
            }
        }

        if (_push != null)
            await _push.PushRuleEventAsync(evt);

        return evt;
    }

    private static bool IsTruthy(object? v)
    {
        if (v is null) return false;
        if (v is bool b) return b;
        if (v is IConvertible)
        {
            try { return Convert.ToDouble(v, CultureInfo.InvariantCulture) != 0; }
            catch { return true; }
        }
        return true;
    }

    private static bool EqualsValue(object? a, object? b)
    {
        b = NormalizeJson(b);
        a = NormalizeJson(a);
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a is IConvertible && b is IConvertible)
        {
            try
            {
                return Convert.ToDouble(a, CultureInfo.InvariantCulture)
                       .Equals(Convert.ToDouble(b, CultureInfo.InvariantCulture));
            }
            catch { /* fallthrough */ }
        }
        return string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture),
            Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }

    private static int Compare(object? a, object? b)
    {
        a = NormalizeJson(a);
        b = NormalizeJson(b);
        var da = Convert.ToDouble(a ?? 0, CultureInfo.InvariantCulture);
        var db = Convert.ToDouble(b ?? 0, CultureInfo.InvariantCulture);
        return da.CompareTo(db);
    }

    private static object? NormalizeJson(object? v)
    {
        if (v is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when je.TryGetInt64(out var l) => l,
                JsonValueKind.Number => je.GetDouble(),
                JsonValueKind.String => je.GetString(),
                _ => je.ToString()
            };
        }
        return v;
    }

    private sealed class RuntimeRule
    {
        public CommRule Rule { get; }
        public CommRuleDefinition Definition { get; }
        public Dictionary<string, CommPoint> PointsByCode { get; }
        public DateTimeOffset? PendingAwaitUntil { get; set; }
        public Dictionary<string, object?> PendingValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public RuntimeRule(CommRule rule, CommRuleDefinition definition, Dictionary<string, CommPoint> pointsByCode)
        {
            Rule = rule;
            Definition = definition;
            PointsByCode = pointsByCode;
        }
    }
}

/// <summary>关闭时的空规则引擎</summary>
public sealed class DisabledCommRuleEngine : ICommRuleEngine
{
    public Task ReloadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ScanOnceAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<CommRuleEventDto> TriggerAsync(int ruleId, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("DeviceComm 未启用");
}

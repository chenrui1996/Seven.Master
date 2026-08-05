using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.DeviceComm;

/// <summary>自动连接、重连、规则扫描宿主</summary>
public sealed class DeviceCommHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DeviceCommOptions> _options;
    private readonly ILogger<DeviceCommHostedService> _logger;

    public DeviceCommHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<DeviceCommOptions> options,
        ILogger<DeviceCommHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 等待 DI / Hub 推送服务就绪
        await Task.Delay(1500, stoppingToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var gateway = scope.ServiceProvider.GetRequiredService<IDeviceCommGateway>();
            var engine = scope.ServiceProvider.GetRequiredService<ICommRuleEngine>();
            var push = scope.ServiceProvider.GetService<IDeviceCommPushService>();

            if (gateway is DeviceCommGateway g && push != null)
                g.SetPushService(push);
            if (engine is CommRuleEngine e && push != null)
                e.SetPushService(push);

            await gateway.ReloadAsync(stoppingToken);
            await engine.ReloadAsync(stoppingToken);

            // AutoConnect
            if (gateway is DeviceCommGateway real)
            {
                foreach (var live in real.GetLiveConnections().Where(x => x.WantConnected))
                {
                    try { await real.ConnectAsync(live.Config.CommConnectionId, stoppingToken); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "启动自动连接失败 {Name}", live.Config.Name);
                    }
                }
            }

            _logger.LogInformation("DeviceComm 宿主已启动，RuleScanIntervalMs={Interval}",
                _options.Value.RuleScanIntervalMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeviceComm 宿主初始化失败");
        }

        var interval = Math.Max(50, _options.Value.RuleScanIntervalMs);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var gateway = scope.ServiceProvider.GetRequiredService<IDeviceCommGateway>();
                var engine = scope.ServiceProvider.GetRequiredService<ICommRuleEngine>();

                if (gateway is DeviceCommGateway real)
                    await real.ProcessReconnectsAsync(stoppingToken);

                await engine.ScanOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DeviceComm 扫描周期异常");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}

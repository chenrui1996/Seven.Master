using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Seven.Infrastructure.Simulator.Gateway;

/// <summary>loopback TCP echo Gateway；收到行后 echo 并通过 Hub 广播。</summary>
public sealed class SimGatewayHostedService : BackgroundService
{
    private readonly ISimWcsProxyNotifier _notifier;
    private readonly SimGatewayOptions _options;
    private readonly ILogger<SimGatewayHostedService> _logger;
    private int _connectionCounter;

    public SimGatewayHostedService(
        ISimWcsProxyNotifier notifier,
        IOptions<SimGatewayOptions> options,
        ILogger<SimGatewayHostedService> logger)
    {
        _notifier = notifier;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        var port = _options.DefaultListenPort;
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        _logger.LogInformation("SimGateway TCP echo listening on 127.0.0.1:{Port}", port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SimGateway TCP listener failed on port {Port}", port);
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        var connectionId = $"tcp-{Interlocked.Increment(ref _connectionCounter)}";
        try
        {
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

            while (!stoppingToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(stoppingToken);
                if (line is null)
                    break;

                var echo = SimGatewayMessage.BuildEchoReply(line);
                await writer.WriteLineAsync(echo.AsMemory(), stoppingToken);
                await _notifier.NotifyMessageReceivedAsync(connectionId, echo, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SimGateway client {ConnectionId} disconnected", connectionId);
        }
        finally
        {
            client.Dispose();
        }
    }
}

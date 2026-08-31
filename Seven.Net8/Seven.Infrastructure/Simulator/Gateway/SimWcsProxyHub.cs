using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Seven.Infrastructure.Simulator.Gateway;

/// <summary>
/// 仿真 WCS SignalR 代理 Hub。契约名对齐 RCS，便于前端迁移。
/// </summary>
[AllowAnonymous]
public class SimWcsProxyHub : Hub
{
    public const string GroupName = "SimWcsProxy";

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName);
        await base.OnConnectedAsync();
    }

    /// <summary>客户端向指定 WCS 连接发送报文，并广播 <c>OnWcsMessageReceived</c>。</summary>
    public Task SendWcsMessage(string connectionId, string payload) =>
        Clients.Group(GroupName).SendAsync("OnWcsMessageReceived", connectionId, payload);
}

/// <summary>从 TCP Gateway 等后端组件向 Hub 订阅者推送 WCS 报文。</summary>
public interface ISimWcsProxyNotifier
{
    Task NotifyMessageReceivedAsync(string connectionId, string payload, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class SimWcsProxyNotifier : ISimWcsProxyNotifier
{
    private readonly IHubContext<SimWcsProxyHub> _hub;

    public SimWcsProxyNotifier(IHubContext<SimWcsProxyHub> hub) => _hub = hub;

    /// <inheritdoc />
    public Task NotifyMessageReceivedAsync(string connectionId, string payload, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group(SimWcsProxyHub.GroupName)
            .SendAsync("OnWcsMessageReceived", connectionId, payload, cancellationToken);
}

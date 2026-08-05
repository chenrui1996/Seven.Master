using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Seven.Application.Interfaces;

namespace Seven.WebApi.Hubs;

/// <summary>设备通讯 SignalR Hub</summary>
[Authorize]
public class DeviceCommHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "DeviceComm");
        await base.OnConnectedAsync();
    }
}

/// <summary>设备通讯推送</summary>
public class DeviceCommPushService : IDeviceCommPushService
{
    private readonly IHubContext<DeviceCommHub> _hub;

    public DeviceCommPushService(IHubContext<DeviceCommHub> hub) => _hub = hub;

    public Task PushConnectionStatusAsync(CommConnectionStatusDto status) =>
        _hub.Clients.Group("DeviceComm").SendAsync("ConnectionStatus", status);

    public Task PushRuleEventAsync(CommRuleEventDto evt) =>
        _hub.Clients.Group("DeviceComm").SendAsync("RuleEvent", evt);
}

/// <summary>未启用时的空推送</summary>
public class NoOpDeviceCommPushService : IDeviceCommPushService
{
    public Task PushConnectionStatusAsync(CommConnectionStatusDto status) => Task.CompletedTask;
    public Task PushRuleEventAsync(CommRuleEventDto evt) => Task.CompletedTask;
}

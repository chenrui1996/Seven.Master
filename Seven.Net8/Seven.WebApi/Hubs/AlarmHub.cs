using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Seven.Application.Interfaces;

namespace Seven.WebApi.Hubs;

/// <summary>
/// 告警 SignalR Hub，前端订阅 ReceiveAlarm / AlarmUpdated
/// </summary>
[Authorize]
public class AlarmHub : Hub
{
    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
        await base.OnConnectedAsync();
    }
}

/// <summary>告警 SignalR 推送实现</summary>
public class AlarmPushService : IAlarmPushService
{
    private readonly IHubContext<AlarmHub> _hub;

    /// <summary>构造函数</summary>
    public AlarmPushService(IHubContext<AlarmHub> hub) => _hub = hub;

    /// <inheritdoc />
    public Task PushNewAlarmAsync(AlarmPushDto alarm) =>
        _hub.Clients.Group("AllUsers").SendAsync("ReceiveAlarm", alarm);

    /// <inheritdoc />
    public Task PushAlarmUpdatedAsync(AlarmPushDto alarm) =>
        _hub.Clients.Group("AllUsers").SendAsync("AlarmUpdated", alarm);
}

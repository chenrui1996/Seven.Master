using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Seven.WebApi.Hubs;

/// <summary>
/// 首页消息 SignalR Hub，用于实时通知与在线统计
/// </summary>
[Authorize]
public class MessageHub : Hub
{
    private static int _online;

    public static int OnlineCount => Volatile.Read(ref _online);

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _online);
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
        await Clients.Group("AllUsers").SendAsync("OnlineCount", OnlineCount);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _online);
        await Clients.Group("AllUsers").SendAsync("OnlineCount", OnlineCount);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string message) =>
        await Clients.Group("AllUsers").SendAsync("ReceiveMessage", message);

    public Task<int> GetOnlineCount() => Task.FromResult(OnlineCount);
}

public interface IMessagePushService
{
    Task PushAsync(string message);
    Task PushSystemNotifyAsync(string title, string content);
}

public class MessagePushService : IMessagePushService
{
    private readonly IHubContext<MessageHub> _hub;

    public MessagePushService(IHubContext<MessageHub> hub) => _hub = hub;

    public Task PushAsync(string message) =>
        _hub.Clients.Group("AllUsers").SendAsync("ReceiveMessage", message);

    public Task PushSystemNotifyAsync(string title, string content) =>
        _hub.Clients.Group("AllUsers").SendAsync("SystemNotify", new { title, content, at = DateTime.Now });
}

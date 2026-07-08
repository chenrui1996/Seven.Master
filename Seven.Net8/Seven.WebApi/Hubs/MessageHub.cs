using Microsoft.AspNetCore.SignalR;

namespace Seven.WebApi.Hubs;

/// <summary>
/// 首页消息 SignalR Hub，用于实时通知
/// </summary>
public class MessageHub : Hub
{
    /// <summary>客户端连接</summary>
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllUsers");
        await base.OnConnectedAsync();
    }

    /// <summary>发送广播消息</summary>
    public async Task SendMessage(string message) =>
        await Clients.Group("AllUsers").SendAsync("ReceiveMessage", message);
}

/// <summary>
/// SignalR 消息推送服务
/// </summary>
public interface IMessagePushService
{
    /// <summary>推送消息给所有在线用户</summary>
    Task PushAsync(string message);
}

/// <summary>SignalR 消息推送实现</summary>
public class MessagePushService : IMessagePushService
{
    private readonly IHubContext<MessageHub> _hub;

    /// <summary>构造函数</summary>
    public MessagePushService(IHubContext<MessageHub> hub) => _hub = hub;

    /// <inheritdoc />
    public Task PushAsync(string message) =>
        _hub.Clients.Group("AllUsers").SendAsync("ReceiveMessage", message);
}

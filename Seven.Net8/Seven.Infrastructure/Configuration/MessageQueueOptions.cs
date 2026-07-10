namespace Seven.Infrastructure.Configuration;

/// <summary>
/// 消息队列配置（appsettings.json → MessageQueue 节点）
/// </summary>
public class MessageQueueOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "MessageQueue";

    /// <summary>Provider：None / RabbitMQ</summary>
    public string Provider { get; set; } = "None";

    /// <summary>是否注册 MassTransit Consumer（API 进程内消费）</summary>
    public bool EnableConsumers { get; set; } = true;

    /// <summary>RabbitMQ 连接参数</summary>
    public RabbitMqOptions RabbitMq { get; set; } = new();
}

/// <summary>RabbitMQ 连接配置</summary>
public class RabbitMqOptions
{
    /// <summary>主机</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>端口</summary>
    public ushort Port { get; set; } = 5672;

    /// <summary>虚拟主机</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>用户名</summary>
    public string Username { get; set; } = "guest";

    /// <summary>密码</summary>
    public string Password { get; set; } = "guest";
}

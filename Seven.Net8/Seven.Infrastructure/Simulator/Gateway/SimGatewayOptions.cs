namespace Seven.Infrastructure.Simulator.Gateway;

/// <summary>仿真 WCS Gateway（TCP + SignalR 代理）配置。</summary>
public class SimGatewayOptions
{
    public const string SectionName = "Simulator:Gateway";

    /// <summary>是否启动 loopback TCP echo 服务。</summary>
    public bool Enabled { get; set; }

    /// <summary>默认 TCP 监听端口（127.0.0.1）。</summary>
    public int DefaultListenPort { get; set; } = 9100;
}

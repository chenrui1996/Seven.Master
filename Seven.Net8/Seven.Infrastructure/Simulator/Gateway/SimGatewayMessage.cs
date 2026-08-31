namespace Seven.Infrastructure.Simulator.Gateway;

/// <summary>Gateway 行帧辅助（Phase III 最小 echo 契约）。</summary>
public static class SimGatewayMessage
{
    /// <summary>将 WCS 连接标识与报文正文格式化为 Hub 事件帧。</summary>
    public static string FormatReceived(string connectionId, string payload) =>
        $"{connectionId}|{payload}";

    /// <summary>解析 Hub 事件帧；格式为 <c>connectionId|payload</c>。</summary>
    public static bool TryParseReceived(string framed, out string connectionId, out string payload)
    {
        connectionId = string.Empty;
        payload = string.Empty;
        if (string.IsNullOrEmpty(framed))
            return false;

        var idx = framed.IndexOf('|');
        if (idx <= 0 || idx >= framed.Length - 1)
            return false;

        connectionId = framed[..idx];
        payload = framed[(idx + 1)..];
        return !string.IsNullOrEmpty(connectionId);
    }

    /// <summary>Phase III 最小 echo：原样返回收到的行。</summary>
    public static string BuildEchoReply(string receivedLine) => receivedLine;
}

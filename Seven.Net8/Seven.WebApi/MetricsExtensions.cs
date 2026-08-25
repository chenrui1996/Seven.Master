using Prometheus;

namespace Seven.WebApi;

public static class MetricsExtensions
{
    /// <summary>
    /// 须尽可能靠前注册，以便在响应离开管道时记录最终状态码
    /// （异常中间件应位于其后，见 prometheus-net 文档）。
    /// </summary>
    public static WebApplication UseSevenMetrics(this WebApplication app)
    {
        app.UseHttpMetrics(options => options.ReduceStatusCodeCardinality());
        return app;
    }

    public static WebApplication MapSevenMetrics(this WebApplication app)
    {
        app.MapMetrics("/metrics");
        return app;
    }
}

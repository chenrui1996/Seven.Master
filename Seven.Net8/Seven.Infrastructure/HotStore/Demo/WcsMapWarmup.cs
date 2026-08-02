using Microsoft.Extensions.Logging;
using Seven.Application.Interfaces;

namespace Seven.Infrastructure.HotStore.Demo;

/// <summary>四向车 Demo：预热简易路网到热层</summary>
public sealed class WcsMapWarmup : IHotStoreWarmup
{
    private readonly ILogger<WcsMapWarmup> _logger;

    public WcsMapWarmup(ILogger<WcsMapWarmup> logger) => _logger = logger;

    /// <inheritdoc />
    public string Name => "WcsDemoMap";

    /// <inheritdoc />
    public async Task WarmupAsync(IHotStore store, CancellationToken cancellationToken = default)
    {
        var map = new WcsDemoMap
        {
            Version = 1,
            Nodes = ["N1", "N2", "N3", "N4", "N5"],
            Edges =
            [
                new("N1", "N2"),
                new("N2", "N3"),
                new("N3", "N4"),
                new("N4", "N5"),
                new("N2", "N4")
            ]
        };

        await store.SetAsync(WcsHotKeys.Map, map, cancellationToken: cancellationToken).ConfigureAwait(false);
        foreach (var node in map.Nodes)
            await store.SetAsync(WcsHotKeys.Node(node), new WcsNodeState { NodeId = node, Free = true }, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        _logger.LogInformation("WCS demo map warmed: {NodeCount} nodes, version {Version}", map.Nodes.Count, map.Version);
    }
}

internal static class WcsHotKeys
{
    public const string Map = "wcs:map";
    public static string Node(string id) => $"wcs:node:{id}";
    public static string Lock(string id) => $"wcs:lock:{id}";
}

internal sealed class WcsDemoMap
{
    public int Version { get; set; }
    public List<string> Nodes { get; set; } = [];
    public List<WcsEdge> Edges { get; set; } = [];
}

internal sealed record WcsEdge(string From, string To);

internal sealed class WcsNodeState
{
    public string NodeId { get; set; } = "";
    public bool Free { get; set; } = true;
    public string? OwnerId { get; set; }
}

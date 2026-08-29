namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>有向边（小图加权最短路输入）。</summary>
public readonly record struct FourWayEdge(string From, string To, double Weight, string? EdgeId = null);

/// <summary>四向车加权最短路（Dijkstra，纯函数）。</summary>
public static class FourWayRouter
{
    public static IReadOnlyList<string> FindPath(
        string from,
        string to,
        IReadOnlyList<FourWayEdge> edges)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentNullException.ThrowIfNull(edges);

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return [from];

        var adj = new Dictionary<string, List<FourWayEdge>>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in edges)
        {
            if (!adj.TryGetValue(edge.From, out var list))
            {
                list = [];
                adj[edge.From] = list;
            }

            list.Add(edge);
        }

        var dist = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { [from] = 0 };
        var prev = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var queue = new PriorityQueue<string, double>();
        queue.Enqueue(from, 0);

        while (queue.TryDequeue(out var node, out var cost))
        {
            if (cost > dist.GetValueOrDefault(node, double.PositiveInfinity))
                continue;
            if (string.Equals(node, to, StringComparison.OrdinalIgnoreCase))
                break;
            if (!adj.TryGetValue(node, out var outgoing))
                continue;

            foreach (var edge in outgoing)
            {
                var nextCost = cost + Math.Max(edge.Weight, 0);
                if (nextCost >= dist.GetValueOrDefault(edge.To, double.PositiveInfinity))
                    continue;
                dist[edge.To] = nextCost;
                prev[edge.To] = node;
                queue.Enqueue(edge.To, nextCost);
            }
        }

        if (!prev.ContainsKey(to) && !string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return [];

        var path = new List<string>();
        for (var cur = to; ; cur = prev[cur])
        {
            path.Add(cur);
            if (string.Equals(cur, from, StringComparison.OrdinalIgnoreCase))
                break;
            if (!prev.ContainsKey(cur))
                return [];
        }

        path.Reverse();
        return path;
    }
}

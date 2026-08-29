namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>有向边输入（加权最短路）。</summary>
public readonly record struct StackerRouteEdge(
    int RouteId,
    string From,
    string To,
    double Weight,
    string ExeStackCode,
    int Capacity,
    int Occupancy);

/// <summary>堆垛包内加权最短路（Dijkstra）；容量满的边不可用。</summary>
public static class StackerRouter
{
    public static IReadOnlyList<int> FindPathRouteIds(
        string from,
        string to,
        IReadOnlyList<StackerRouteEdge> edges)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from);
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentNullException.ThrowIfNull(edges);

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return [];

        var usable = edges.Where(e => e.Occupancy < Math.Max(e.Capacity, 1)).ToList();
        var adj = new Dictionary<string, List<StackerRouteEdge>>(StringComparer.OrdinalIgnoreCase);
        foreach (var edge in usable)
        {
            if (!adj.TryGetValue(edge.From, out var list))
            {
                list = [];
                adj[edge.From] = list;
            }

            list.Add(edge);
        }

        var dist = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { [from] = 0 };
        var prevEdge = new Dictionary<string, StackerRouteEdge>(StringComparer.OrdinalIgnoreCase);
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
                var nextCost = cost + Math.Max(edge.Weight, 0) + edge.Occupancy * 0.01;
                if (nextCost >= dist.GetValueOrDefault(edge.To, double.PositiveInfinity))
                    continue;
                dist[edge.To] = nextCost;
                prevEdge[edge.To] = edge;
                queue.Enqueue(edge.To, nextCost);
            }
        }

        if (!prevEdge.ContainsKey(to))
            return [];

        var routeIds = new List<int>();
        for (var cur = to; !string.Equals(cur, from, StringComparison.OrdinalIgnoreCase);)
        {
            if (!prevEdge.TryGetValue(cur, out var edge))
                return [];
            routeIds.Add(edge.RouteId);
            cur = edge.From;
        }

        routeIds.Reverse();
        return routeIds;
    }
}

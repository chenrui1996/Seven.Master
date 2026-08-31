using Seven.Application.Simulator;

namespace Seven.Infrastructure.Simulator;

public sealed class SimulationImportService : ISimulationImportService
{
    public ImportGridResult ImportGrid(ImportGridRequest request)
    {
        var warnings = new List<string>();
        var packId = string.IsNullOrWhiteSpace(request.PackId) ? "stacker" : request.PackId.Trim();

        if (request.Cells is null || request.Cells.Count == 0)
            return new ImportGridResult(
                new SimMapDto(packId, Array.Empty<SimMapNodeDto>()),
                new[] { "No cells provided" });

        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nodes = new List<SimMapNodeDto>();
        var index = 0;

        foreach (var cell in request.Cells)
        {
            index++;
            var code = cell.Code?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(code))
            {
                warnings.Add($"Cell #{index}: empty code skipped");
                continue;
            }

            if (!seenCodes.Add(code))
            {
                warnings.Add($"Duplicate code \"{code}\" skipped");
                continue;
            }

            nodes.Add(new SimMapNodeDto(Guid.NewGuid().ToString("N"), code, cell.X, cell.Y));
        }

        if (nodes.Count == 0)
            warnings.Add("No valid cells after validation");

        var map = new SimMapDto(
            packId,
            nodes,
            Array.Empty<SimMapEdgeDto>(),
            Array.Empty<SimMapDeviceDto>(),
            Array.Empty<SimMapRequestPointDto>());

        return new ImportGridResult(map, warnings);
    }

    public RouteGroupsPreviewResult PreviewRouteGroups(RouteGroupsPreviewRequest request)
    {
        var warnings = new List<string>();
        var nodes = request.Nodes ?? Array.Empty<SimMapNodeDto>();
        var edges = request.Edges ?? Array.Empty<SimMapEdgeDto>();

        if (nodes.Count == 0)
            return new RouteGroupsPreviewResult(Array.Empty<RouteGroupSuggestionDto>(), new[] { "No nodes in map" });

        var nodeIds = nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        var adjacency = nodes.ToDictionary(n => n.Id, _ => new List<string>());

        foreach (var edge in edges)
        {
            if (!nodeIds.Contains(edge.From) || !nodeIds.Contains(edge.To))
            {
                warnings.Add($"Edge {edge.Id}: missing endpoint node");
                continue;
            }

            adjacency[edge.From].Add(edge.To);
            adjacency[edge.To].Add(edge.From);
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var components = new List<List<string>>();

        foreach (var node in nodes)
        {
            if (visited.Contains(node.Id)) continue;

            var component = new List<string>();
            var queue = new Queue<string>();
            queue.Enqueue(node.Id);
            visited.Add(node.Id);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                component.Add(current);
                foreach (var neighbor in adjacency[current])
                {
                    if (visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }

            components.Add(component);
        }

        var isolated = components.Where(c => c.Count == 1).ToList();
        if (isolated.Count > 0)
            warnings.Add($"{isolated.Count} isolated node(s) without edges");

        var suggested = new List<RouteGroupSuggestionDto>();
        var groupIndex = 0;
        foreach (var component in components.OrderByDescending(c => c.Count))
        {
            groupIndex++;
            var code = request.PackId.Equals("fourway", StringComparison.OrdinalIgnoreCase)
                ? $"Fw.RG-{groupIndex:D2}"
                : $"RG-{groupIndex:D2}";

            if (component.Count >= 3 && HasCycle(component, adjacency))
                warnings.Add($"Group {code}: possible cycle ({component.Count} nodes); review for four-way loops");

            suggested.Add(new RouteGroupSuggestionDto(code, component));
        }

        return new RouteGroupsPreviewResult(suggested, warnings);
    }

    private static bool HasCycle(IReadOnlyList<string> component, IReadOnlyDictionary<string, List<string>> adjacency)
    {
        var set = component.ToHashSet(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new HashSet<string>(StringComparer.Ordinal);

        bool Dfs(string node)
        {
            visited.Add(node);
            stack.Add(node);
            foreach (var neighbor in adjacency[node])
            {
                if (!set.Contains(neighbor)) continue;
                if (!visited.Contains(neighbor))
                {
                    if (Dfs(neighbor)) return true;
                }
                else if (stack.Contains(neighbor))
                {
                    return true;
                }
            }

            stack.Remove(node);
            return false;
        }

        foreach (var node in component)
        {
            if (!visited.Contains(node) && Dfs(node))
                return true;
        }

        return false;
    }
}

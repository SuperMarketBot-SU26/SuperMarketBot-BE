using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Domain.Entities;

namespace SmartMarketBot.Application.Services;

public sealed class NavigationService(IAppDbContext dbContext) : INavigationService
{
    private sealed record NodeData(int NodeId, double XCoord, double YCoord, bool IsBlocked);
    private sealed record EdgeData(int FromNodeId, int ToNodeId, double Distance, bool IsBidirectional);

    public async Task<RoutePlanResultDto> PlanRouteAsync(RoutePlanRequestDto request, CancellationToken cancellationToken = default)
    {
        var nodes = await dbContext.NavigationNodes
            .AsNoTracking()
            .Select(n => new NodeData(n.NodeID, n.XCoord, n.YCoord, n.IsBlocked))
            .ToDictionaryAsync(n => n.NodeId, cancellationToken);

        if (!nodes.ContainsKey(request.StartNodeId) || !nodes.ContainsKey(request.EndNodeId))
        {
            throw new InvalidOperationException("Start or end node does not exist.");
        }

        if (nodes[request.StartNodeId].IsBlocked || nodes[request.EndNodeId].IsBlocked)
        {
            throw new InvalidOperationException("Start or end node is blocked.");
        }

        var edges = await dbContext.NavigationEdges
            .AsNoTracking()
            .Select(e => new EdgeData(e.FromNodeID, e.ToNodeID, e.Distance, e.IsBidirectional))
            .ToListAsync(cancellationToken);

        var adjacency = BuildAdjacency(edges, nodes);
        var (distances, previous) = Dijkstra(adjacency, request.StartNodeId);

        if (!distances.TryGetValue(request.EndNodeId, out var totalDistance) || double.IsPositiveInfinity(totalDistance))
        {
            return new RoutePlanResultDto(0d, []);
        }

        var path = ReconstructPath(previous, request.StartNodeId, request.EndNodeId);
        var resultNodes = path
            .Select(nodeId => new RouteNodeDto(
                nodeId,
                nodes[nodeId].XCoord,
                nodes[nodeId].YCoord,
                distances[nodeId]))
            .ToList();

        return new RoutePlanResultDto(totalDistance, resultNodes);
    }

    private static Dictionary<int, List<(int ToNodeId, double Weight)>> BuildAdjacency(
        IReadOnlyList<EdgeData> edges,
        IReadOnlyDictionary<int, NodeData> nodes)
    {
        var graph = nodes.Keys.ToDictionary(nodeId => nodeId, _ => new List<(int ToNodeId, double Weight)>());

        foreach (var edge in edges)
        {
            if (!nodes.ContainsKey(edge.FromNodeId) || !nodes.ContainsKey(edge.ToNodeId))
            {
                continue;
            }

            if (nodes[edge.FromNodeId].IsBlocked || nodes[edge.ToNodeId].IsBlocked)
            {
                continue;
            }

            graph[edge.FromNodeId].Add((edge.ToNodeId, edge.Distance));

            if (edge.IsBidirectional)
            {
                graph[edge.ToNodeId].Add((edge.FromNodeId, edge.Distance));
            }
        }

        return graph;
    }

    private static (Dictionary<int, double> Distances, Dictionary<int, int?> Previous) Dijkstra(
        Dictionary<int, List<(int ToNodeId, double Weight)>> graph,
        int startNodeId)
    {
        var distances = graph.Keys.ToDictionary(k => k, _ => double.PositiveInfinity);
        var previous = graph.Keys.ToDictionary(k => k, _ => (int?)null);
        var queue = new PriorityQueue<int, double>();

        distances[startNodeId] = 0d;
        queue.Enqueue(startNodeId, 0d);

        while (queue.TryDequeue(out var current, out var currentDistance))
        {
            if (currentDistance > distances[current])
            {
                continue;
            }

            foreach (var (toNodeId, weight) in graph[current])
            {
                var nextDistance = currentDistance + weight;
                if (nextDistance >= distances[toNodeId])
                {
                    continue;
                }

                distances[toNodeId] = nextDistance;
                previous[toNodeId] = current;
                queue.Enqueue(toNodeId, nextDistance);
            }
        }

        return (distances, previous);
    }

    private static IReadOnlyList<int> ReconstructPath(
        IReadOnlyDictionary<int, int?> previous,
        int startNodeId,
        int endNodeId)
    {
        var stack = new Stack<int>();
        var current = endNodeId;

        while (true)
        {
            stack.Push(current);
            if (current == startNodeId)
            {
                break;
            }

            if (!previous.TryGetValue(current, out var parent) || parent is null)
            {
                return [];
            }

            current = parent.Value;
        }

        return stack.ToList();
    }
}

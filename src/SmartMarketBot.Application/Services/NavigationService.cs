using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;

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

    // ── Flow 1: Multi-stop Shopping Route ────────────────────────────────────

    private sealed record ProductNodeInfo(int ProductId, int NodeId, double XCoord, double YCoord, string? SlotCode);

    public async Task<OptimizeShoppingRouteResponseDto> OptimizeShoppingRouteAsync(
        OptimizeShoppingRouteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Load nodes, edges, forbidden zones
        var nodes = await dbContext.NavigationNodes
            .AsNoTracking()
            .Select(n => new NodeData(n.NodeID, n.XCoord, n.YCoord, n.IsBlocked))
            .ToDictionaryAsync(n => n.NodeId, cancellationToken);

        var edges = await dbContext.NavigationEdges
            .AsNoTracking()
            .Select(e => new EdgeData(e.FromNodeID, e.ToNodeID, e.Distance, e.IsBidirectional))
            .ToListAsync(cancellationToken);

        var forbiddenZoneIds = await dbContext.ForbiddenZones
            .AsNoTracking()
            .Where(fz => fz.IsActive)
            .Select(fz => fz.ForbiddenZoneID)
            .ToListAsync(cancellationToken);

        var adjacency = BuildAdjacency(edges, nodes);

        // 2. Tìm NodeID gần nhất cho từng ProductID (qua Slots → ShelfLevel → Aisle → NavigationNode)
        var rawMap = await dbContext.Slots
            .AsNoTracking()
            .Where(s => request.ProductIds.Contains(s.ProductID ?? 0) && s.ProductID != null)
            .Join(dbContext.ShelfLevels, s => s.ShelfLevelID, sl => sl.ShelfLevelID,
                  (s, sl) => new { s.ProductID, sl.AisleID, s.SlotCode })
            .Join(dbContext.NavigationNodes, x => x.AisleID, n => n.LinkedAisleID,
                  (x, n) => new ProductNodeInfo(x.ProductID!.Value, n.NodeID, n.XCoord, n.YCoord, x.SlotCode))
            .ToListAsync(cancellationToken);

        var productNodeMap = rawMap
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.First());

        // 3. Nearest Neighbour TSP để tối ưu thứ tự ghé qua
        var remaining = productNodeMap.Keys.ToList();
        var orderedProductIds = new List<int>();
        var currentNodeId = request.StartNodeId;

        while (remaining.Count > 0)
        {
            var (distancesFromCurrent, _) = Dijkstra(adjacency, currentNodeId);
            int best = -1;
            double bestDist = double.PositiveInfinity;
            foreach (var pid in remaining)
            {
                if (!productNodeMap.TryGetValue(pid, out var pni)) continue;
                var d = distancesFromCurrent.ContainsKey(pni.NodeId) ? distancesFromCurrent[pni.NodeId] : double.PositiveInfinity;
                if (d < bestDist) { bestDist = d; best = pid; }
            }
            if (best < 0) break;
            orderedProductIds.Add(best);
            currentNodeId = productNodeMap[best].NodeId;
            remaining.Remove(best);
        }

        // 4. Xây waypoints theo thứ tự tối ưu
        var waypoints = new List<ShoppingWaypointDto>();
        double totalDistance = 0;
        int prevNode = request.StartNodeId;

        for (int i = 0; i < orderedProductIds.Count; i++)
        {
            var pid = orderedProductIds[i];
            if (!productNodeMap.TryGetValue(pid, out var pn)) continue;

            var (dist, _) = Dijkstra(adjacency, prevNode);
            totalDistance += dist.ContainsKey(pn.NodeId) ? dist[pn.NodeId] : 0;
            prevNode = pn.NodeId;

            waypoints.Add(new ShoppingWaypointDto(
                i + 1,
                pn.NodeId,
                nodes.ContainsKey(pn.NodeId) ? nodes[pn.NodeId].XCoord.ToString("F1") + "," + nodes[pn.NodeId].YCoord.ToString("F1") : $"Node {pn.NodeId}",
                pn.XCoord,
                pn.YCoord,
                pid,
                $"Product #{pid}",
                null,
                pn.SlotCode,
                pn.SlotCode is null ? null : $"Slot {pn.SlotCode}"));
        }

        return new OptimizeShoppingRouteResponseDto(
            Math.Round(totalDistance, 2),
            waypoints.Count,
            waypoints,
            forbiddenZoneIds,
            $"TSP Nearest-Neighbour + Dijkstra. {forbiddenZoneIds.Count} ForbiddenZone(s) excluded.");
    }

    public async Task SetNodeBlockedAsync(int nodeId, bool isBlocked, string? reason, CancellationToken cancellationToken = default)
    {
        var node = await dbContext.NavigationNodes
            .FirstOrDefaultAsync(n => n.NodeID == nodeId, cancellationToken)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        node.IsBlocked = isBlocked;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

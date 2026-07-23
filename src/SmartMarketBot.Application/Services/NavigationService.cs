using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Domain.Entities;

namespace SmartMarketBot.Application.Services;

public sealed class NavigationService(IAppDbContext dbContext, ILocalizationService localizer) : INavigationService
{
    private sealed record NodeData(int NodeId, double XCoord, double YCoord, bool IsBlocked);
    private sealed record EdgeData(int FromNodeId, int ToNodeId, double Distance, bool IsBidirectional);
    private sealed record ObstacleRect(double XMin, double YMin, double XMax, double YMax);

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Tải danh sách NavigationNodes và đánh dấu bất kỳ node nào
    /// có tọa độ nằm trong vùng cấm (SEMANTIC_OBJECT loại 'obstacle') là IsBlocked = true.
    /// Schema mới (V4.0): thay thế bảng ForbiddenZones bằng SEMANTIC_OBJECT (object_type = 'obstacle').
    /// </summary>
    private async Task<Dictionary<int, NodeData>> LoadNodesWithObstaclesAsync(
        CancellationToken ct)
    {
        var nodes = await dbContext.NavigationNodes
            .AsNoTracking()
            .Select(n => new NodeData(n.NodeId, n.XCoord, n.YCoord, n.IsBlocked))
            .ToDictionaryAsync(n => n.NodeId, ct);

        // Vùng cấm động giờ là SEMANTIC_OBJECT loại 'obstacle' (cùng cấu trúc AABB)
        var obstacles = await dbContext.SemanticObjects
            .AsNoTracking()
            .Where(so => so.ObjectType == "obstacle")
            .Select(so => new ObstacleRect(so.XMin, so.YMin, so.XMax, so.YMax))
            .ToListAsync(ct);

        if (obstacles.Count == 0)
            return nodes;

        // Đánh dấu các node nằm trong vùng cấm là blocked (in-memory)
        foreach (var nodeId in nodes.Keys.ToList())
        {
            var n = nodes[nodeId];
            if (!n.IsBlocked && IsInsideAnyObstacle(n.XCoord, n.YCoord, obstacles))
                nodes[nodeId] = n with { IsBlocked = true };
        }

        return nodes;
    }

    private static bool IsInsideAnyObstacle(double x, double y, IReadOnlyList<ObstacleRect> obstacles)
    {
        foreach (var o in obstacles)
            if (x >= o.XMin && x <= o.XMax && y >= o.YMin && y <= o.YMax)
                return true;
        return false;
    }

    // ── PlanRouteAsync ───────────────────────────────────────────────────────

    public async Task<RoutePlanResultDto> PlanRouteAsync(
        RoutePlanRequestDto request, CancellationToken cancellationToken = default)
    {
        // Nodes already include obstacle (ForbiddenZone) coordinate filtering
        var nodes = await LoadNodesWithObstaclesAsync(cancellationToken);

        if (!nodes.ContainsKey(request.StartNodeId) || !nodes.ContainsKey(request.EndNodeId))
            throw new InvalidOperationException(localizer.Get("StartEndNodeNotExist"));

        if (nodes[request.StartNodeId].IsBlocked || nodes[request.EndNodeId].IsBlocked)
            throw new InvalidOperationException(localizer.Get("StartEndNodeBlocked"));

        var edges = await dbContext.NavigationEdges
            .AsNoTracking()
            .Select(e => new EdgeData(e.FromNodeId, e.ToNodeId, e.Distance, e.IsBidirectional))
            .ToListAsync(cancellationToken);

        var adjacency = BuildAdjacency(edges, nodes);
        var (distances, previous) = Dijkstra(adjacency, request.StartNodeId);

        if (!distances.TryGetValue(request.EndNodeId, out var totalDistance) || double.IsPositiveInfinity(totalDistance))
            return new RoutePlanResultDto(0d, []);

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

    // ── OptimizeShoppingRouteAsync ───────────────────────────────────────────

    private sealed record ProductNodeInfo(int ProductId, int NodeId, double XCoord, double YCoord, string? SlotCode);

    public async Task<OptimizeShoppingRouteResponseDto> OptimizeShoppingRouteAsync(
        OptimizeShoppingRouteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Load nodes (với obstacle coordinate check) + edges
        var nodes = await LoadNodesWithObstaclesAsync(cancellationToken);

        var edges = await dbContext.NavigationEdges
            .AsNoTracking()
            .Select(e => new EdgeData(e.FromNodeId, e.ToNodeId, e.Distance, e.IsBidirectional))
            .ToListAsync(cancellationToken);

        // Lấy IDs vùng cấm để trả về response (thông tin cho client)
        var obstacleIds = await dbContext.SemanticObjects
            .AsNoTracking()
            .Where(so => so.ObjectType == "obstacle")
            .Select(so => so.ObjectId)
            .ToListAsync(cancellationToken);

        // Adjacency graph: tự động loại các node bị blocked (kể cả từ obstacle)
        var adjacency = BuildAdjacency(edges, nodes);

        // 2. Tìm NodeId gần nhất cho từng ProductId
        //    Schema mới (V4.0): Product → ProductSlot → Slot → Shelf → Aisle → AisleNode → NavigationNode
        var requestedPids = request.ProductIds ?? Array.Empty<int>();
        var rawMap = await (
            from ps in dbContext.ProductSlots.AsNoTracking()
            where requestedPids.Contains(ps.ProductId)
            join s  in dbContext.Slots.AsNoTracking()       on ps.SlotId   equals s.SlotId
            join sh in dbContext.Shelves.AsNoTracking()     on s.ShelfId   equals sh.ShelfId
            join a  in dbContext.Aisles.AsNoTracking()      on sh.AisleId  equals a.AisleId
            join an in dbContext.AisleNodes.AsNoTracking()  on a.AisleId   equals an.AisleId
            join n  in dbContext.NavigationNodes.AsNoTracking() on an.NodeId equals n.NodeId
            select new ProductNodeInfo(ps.ProductId, n.NodeId, n.XCoord, n.YCoord, s.SlotCode)
        ).ToListAsync(cancellationToken);


        var productNodeMap = rawMap
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.First());

        // 3. Nearest-Neighbour TSP để tối ưu thứ tự ghé qua
        var remaining = productNodeMap.Keys.ToList();
        var orderedProductIds = new List<int>();
        int currentNodeId = 0;

        // Nếu client truyền tọa độ thực tế (startX, startY), tìm NavigationNode gần nhất theo Euclid
        if (request.StartX.HasValue && request.StartY.HasValue && nodes.Count > 0)
        {
            var nearestNode = nodes.Values
                .Where(n => !n.IsBlocked)
                .OrderBy(n => DistanceSquared(n.XCoord, n.YCoord, request.StartX.Value, request.StartY.Value))
                .FirstOrDefault();

            nearestNode ??= nodes.Values
                .OrderBy(n => DistanceSquared(n.XCoord, n.YCoord, request.StartX.Value, request.StartY.Value))
                .FirstOrDefault();

            if (nearestNode != null)
            {
                currentNodeId = nearestNode.NodeId;
            }
        }

        // Nếu chưa tìm thấy từ tọa độ, thử dùng StartNodeId nếu được truyền vào
        if (currentNodeId == 0 && request.StartNodeId.HasValue && request.StartNodeId.Value > 0)
        {
            if (nodes.ContainsKey(request.StartNodeId.Value))
            {
                currentNodeId = request.StartNodeId.Value;
            }
        }

        // Fallback an toàn: nếu vẫn chưa có NodeId hợp lệ, tự động chọn Node đầu tiên không bị cản
        if (currentNodeId == 0 || !nodes.ContainsKey(currentNodeId))
        {
            var fallbackNode = nodes.Values.FirstOrDefault(n => !n.IsBlocked)?.NodeId ?? nodes.Keys.FirstOrDefault();
            if (fallbackNode > 0)
            {
                currentNodeId = fallbackNode;
            }
            else
            {
                return new OptimizeShoppingRouteResponseDto(
                    0d, 0, [], obstacleIds,
                    "No valid navigation nodes found on map.");
            }
        }



        int originNodeId = currentNodeId;

        while (remaining.Count > 0)
        {
            var (distancesFromCurrent, _) = Dijkstra(adjacency, currentNodeId);
            int best = -1;
            double bestDist = double.PositiveInfinity;
            foreach (var pid in remaining)
            {
                if (!productNodeMap.TryGetValue(pid, out var pni)) continue;
                var d = distancesFromCurrent.TryGetValue(pni.NodeId, out var dv) ? dv : double.PositiveInfinity;
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
        int prevNode = originNodeId;

        // Nếu client truyền tọa độ thực tế startX & startY, thêm waypoint 0 làm vị trí xuất phát của Robot
        if (request.StartX.HasValue && request.StartY.HasValue)
        {
            double sX = request.StartX.Value;
            double sY = request.StartY.Value;

            waypoints.Add(new ShoppingWaypointDto(
                0,
                originNodeId,
                $"{sX:F1},{sY:F1}",
                sX,
                sY,
                null,
                "Vị trí Robot (Xuất phát)",
                null,
                null,
                "Lối xuất phát"));

            // Cộng thêm khoảng cách từ vị trí Robot (startX, startY) tới originNodeId (Node gần nhất)
            if (nodes.TryGetValue(originNodeId, out var originNode))
            {
                totalDistance += Math.Sqrt(DistanceSquared(sX, sY, originNode.XCoord, originNode.YCoord));
            }
        }

        for (int i = 0; i < orderedProductIds.Count; i++)
        {
            var pid = orderedProductIds[i];
            if (!productNodeMap.TryGetValue(pid, out var pn)) continue;

            var (dist, _) = Dijkstra(adjacency, prevNode);
            var segDist = dist.TryGetValue(pn.NodeId, out var sd) && !double.IsInfinity(sd) && !double.IsNaN(sd) ? sd : 0;
            totalDistance += segDist;
            prevNode = pn.NodeId;

            var coordLabel = nodes.TryGetValue(pn.NodeId, out var nd)
                ? $"{nd.XCoord:F1},{nd.YCoord:F1}"
                : $"Node {pn.NodeId}";

            waypoints.Add(new ShoppingWaypointDto(
                i + 1,
                pn.NodeId,
                coordLabel,
                pn.XCoord,
                pn.YCoord,
                pid,
                $"Product #{pid}",
                null,
                pn.SlotCode,
                pn.SlotCode is null ? null : $"Slot {pn.SlotCode}"));
        }


        var safeTotalDistance = double.IsInfinity(totalDistance) || double.IsNaN(totalDistance) ? 0d : Math.Round(totalDistance, 2);

        return new OptimizeShoppingRouteResponseDto(
            safeTotalDistance,
            waypoints.Count,
            waypoints,
            obstacleIds,
            $"TSP Nearest-Neighbour + Dijkstra. {obstacleIds.Count} obstacle(s) excluded from graph.");

    }

    /// <summary>Phase B Step 2 — Tính route theo NodeCode (line-scan). Algorithm giống PlanRouteAsync, chỉ khác input.</summary>
    public async Task<RouteLinePlanResultDto> PlanLineRouteAsync(
        RouteLinePlanRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Resolve NodeCode → NodeId (DB). Tên node duy nhất trong 1 map.
        var nodeIds = await dbContext.NavigationNodes
            .AsNoTracking()
            .Where(n => n.NodeCode == request.StartNodeCode || n.NodeCode == request.EndNodeCode)
            .Select(n => new { n.NodeId, n.NodeCode, n.MapId })
            .ToListAsync(cancellationToken);

        var startEntry = nodeIds.FirstOrDefault(n => n.NodeCode == request.StartNodeCode);
        var endEntry = nodeIds.FirstOrDefault(n => n.NodeCode == request.EndNodeCode);

        if (startEntry is null)
            throw new KeyNotFoundException(localizer.Get("StartNodeNotFoundByCode", request.StartNodeCode));
        if (endEntry is null)
            throw new KeyNotFoundException(localizer.Get("EndNodeNotFoundByCode", request.EndNodeCode));

        // 2. Reuse Dijkstra pipeline (NodeId-driven). Cross-map route giữa 2 map khác nhau bị từ chối.
        if (startEntry.MapId != endEntry.MapId)
            throw new InvalidOperationException(localizer.Get("StartEndNodeMapMismatch"));

        var route = await PlanRouteAsync(
            new RoutePlanRequestDto(startEntry.NodeId, endEntry.NodeId),
            cancellationToken);

        if (route.Nodes.Count == 0)
            return new RouteLinePlanResultDto(0d, [], startEntry.NodeId, endEntry.NodeId);

        var pathIds = route.Nodes.Select(n => n.NodeId).ToHashSet();
        var codeMap = await dbContext.NavigationNodes
            .AsNoTracking()
            .Where(n => pathIds.Contains(n.NodeId))
            .Select(n => new { n.NodeId, n.NodeCode })
            .ToDictionaryAsync(n => n.NodeId, n => n.NodeCode, cancellationToken);

        var resultNodes = route.Nodes
            .Select(n => new RouteLineNodeDto(
                n.NodeId,
                codeMap.TryGetValue(n.NodeId, out var code) ? code : string.Empty,
                n.DistanceFromStart))
            .ToList();

        return new RouteLinePlanResultDto(
            route.TotalDistance,
            resultNodes,
            startEntry.NodeId,
            endEntry.NodeId);
    }

    // ── FindMobileRouteAsync ─────────────────────────────────────────────────

    private static double DistanceSquared(double x1, double y1, double x2, double y2)
        => (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);

    public async Task<MobileRouteResponseDto> FindMobileRouteAsync(
        MobileRouteRequestDto request, CancellationToken cancellationToken = default)
    {
        var nodes = await LoadNodesWithObstaclesAsync(cancellationToken);

        if (nodes.Count == 0)
            return new MobileRouteResponseDto(0d, 0, []);

        // Tìm NavigationNode gần nhất với (startX, startY)
        var startNode = nodes.Values
            .OrderBy(n => DistanceSquared(n.XCoord, n.YCoord, request.StartX, request.StartY))
            .First();

        if (startNode.IsBlocked)
            throw new InvalidOperationException(localizer.Get("StartEndNodeBlocked"));

        int endNodeId;

        if (request.EndNodeId.HasValue)
        {
            if (!nodes.TryGetValue(request.EndNodeId.Value, out var endNodeData))
                throw new KeyNotFoundException(localizer.Get("EndNodeNotFound", request.EndNodeId.Value));
            endNodeId = request.EndNodeId.Value;
        }
        else if (request.EndObjectId.HasValue)
        {
            var destObject = await dbContext.SemanticObjects
                .AsNoTracking()
                .FirstOrDefaultAsync(so => so.ObjectId == request.EndObjectId.Value, cancellationToken)
                ?? throw new KeyNotFoundException(localizer.Get("SemanticObjectNotFound", request.EndObjectId.Value));

            // Tìm node gần nhất nằm bên trong/vùng phủ của semantic object
            var targetNodes = nodes.Values
                .Where(n => !n.IsBlocked)
                .ToList();

            var bestNode = targetNodes
                .Where(n => n.XCoord >= destObject.XMin && n.XCoord <= destObject.XMax
                         && n.YCoord >= destObject.YMin && n.YCoord <= destObject.YMax)
                .OrderBy(n => DistanceSquared(n.XCoord, n.YCoord, (destObject.XMin + destObject.XMax) / 2, (destObject.YMin + destObject.YMax) / 2))
                .FirstOrDefault();

            // Nếu không có node nào bên trong object, lấy node gần tâm object nhất
            bestNode ??= targetNodes
                .OrderBy(n => DistanceSquared(n.XCoord, n.YCoord, (destObject.XMin + destObject.XMax) / 2, (destObject.YMin + destObject.YMax) / 2))
                .First();

            endNodeId = bestNode.NodeId;
        }
        else
        {
            throw new InvalidOperationException("EndObjectId or EndNodeId is required.");
        }

        if (nodes[endNodeId].IsBlocked)
            throw new InvalidOperationException(localizer.Get("StartEndNodeBlocked"));

        var edges = await dbContext.NavigationEdges
            .AsNoTracking()
            .Select(e => new EdgeData(e.FromNodeId, e.ToNodeId, e.Distance, e.IsBidirectional))
            .ToListAsync(cancellationToken);

        var adjacency = BuildAdjacency(edges, nodes);
        var (distances, previous) = Dijkstra(adjacency, startNode.NodeId);

        if (!distances.TryGetValue(endNodeId, out var totalDistance) || double.IsPositiveInfinity(totalDistance))
            return new MobileRouteResponseDto(0d, 0, []);

        var pathIds = ReconstructPath(previous, startNode.NodeId, endNodeId);

        // Lấy semantic object info cho điểm đích
        string? destLabel = null;
        if (request.EndObjectId.HasValue)
        {
            destLabel = await dbContext.SemanticObjects
                .AsNoTracking()
                .Where(so => so.ObjectId == request.EndObjectId.Value)
                .Select(so => so.Label)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Tính khoảng cách từ (startX,startY) đến startNode
        var distToFirstNode = Math.Sqrt(DistanceSquared(request.StartX, request.StartY, startNode.XCoord, startNode.YCoord));

        var path = new List<MobileRoutePointDto>
        {
            new(request.StartX, request.StartY, startNode.NodeId, "Điểm xuất phát")
        };

        double accumulated = distToFirstNode;
        foreach (var nodeId in pathIds.Skip(1))
        {
            var nd = nodes[nodeId];
            accumulated += distances.TryGetValue(nodeId, out var d) ? d : 0;
            path.Add(new MobileRoutePointDto(nd.XCoord, nd.YCoord, nodeId, null));
        }

        if (request.EndObjectId.HasValue)
        {
            // Thêm điểm đích tại tâm của SemanticObject
            var destObject = await dbContext.SemanticObjects
                .AsNoTracking()
                .FirstOrDefaultAsync(so => so.ObjectId == request.EndObjectId.Value, cancellationToken);

            if (destObject is not null)
            {
                var centerX = (destObject.XMin + destObject.XMax) / 2;
                var centerY = (destObject.YMin + destObject.YMax) / 2;
                path.Add(new MobileRoutePointDto(centerX, centerY, null, destLabel ?? "Đích đến"));
            }
        }

        var totalDist = totalDistance + distToFirstNode;
        // Ước tính: robot đi với vận tốc ~0.5 m/s
        var estimatedSeconds = (int)Math.Ceiling(totalDist / 0.5);

        return new MobileRouteResponseDto(Math.Round(totalDist, 2), estimatedSeconds, path);
    }

    // ── SetNodeBlockedAsync ───────────────────────────────────────────────────

    public async Task SetNodeBlockedAsync(int nodeId, bool isBlocked, string? reason, CancellationToken cancellationToken = default)
    {
        var node = await dbContext.NavigationNodes
            .FirstOrDefaultAsync(n => n.NodeId == nodeId, cancellationToken)
            ?? throw new InvalidOperationException(localizer.Get("NodeNotFound", nodeId));

        node.IsBlocked = isBlocked;
        await dbContext.SaveChangesAsync(cancellationToken);
        _ = reason; // Reason chưa có cột lưu trong schema V4.0; bỏ qua (reserved cho tương lai)
    }

    // ── Graph algorithms ─────────────────────────────────────────────────────

    private static Dictionary<int, List<(int ToNodeId, double Weight)>> BuildAdjacency(
        IReadOnlyList<EdgeData> edges,
        IReadOnlyDictionary<int, NodeData> nodes)
    {
        var graph = nodes.Keys.ToDictionary(nodeId => nodeId, _ => new List<(int, double)>());

        foreach (var edge in edges)
        {
            if (!nodes.ContainsKey(edge.FromNodeId) || !nodes.ContainsKey(edge.ToNodeId))
                continue;

            if (nodes[edge.FromNodeId].IsBlocked || nodes[edge.ToNodeId].IsBlocked)
                continue;

            graph[edge.FromNodeId].Add((edge.ToNodeId, edge.Distance));

            if (edge.IsBidirectional)
                graph[edge.ToNodeId].Add((edge.FromNodeId, edge.Distance));
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

        if (distances.ContainsKey(startNodeId))
        {
            distances[startNodeId] = 0d;
            queue.Enqueue(startNodeId, 0d);
        }


        while (queue.TryDequeue(out var current, out var currentDistance))
        {
            if (currentDistance > distances[current]) continue;

            foreach (var (toNodeId, weight) in graph[current])
            {
                var nextDistance = currentDistance + weight;
                if (nextDistance >= distances[toNodeId]) continue;

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
            if (current == startNodeId) break;

            if (!previous.TryGetValue(current, out var parent) || parent is null)
                return [];

            current = parent.Value;
        }

        return stack.ToList();
    }
}

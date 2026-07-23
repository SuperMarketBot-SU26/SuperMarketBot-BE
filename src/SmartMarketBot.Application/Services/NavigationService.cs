using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Domain.Entities;

namespace SmartMarketBot.Application.Services;

public sealed class NavigationService(IAppDbContext dbContext, ILocalizationService localizer) : INavigationService
{
    private sealed record NodeData(int NodeId, string? NodeName, double XCoord, double YCoord, string? NodeType, bool IsBlocked);
    private sealed record EdgeData(int FromNodeId, int ToNodeId, double Distance, bool IsBidirectional);

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Tải danh sách NavigationNodes.
    /// Schema mới (V4.0): thay thế bảng ForbiddenZones bằng SEMANTIC_OBJECT.
    /// (Đã loại bỏ logic vùng cấm động obstacle).
    /// </summary>
    private async Task<Dictionary<int, NodeData>> LoadNodesAsync(CancellationToken ct)
    {
        return await dbContext.NavigationNodes
            .AsNoTracking()
            .Select(n => new NodeData(n.NodeId, n.NodeName, n.XCoord, n.YCoord, n.NodeType, n.IsBlocked))
            .ToDictionaryAsync(n => n.NodeId, ct);
    }

    // ── PlanRouteAsync ───────────────────────────────────────────────────────

    public async Task<RoutePlanResultDto> PlanRouteAsync(
        RoutePlanRequestDto request, CancellationToken cancellationToken = default)
    {
        var nodes = await LoadNodesAsync(cancellationToken);

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
        // 1. Load nodes + edges
        var nodes = await LoadNodesAsync(cancellationToken);

        var edges = await dbContext.NavigationEdges
            .AsNoTracking()
            .Select(e => new EdgeData(e.FromNodeId, e.ToNodeId, e.Distance, e.IsBidirectional))
            .ToListAsync(cancellationToken);

        // Lấy IDs vùng cấm để trả về response (thông tin cho client)
        var obstacleIds = new List<int>();

        // Adjacency graph
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

        int waypointOrder = waypoints.Count;

        for (int i = 0; i < orderedProductIds.Count; i++)
        {
            var pid = orderedProductIds[i];
            if (!productNodeMap.TryGetValue(pid, out var pn)) continue;

            var (dist, previous) = Dijkstra(adjacency, prevNode);
            var segDist = dist.TryGetValue(pn.NodeId, out var sd) && !double.IsInfinity(sd) && !double.IsNaN(sd) ? sd : 0;
            totalDistance += segDist;

            var legPath = ReconstructPath(previous, prevNode, pn.NodeId);
            foreach (var stepNodeId in legPath.Skip(1))
            {
                if (!nodes.TryGetValue(stepNodeId, out var stepNode)) continue;

                bool isTargetProduct = (stepNodeId == pn.NodeId);
                waypoints.Add(new ShoppingWaypointDto(
                    waypointOrder++,
                    stepNodeId,
                    $"{stepNode.XCoord:F1},{stepNode.YCoord:F1}",
                    stepNode.XCoord,
                    stepNode.YCoord,
                    isTargetProduct ? pid : null,
                    isTargetProduct ? $"Product #{pid}" : (stepNode.NodeName ?? "Lối đi"),
                    null,
                    isTargetProduct ? pn.SlotCode : null,
                    isTargetProduct ? (pn.SlotCode is null ? null : $"Slot {pn.SlotCode}") : "Hành lang"));
            }

            prevNode = pn.NodeId;
        }

        // 5. Tự động thêm điểm cuối là Quầy Thu Ngân (Checkout) nếu có trên sơ đồ
        var checkoutNode = nodes.Values.FirstOrDefault(n => "CHECKOUT".Equals(n.NodeType, StringComparison.OrdinalIgnoreCase) || (n.NodeName != null && n.NodeName.Contains("Checkout", StringComparison.OrdinalIgnoreCase)));
        if (checkoutNode != null && checkoutNode.NodeId != prevNode)
        {
            var (dist, previous) = Dijkstra(adjacency, prevNode);
            var segDist = dist.TryGetValue(checkoutNode.NodeId, out var sd) && !double.IsInfinity(sd) && !double.IsNaN(sd) ? sd : 0;
            totalDistance += segDist;

            var legPath = ReconstructPath(previous, prevNode, checkoutNode.NodeId);
            foreach (var stepNodeId in legPath.Skip(1))
            {
                if (!nodes.TryGetValue(stepNodeId, out var stepNode)) continue;

                bool isCheckoutFinal = (stepNodeId == checkoutNode.NodeId);
                waypoints.Add(new ShoppingWaypointDto(
                    waypointOrder++,
                    stepNodeId,
                    $"{stepNode.XCoord:F1},{stepNode.YCoord:F1}",
                    stepNode.XCoord,
                    stepNode.YCoord,
                    null,
                    isCheckoutFinal ? "Quầy Thu Ngân (Checkout)" : (stepNode.NodeName ?? "Lối đi"),
                    null,
                    isCheckoutFinal ? "CHECKOUT" : null,
                    isCheckoutFinal ? "Điểm kết thúc / Thanh toán" : "Hành lang"));
            }
        }


        waypoints = ExpandPathWithDetourNodes(waypoints);
        var safeTotalDistance = double.IsInfinity(totalDistance) || double.IsNaN(totalDistance) ? 0d : Math.Round(totalDistance, 2);

        return new OptimizeShoppingRouteResponseDto(
            safeTotalDistance,
            waypoints.Count,
            waypoints,
            obstacleIds,
            $"TSP Nearest-Neighbour + Dijkstra + Dynamic Detour.");
    }

    private static List<ShoppingWaypointDto> ExpandPathWithDetourNodes(
        List<ShoppingWaypointDto> rawWaypoints)
    {
        if (rawWaypoints.Count < 2) return rawWaypoints;

        var expanded = new List<ShoppingWaypointDto> { rawWaypoints[0] with { Order = 0 } };
        int orderCounter = 1;

        for (int i = 0; i < rawWaypoints.Count - 1; i++)
        {
            var curr = rawWaypoints[i];
            var next = rawWaypoints[i + 1];

            bool isDiagonal = Math.Abs(curr.X - next.X) > 1e-4 && Math.Abs(curr.Y - next.Y) > 1e-4;

            if (isDiagonal)
            {
                double cornerX = curr.X;
                double cornerY = next.Y;

                expanded.Add(new ShoppingWaypointDto(
                    orderCounter++,
                    0, // Virtual NodeId = 0
                    $"{cornerX:F1},{cornerY:F1}",
                    cornerX,
                    cornerY,
                    null,
                    "Corner Detour Node",
                    null,
                    null,
                    "Bẻ góc 90°"));
            }

            expanded.Add(next with { Order = orderCounter++ });
        }

        return expanded;
    }


    // ── FindMobileRouteAsync ─────────────────────────────────────────────────

    private static double DistanceSquared(double x1, double y1, double x2, double y2)
        => (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2);

    public async Task<MobileRouteResponseDto> FindMobileRouteAsync(
        MobileRouteRequestDto request, CancellationToken cancellationToken = default)
    {
        var nodes = await LoadNodesAsync(cancellationToken);

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

        var path = new List<MobileRoutePointDto>();
        double currentX = request.StartX;
        double currentY = request.StartY;

        path.Add(new MobileRoutePointDto(currentX, currentY, startNode.NodeId, "Điểm xuất phát"));

        foreach (var nodeId in pathIds)
        {
            var nd = nodes[nodeId];
            bool dx = Math.Abs(currentX - nd.XCoord) > 1e-4;
            bool dy = Math.Abs(currentY - nd.YCoord) > 1e-4;

            if (dx && dy)
            {
                // Bẻ góc 90 độ nếu lệch cả X và Y
                path.Add(new MobileRoutePointDto(currentX, nd.YCoord, 0, "Bẻ góc 90°"));
                currentY = nd.YCoord;
            }

            if (dx || dy)
            {
                path.Add(new MobileRoutePointDto(nd.XCoord, nd.YCoord, nodeId, null));
                currentX = nd.XCoord;
                currentY = nd.YCoord;
            }
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

                if (Math.Abs(currentX - centerX) > 1e-4 && Math.Abs(currentY - centerY) > 1e-4)
                {
                    path.Add(new MobileRoutePointDto(currentX, centerY, 0, "Bẻ góc điểm đích"));
                }
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
            if (!nodes.TryGetValue(edge.FromNodeId, out var from) || !nodes.TryGetValue(edge.ToNodeId, out var to))
                continue;

            if (from.IsBlocked || to.IsBlocked)
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

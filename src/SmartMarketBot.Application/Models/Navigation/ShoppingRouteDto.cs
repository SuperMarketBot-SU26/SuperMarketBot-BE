using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartMarketBot.Application.Models.Navigation;

// ─── Flow 1: Multi-stop Shopping Route ───────────────────────────────────────

/// <summary>Request tối ưu hoá lộ trình gom hàng đa điểm (TSP + Dijkstra + ForbiddenZones).</summary>
public sealed record OptimizeShoppingRouteRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "RobotId phải hợp lệ.")]
    public int RobotId { get; init; }

    public int? StartNodeId { get; init; }
    public double? StartX { get; init; }
    public double? StartY { get; init; }

    [Required(ErrorMessage = "Danh sách ProductIds là bắt buộc.")]
    [MinLength(1, ErrorMessage = "Danh sách ProductIds phải chứa ít nhất 1 sản phẩm.")]
    public IReadOnlyList<int>? ProductIds { get; init; }

    [JsonConstructor]
    public OptimizeShoppingRouteRequestDto(
        int robotId,
        int? startNodeId = null,
        double? startX = null,
        double? startY = null,
        IReadOnlyList<int>? productIds = null)
    {
        RobotId = robotId;
        StartNodeId = startNodeId;
        StartX = startX;
        StartY = startY;
        ProductIds = productIds;
    }

    public OptimizeShoppingRouteRequestDto(int robotId, int startNodeId, IReadOnlyList<int> productIds)
        : this(robotId, startNodeId, null, null, productIds) { }
}





/// <summary>Một điểm dừng trong lộ trình mua sắm tối ưu.</summary>
public sealed record ShoppingWaypointDto(
    int Order,
    int NodeId,
    string NodeName,
    double X,
    double Y,
    int? ProductId,
    string ProductName,
    int? ShelfLevelId,
    string? SlotCode,
    string? ShelfLocation);


public sealed record OptimizeShoppingRouteResponseDto(
    double TotalDistanceMeters,
    int WaypointCount,
    IReadOnlyList<ShoppingWaypointDto> Waypoints,
    IReadOnlyList<int> ExcludedForbiddenZoneIds,
    string OptimizationNote);

/// <summary>Block/Unblock một node (POST /api/navigation/nodes/{id}/block).</summary>
public sealed record BlockNodeRequestDto(bool IsBlocked, string? Reason);

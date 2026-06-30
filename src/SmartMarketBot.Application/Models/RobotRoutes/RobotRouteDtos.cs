namespace SmartMarketBot.Application.Models.RobotRoutes;

/// <summary>
/// Route hiển thị danh sách (GET list) — nhẹ, không load waypoints chi tiết.
/// </summary>
public sealed record RobotRouteListDto(
    int RobotRouteId,
    int MapId,
    string RouteName,
    string RouteType,
    string? Description,
    int? ZoneId,
    string? ZoneName,
    int RobotId,
    DateTime CreatedAt,
    int WaypointCount);

/// <summary>
/// Route đầy đủ kèm danh sách tọa độ waypoints (GET detail) — cho Android Robot vẽ polyline.
/// </summary>
public sealed record RobotRouteDetailDto(
    int RobotRouteId,
    int MapId,
    string RouteName,
    string RouteType,
    string? Description,
    int? ZoneId,
    string? ZoneName,
    int RobotId,
    DateTime CreatedAt,
    List<RouteWaypointDto> Waypoints);

/// <summary>
/// Tọa độ 1 waypoint trong route — gửi xuống Android/IOT để vẽ đường đi.
/// </summary>
public sealed record RouteWaypointDto(
    int NodeId,
    string? NodeName,
    double XCoord,
    double YCoord,
    int SequenceOrder);

/// <summary>
/// Tạo route mới: truyền danh sách NodeId theo thứ tự (SequenceOrder tự tăng từ 0).
/// </summary>
public sealed record RobotRouteCreateDto
{
    public required int MapId { get; init; }
    public required int RobotId { get; init; }
    public required string RouteName { get; init; }
    public string RouteType { get; init; } = "patrol";
    public string? Description { get; init; }
    /// <summary>Có thể null nếu route chưa gán khu vực.</summary>
    public int? ZoneId { get; init; }
    /// <summary>Danh sách NodeId sắp xếp theo thứ tự di chuyển. Nếu rỗng → route rỗng.</summary>
    public List<int> NodeIds { get; init; } = [];
}

/// <summary>
/// Cập nhật route: ghi đè hoàn toàn thông tin + danh sách waypoints.
/// </summary>
public sealed record RobotRouteUpdateDto
{
    public string RouteName { get; init; } = string.Empty;
    public string RouteType { get; init; } = "patrol";
    public string? Description { get; init; }
    public int? ZoneId { get; init; }
    /// <summary>Danh sách NodeId mới (ghi đè hoàn toàn, xóa route cũ).</summary>
    public List<int> NodeIds { get; init; } = [];
}

/// <summary>
/// Kết quả tạo / cập nhật route.
/// </summary>
public sealed record RobotRouteResultDto(
    int RobotRouteId,
    string RouteName,
    string Message);

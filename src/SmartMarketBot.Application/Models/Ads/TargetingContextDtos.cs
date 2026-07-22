namespace SmartMarketBot.Application.Models.Ads;

/// <summary>
/// Tóm tắt một SemanticObject loại "shelf" cho UI TargetingSelector — chỉ các field
/// mà dropdown "Chọn Kệ Hàng" thực sự cần. Không trả AABB/Confidence/DetectedAt vì
/// TargetingSelector không dùng.
/// </summary>
public sealed record ShelfSummaryDto(
    int ObjectId,
    string? Label,
    string ObjectType,
    int? ProductTypeId,
    string? ProductTypeName);

/// <summary>
/// Tóm tắt một RobotRoute cho UI TargetingSelector — khớp 1:1 với những gì
/// useMapAndRoutes.normalizeRoutes() + deriveZones() cần ở FE.
/// </summary>
public sealed record RouteSummaryDto(
    int RobotRouteId,
    string RouteName,
    int? ZoneId,
    string? ZoneName,
    int WaypointCount);

/// <summary>
/// Single-fetch payload cho TargetingSelector. Thay thế 4 HTTP call trước đây
/// (GET /maps/latest + GET /semantic-objects?pageSize=500 + GET /routes?mapId=
/// + GET /ad-campaigns/{id}/routes).
///
/// Lưu ý: zoneIds thuộc campaign KHÔNG có ở đây vì BE không tồn tại endpoint
/// nào đọc assigned zones cho campaign — xem comment tại TargetingSelector FE.
/// </summary>
public sealed record TargetingContextResponseDto(
    int MapId,
    int FloorId,
    IReadOnlyList<ShelfSummaryDto> Shelves,
    IReadOnlyList<RouteSummaryDto> Routes,
    IReadOnlyList<int> AssignedRouteIds);
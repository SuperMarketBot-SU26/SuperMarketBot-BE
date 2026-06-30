using SmartMarketBot.Application.Models.RobotRoutes;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// CRUD + filter routes theo map / zone / routeType.
/// Kết quả trả về đã bao gồm thông tin Zone (navigation) và số waypoints.
/// </summary>
public interface IRobotRouteService
{
    /// <summary>Lấy toàn bộ routes của 1 map, hỗ trợ filter theo zoneId / routeType.</summary>
    Task<IReadOnlyList<RobotRouteListDto>> GetRoutesByMapAsync(
        int mapId,
        int? zoneId = null,
        string? routeType = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy chi tiết route kèm danh sách waypoints đã sắp xếp theo SequenceOrder.</summary>
    Task<RobotRouteDetailDto?> GetRouteByIdAsync(
        int routeId,
        CancellationToken cancellationToken = default);

    /// <summary>Tạo route mới — tự động tạo RouteNodeMapping với SequenceOrder tăng dần từ 0.
    /// Nếu NodeIds rỗng → tạo route trống.</summary>
    Task<RobotRouteResultDto> CreateRouteAsync(
        RobotRouteCreateDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>Cập nhật route: ghi đè RouteName / RouteType / Description / ZoneId,
    /// xóa toàn bộ RouteNodeMapping cũ và tạo lại từ NodeIds mới.</summary>
    Task<RobotRouteResultDto> UpdateRouteAsync(
        int routeId,
        RobotRouteUpdateDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>Xóa route (cascade xóa RouteNodeMapping tự động).</summary>
    Task<bool> DeleteRouteAsync(int routeId, CancellationToken cancellationToken = default);
}

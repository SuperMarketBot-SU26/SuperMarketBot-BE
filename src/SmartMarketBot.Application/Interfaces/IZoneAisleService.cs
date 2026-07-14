using SmartMarketBot.Application.Models.Maps;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// Trả về dữ liệu Zone và Aisle để frontend (Web Manager, Mobile App)
/// render dropdown, filter và bản đồ mật độ kệ hàng.
/// </summary>
public interface IZoneAisleService
{
    /// <summary>Lấy tất cả Zone. Hỗ trợ lọc theo floorId.</summary>
    Task<IReadOnlyList<ZoneDto>> GetZonesAsync(int? floorId = null, CancellationToken ct = default);

    /// <summary>Lấy tất cả Aisle. Hỗ trợ lọc theo zoneId.</summary>
    Task<IReadOnlyList<AisleDto>> GetAislesAsync(int? zoneId = null, CancellationToken ct = default);

    /// <summary>
    /// Mật độ hàng hoá của từng kệ (Aisle) dựa trên lần AisleScan gần nhất.
    /// Trả kèm DensityColor (green/yellow/red) để frontend tô màu trực tiếp.
    /// Hỗ trợ lọc theo zoneId.
    /// </summary>
    Task<IReadOnlyList<AisleDensityDto>> GetAisleDensitiesAsync(int? zoneId = null, CancellationToken ct = default);
}

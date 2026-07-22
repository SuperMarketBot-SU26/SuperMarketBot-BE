namespace SmartMarketBot.Application.Models.Maps;

/// <summary>
/// Thông tin tóm tắt của 1 tầng trong siêu thị.
/// Dùng để render dropdown chọn tầng (Web Manager, Mobile App, Robot dashboard).
/// </summary>
public sealed record FloorDto(
    int FloorId,
    int FloorNumber,
    int ZoneCount,
    int MapCount);

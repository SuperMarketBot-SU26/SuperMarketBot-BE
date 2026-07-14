namespace SmartMarketBot.Application.Models.Maps;

/// <summary>Zone (khu vực) trên bản đồ tầng.</summary>
public sealed record ZoneDto(
    int ZoneId,
    int FloorId,
    string? ZoneName,
    string? Description);

/// <summary>Aisle (dãy kệ hàng) thuộc một Zone.</summary>
public sealed record AisleDto(
    int AisleId,
    int ZoneId,
    string AisleCode,
    string? AisleName);

/// <summary>Mật độ hàng hoá của một kệ sau lần scan gần nhất.</summary>
public sealed record AisleDensityDto(
    int AisleId,
    string AisleCode,
    string? AisleName,
    int? LatestScanId,
    DateTime? ScannedAt,
    decimal DensityPercentage,
    decimal EmptyPercentage,
    bool NeedsRestock,
    string? ImageUrl,
    /// <summary>Màu hiển thị tương ứng với mật độ: green ≥ 70%, yellow 40–69%, red &lt; 40%.</summary>
    string DensityColor);

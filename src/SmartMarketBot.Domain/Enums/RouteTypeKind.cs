namespace SmartMarketBot.Domain.Enums;

/// <summary>
/// Loại lộ trình robot. Lưu DB dưới dạng string lowercase (NVARCHAR(50)).
/// Wire/DB value controlled by <see cref="RouteTypeKindExtensions.ToDbString"/>.
///
/// Taxonomy:
///  - Patrol       : tuần tra quét kệ (mật độ, empty percentage)
///  - AdZone       : quảng cáo theo Zone (robot chạy quanh 1 zone quảng cáo)
///  - AdShelf      : quảng cáo theo Shelf (robot dừng tại kệ để phát ads)
///  - AdAutonomous : tự động phát ads (không gắn với shelf/zone cố định)
/// </summary>
public enum RouteTypeKind
{
    /// <summary>Tuần tra — quét kệ dọc đường, scan EmptyPercentage.</summary>
    Patrol = 0,

    /// <summary>Quảng cáo theo Zone (khu vực bản đồ) — robot chạy quanh 1 zone quảng cáo.</summary>
    AdZone = 1,

    /// <summary>Quảng cáo theo Shelf (kệ) — robot dừng tại kệ để phát ads.</summary>
    AdShelf = 2,

    /// <summary>Tự động phát ads — không gắn với shelf/zone cố định, do AI/hệ thống chọn.</summary>
    AdAutonomous = 3
}

public static class RouteTypeKindExtensions
{
    /// <summary>Wire/DB value (lowercase snake_case). KHÔNG đổi khi đã có data.</summary>
    public static string ToDbString(this RouteTypeKind kind) => kind switch
    {
        RouteTypeKind.Patrol       => "patrol",
        RouteTypeKind.AdZone       => "ad_zone",
        RouteTypeKind.AdShelf      => "ad_shelf",
        RouteTypeKind.AdAutonomous => "ad_autonomous",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown RouteTypeKind")
    };

    /// <summary>Parse an incoming string (FE wire format, DB column, query string).
    /// Case-insensitive để khớp cả "patrol" và "Patrol", "AdZone" và "ad_zone".</summary>
    public static bool TryParseDbString(string? value, out RouteTypeKind kind)
    {
        if (Enum.TryParse<RouteTypeKind>(value, ignoreCase: true, out kind))
            return Enum.IsDefined(typeof(RouteTypeKind), kind);
        kind = RouteTypeKind.Patrol;
        return false;
    }

    /// <summary>Tất cả wire values hợp lệ, dùng để build CHECK constraint & dropdown.</summary>
    public static IReadOnlyList<string> AllDbStrings { get; } =
        Enum.GetValues<RouteTypeKind>().Select(k => k.ToDbString()).ToList();

    /// <summary>Nhãn hiển thị (Tiếng Việt) — fallback khi localizer thiếu key.
    /// BE controller có thể dùng trực tiếp hoặc ưu tiên localizer.Get("RouteType_xxx").</summary>
    public static string DefaultLabel(this RouteTypeKind kind) => kind switch
    {
        RouteTypeKind.Patrol       => "Tuần tra",
        RouteTypeKind.AdZone       => "QC theo khu vực",
        RouteTypeKind.AdShelf      => "QC theo kệ",
        RouteTypeKind.AdAutonomous => "QC tự động",
        _ => kind.ToString()
    };

    public static string DefaultDescription(this RouteTypeKind kind) => kind switch
    {
        RouteTypeKind.Patrol       => "Robot di chuyển dọc các kệ để quét mật độ hàng hoá.",
        RouteTypeKind.AdZone       => "Robot chạy quanh một khu vực (zone) để phát quảng cáo cho khách.",
        RouteTypeKind.AdShelf      => "Robot dừng tại các kệ quảng cáo và phát nội dung tương ứng.",
        RouteTypeKind.AdAutonomous => "Robot tự chọn thời điểm & vị trí phát quảng cáo, không gắn với shelf/zone cố định.",
        _ => string.Empty
    };
}

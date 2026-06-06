namespace SmartMarketBot.Domain.Common;

/// <summary>
/// Cung cấp thời gian hiện tại theo múi giờ Việt Nam (UTC+7).
/// Dùng thay thế cho DateTime.UtcNow / DateTime.Now / DateTime.Today
/// trên toàn hệ thống để đảm bảo nhất quán múi giờ khi deploy lên Azure (UTC).
/// </summary>
public static class VnDateTime
{
    // Hỗ trợ cả Windows (Azure App Service Windows) và Linux (Azure App Service Linux/Container)
    private static readonly TimeZoneInfo VnTz = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

    /// <summary>Ngày giờ hiện tại theo giờ Việt Nam.</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTz);

    /// <summary>Ngày hiện tại theo giờ Việt Nam (giờ = 00:00:00).</summary>
    public static DateTime Today => Now.Date;

    /// <summary>Chỉ phần ngày (DateOnly) theo giờ Việt Nam.</summary>
    public static DateOnly DateToday => DateOnly.FromDateTime(Now);

    /// <summary>Chỉ phần giờ (TimeOnly) theo giờ Việt Nam.</summary>
    public static TimeOnly TimeNow => TimeOnly.FromDateTime(Now);
}

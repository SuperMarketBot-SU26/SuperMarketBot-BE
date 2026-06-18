using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Realtime;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// SKELETON — Bạn A: fill <c>PublishAlertAsync</c> để BE push realtime cho Staff khi có sự cố.
/// Gợi ý các trigger cần hook:
/// <list type="bullet">
///   <item><c>StaffService.ReportOutOfStockAsync</c> — OOS báo từ member/robot.</item>
///   <item><c>RobotService</c> nhận status = 'Error' từ MQTT.</item>
///   <item><c>MemberService.ScanItemAsync</c> — AllergyAlert severity cao.</item>
/// </list>
/// Hiện tại đã có DI Notifier; bạn A chỉ cần viết wrapper + persist alert (nếu cần) + gọi Notifier.
/// </summary>
public sealed class StaffRealtimeService(
    IStaffRealtimeNotifier notifier,
    ILogger<StaffRealtimeService> logger) : IStaffRealtimeService
{
    public async Task PublishAlertAsync(StaffRealtimeAlertDto alert, CancellationToken ct = default)
    {
        // TODO(bạn A): persist alert vào DB nếu cần, log structured, rồi broadcast.
        logger.LogInformation("Staff realtime alert: {AlertType} severity={Severity} slot={SlotId}",
            alert.AlertType, alert.Severity, alert.SlotId);
        await notifier.BroadcastAlertAsync(alert, ct);
    }

    public Task<int> GetUnreadCountAsync(CancellationToken ct = default)
    {
        // TODO(bạn A): query bảng alert (MEMBER_ALERT hoặc bảng STAFF_ALERT mới) chưa đọc.
        return Task.FromResult(0);
    }
}
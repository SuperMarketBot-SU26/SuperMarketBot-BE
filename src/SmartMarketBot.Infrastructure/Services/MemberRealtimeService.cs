using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Realtime;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// SKELETON — Bạn B: fill <c>PushUpdateAsync</c> + <c>GetPurchaseFrequencyAsync</c>.
/// Frequency scoring query gợi ý: dùng bảng <c>MEMBER_CART</c> hoặc <c>ORDER</c> đã có, đếm trong N ngày.
/// Nếu bạn B muốn bảng <c>MEMBER_PURCHASE_HISTORY</c> riêng — liên hệ lead trước khi ALTER (đã chốt không thêm bảng).
/// </summary>
public sealed class MemberRealtimeService(
    IMemberRealtimeNotifier notifier,
    AppDbContext db,
    ILogger<MemberRealtimeService> logger) : IMemberRealtimeService
{
    public async Task PushUpdateAsync(int memberId, MemberRealtimeUpdateDto update, CancellationToken ct = default)
    {
        // TODO(bạn B): persist vào MEMBER_ALERT (nếu cần), rồi push realtime.
        logger.LogInformation("Member {MemberId} realtime update: {UpdateType}", memberId, update.UpdateType);
        await notifier.PushToMemberAsync(memberId, update, ct);
    }

    public async Task<int> GetPurchaseFrequencyAsync(int memberId, int productId, int windowDays = 30, CancellationToken ct = default)
    {
        // TODO(bạn B): đếm số lần Member mua Product trong windowDays. Ví dụ:
        //   - Nếu có bảng ORDER/INVOICE: WHERE MemberID = @mid AND ProductID = @pid AND CreatedAt >= GETUTCDATE() - @days
        //   - Hoặc dùng MEMBER_CART.ScanTime làm proxy.
        // Tạm trả 0 cho skeleton.
        await Task.CompletedTask;
        return 0;
    }
}
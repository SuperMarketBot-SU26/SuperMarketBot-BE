using SmartMarketBot.Application.Models.Realtime;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// Flow 3 — Realtime cho Member App.
/// Bạn B: fill <c>MemberRealtimeService</c> implementation. Mục tiêu thêm: frequency scoring theo
/// <c>MemberPurchaseHistory</c> (cần bảng mới nếu muốn) hoặc tận dụng <c>MEMBER_ALERT</c> + <c>MEMBER_CART</c> đã có.
/// </summary>
public interface IMemberRealtimeService
{
    /// <summary>Push update tới Member client đang subscribe <c>/hubs/member</c> với <c>memberId</c> cụ thể.</summary>
    Task PushUpdateAsync(int memberId, MemberRealtimeUpdateDto update, CancellationToken ct = default);

    /// <summary>Bạn B: implement frequency scoring — đếm số lần Member mua Product trong 30 ngày gần nhất.</summary>
    Task<int> GetPurchaseFrequencyAsync(int memberId, int productId, int windowDays = 30, CancellationToken ct = default);
}
namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// Ghi notification vào DB trong scope riêng (dùng sau khi request scope đã dispose).
/// Tách khỏi MemberService để tránh dùng DbContext đã disposed khi ghi fire-and-forget.
/// </summary>
public interface IMemberNotificationWriter
{
    Task SaveAsync(
        int memberId,
        string notifType,
        string title,
        string message,
        string? payloadJson = null,
        CancellationToken ct = default);
}

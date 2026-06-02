using SmartMarketBot.Application.Models.Members;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Flow 3 — Budget &amp; Health + Flow 2 Deal Hunter + Member Alerts.</summary>
public interface IMemberService
{
    // ── Flow 3: Budget & Health ──────────────────────────────────────────────

    /// <summary>Cài đặt ngân sách stop-loss cho phiên mua sắm.</summary>
    Task<SetBudgetResponseDto> SetShoppingBudgetAsync(int memberId, SetBudgetRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Quét barcode sản phẩm vào giỏ hàng.
    /// Kiểm tra: (1) dị ứng, (2) vượt ngân sách, (3) mua trùng sản phẩm.
    /// Lưu MemberAlert nếu có cảnh báo.
    /// </summary>
    Task<ScanItemResponseDto> ScanItemAsync(int memberId, ScanItemRequestDto request, CancellationToken ct = default);

    // ── Flow 2: Deal Hunter ──────────────────────────────────────────────────

    /// <summary>Lấy danh sách deal/ưu đãi cá nhân hóa dựa trên lịch sử mua sắm.</summary>
    Task<MemberDealsResponseDto> GetPersonalizedDealsAsync(int memberId, CancellationToken ct = default);

    // ── Alerts ───────────────────────────────────────────────────────────────

    Task<MemberAlertsResponseDto> GetAlertsAsync(int memberId, CancellationToken ct = default);

    Task MarkAlertsReadAsync(int memberId, MarkAlertsReadRequestDto request, CancellationToken ct = default);
}

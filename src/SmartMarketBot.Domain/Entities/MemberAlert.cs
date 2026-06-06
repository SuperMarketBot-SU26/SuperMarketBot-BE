using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Lịch sử cảnh báo cá nhân hoá gửi đến hội viên.
/// AlertType: 'Allergy' | 'BudgetExceeded' | 'DuplicatePurchase' | 'OutOfStock'
/// Buổi 16 — Thầy Đỗ Tấn Nhàn.
/// </summary>
public class MemberAlert
{
    public int AlertID { get; set; }
    public int MemberID { get; set; }

    /// <summary>'Allergy' | 'BudgetExceeded' | 'DuplicatePurchase' | 'OutOfStock'</summary>
    public string AlertType { get; set; } = string.Empty;

    public string AlertMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = VnDateTime.Now;
    public bool IsRead { get; set; } = false;

    public virtual Member Member { get; set; } = null!;
}

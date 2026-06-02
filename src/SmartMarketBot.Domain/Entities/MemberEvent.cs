namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Sự kiện đặc biệt của hội viên (Birthday, Anniversary, VIPUpgrade).
/// Robot sẽ đọc bảng này mỗi buổi sáng để chủ động tặng coupon/chào mừng.
/// Buổi 16 — Thầy Đỗ Tấn Nhàn.
/// </summary>
public class MemberEvent
{
    public int EventID { get; set; }
    public int MemberID { get; set; }

    /// <summary>'Birthday' | 'Anniversary' | 'VIPUpgrade'</summary>
    public string EventName { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    /// <summary>% giảm giá riêng (VD: 15.00 = 15%) — null = không giảm</summary>
    public decimal? DiscountPct { get; set; }

    /// <summary>false = chưa xử lý, true = đã gửi coupon/chào mừng</summary>
    public bool IsProcessed { get; set; } = false;

    public virtual Member Member { get; set; } = null!;
}

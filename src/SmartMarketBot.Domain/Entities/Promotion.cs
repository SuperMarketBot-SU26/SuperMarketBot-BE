namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Khuyến mãi giảm giá sản phẩm. ERD V4.0 không có bảng này - code cũ dùng Promotion,
/// giờ gộp vào AD_CAMPAIGN. Tạm giữ riêng để không vỡ service cũ.
/// </summary>
public class Promotion
{
    public int PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

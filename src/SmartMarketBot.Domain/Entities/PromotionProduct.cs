namespace SmartMarketBot.Domain.Entities;

/// <summary>Bảng nối N-N giữa Promotion và Product.</summary>
public class PromotionProduct
{
    public int PromotionId { get; set; }
    public int ProductId { get; set; }
    public int Priority { get; set; }
}

namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Liên kết N-N giữa AdCampaign và RobotRoute.
/// Mỗi row là một quyền phát quảng cáo trên một route cụ thể.
/// Khai báo khi brand activate campaign: chỉ những campaign có route trùng
/// với route robot đang chạy (RouteAssignment.Status = Active) mới được phát.
/// PriceRoute trên AdPackage = đơn giá / 1 route, tính lúc activate.
/// </summary>
public sealed class AdCampaignRoute
{
    public int AdCampaignId { get; set; }
    public int RobotRouteId { get; set; }
    public decimal RoutePriceCharged { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public AdCampaign? AdCampaign { get; set; }
    public RobotRoute? RobotRoute { get; set; }
}

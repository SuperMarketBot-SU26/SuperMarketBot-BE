using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// API General Deals — lấy tất cả sản phẩm đang giảm giá dành cho Guest và User.
/// Nguồn deal: Product.PromotionPrice + SponsoredProducts thuộc AdCampaign Active.
/// </summary>
public interface IGeneralDealService
{
    /// <summary>
    /// Lấy danh sách deal toàn hệ thống, có lọc + phân trang.
    /// </summary>
    Task<GeneralDealsResponseDto> GetDealsAsync(
        GeneralDealsFilterDto filter,
        int? memberId,
        CancellationToken ct = default);
}

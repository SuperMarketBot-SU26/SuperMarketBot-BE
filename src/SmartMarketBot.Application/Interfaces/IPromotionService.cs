using SmartMarketBot.Application.Models.Promotions;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Flow 5 — Ads Monetization: tính Priority Score và trả về sản phẩm tài trợ gợi ý.</summary>
public interface IPromotionService
{
    /// <summary>
    /// Lấy danh sách sản phẩm gợi ý cá nhân hóa.
    /// Priority Score = AdScore (SponsoredProducts) + CustomerMatchScore (SearchMode/Allergy) + PromotionScore.
    /// </summary>
    Task<SponsoredRecommendationResponseDto> GetSponsoredRecommendationsAsync(
        SponsoredRecommendationQueryDto query,
        CancellationToken ct = default);
}

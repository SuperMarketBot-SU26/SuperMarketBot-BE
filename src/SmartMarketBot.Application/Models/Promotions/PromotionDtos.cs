namespace SmartMarketBot.Application.Models.Promotions;

// ─── Flow 5: Ads Monetization ────────────────────────────────────────────────

/// <summary>Truy vấn lấy danh sách sản phẩm gợi ý cá nhân hóa (có tài trợ).</summary>
public sealed record SponsoredRecommendationQueryDto(
    int MemberId,
    string? Query,
    int Limit = 5);

/// <summary>Một sản phẩm trong danh sách gợi ý đã tính Priority Score.</summary>
public sealed record SponsoredRecommendationDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    string? ImageUrl,
    string? Barcode,
    /// <summary>Điểm ưu tiên tổng = AdScore + CustomerMatchScore + PromotionScore</summary>
    int PriorityScore,
    int AdScore,
    int CustomerMatchScore,
    int PromotionScore,
    bool IsSponsored,
    bool HasActivePromotion,
    decimal? DiscountedPrice,
    string? SponsorBrand,
    /// <summary>Cờ cảnh báo: sản phẩm chứa thành phần dị ứng của member này</summary>
    bool HasAllergyWarning,
    string? AllergyWarningDetail);

/// <summary>Response wrapping toàn bộ panel quảng cáo + danh sách gợi ý.</summary>
public sealed record SponsoredRecommendationResponseDto(
    int MemberId,
    string SearchMode,
    IReadOnlyList<SponsoredRecommendationDto> Recommendations,
    int TotalCount);

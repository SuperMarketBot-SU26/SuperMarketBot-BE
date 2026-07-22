namespace SmartMarketBot.Application.Models.Ads;

/// <summary>1 Sponsored product trong response (kèm cờ allergy conflict cho FE).</summary>
public sealed record SponsoredRecommendationDto(
    int SponsoredId,
    int AdCampaignId,
    string CampaignName,
    int BrandId,
    string BrandName,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    decimal? PromotionPrice,
    string? ImageUrl,
    int? SlotId,
    string? SlotCode,
    int? ZoneId,
    string? ZoneName,
    int Priority,
    int ProfileScore,
    int WeekendBonus,
    int SystemBrandBoost,
    int TotalScore,
    bool IsSystemBrand,
    bool HasAllergenConflict,
    IReadOnlyList<string> AllergenConflicts);

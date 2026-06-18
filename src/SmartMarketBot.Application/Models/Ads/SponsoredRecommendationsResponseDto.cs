namespace SmartMarketBot.Application.Models.Ads;

public sealed record SponsoredRecommendationsResponseDto(
    int MemberId,
    int? ContextSlotId,
    int? ContextZoneId,
    string? ContextZoneName,
    int TotalCount,
    IReadOnlyList<SponsoredRecommendationDto> Items);

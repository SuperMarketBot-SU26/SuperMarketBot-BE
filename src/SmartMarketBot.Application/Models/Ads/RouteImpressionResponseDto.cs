namespace SmartMarketBot.Application.Models.Ads;

public sealed record RouteImpressionLogItem(
    int SponsoredId,
    int ProductId,
    int AdCampaignId,
    decimal ChargedAmount);

public sealed record RouteImpressionResponseDto(
    string RobotCode,
    int SlotId,
    int? SemanticObjectId,
    int ImpressionCount,
    decimal TotalChargedAmount,
    IReadOnlyList<RouteImpressionLogItem> Logs,
    string Message);

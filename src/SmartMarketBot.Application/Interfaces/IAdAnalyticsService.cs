using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Phase B - Flow 1: ghi nhận Route-based impression + analytics theo Zone.</summary>
public interface IAdAnalyticsService
{
    /// <summary>
    /// Robot vừa tới Slot thuộc Zone có <c>AdCampaign.RobotZoneId</c> active.
    /// BE log 1 impression (ActionType='RoutePass') cho mỗi SponsoredProduct trong campaign
    /// mà <c>Status='Active'</c> và còn trong thời gian chạy.
    /// Charge = <c>AdPackage.PriceRoute</c> cho 1 lượt chạy qua (Route-based billing).
    /// </summary>
    Task<RouteImpressionResponseDto> RecordRoutePassAsync(string robotCode, RouteImpressionRequestDto request, CancellationToken ct = default);
}

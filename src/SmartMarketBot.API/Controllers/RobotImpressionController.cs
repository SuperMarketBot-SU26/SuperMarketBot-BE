using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// Phase B - Flow 1: Route-based impression recording (Robot → BE).
/// Base route: api/robots/{robotCode}/impression
/// </summary>
[ApiController]
[Route("api/robots")]
public sealed class RobotImpressionController(IAdAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// Robot báo vừa tới Slot X tọa độ (x,y). BE tìm Zone, query các AdCampaign active
    /// gắn với Zone đó, và ghi 1 impression (ActionType='RoutePass') cho từng Sponsored product
    /// trong campaign. Charge = AdPackage.PriceRoute (Route-based billing).
    /// </summary>
    [HttpPost("{robotCode}/impression")]
    [AllowAnonymous]
    public async Task<ActionResult<RouteImpressionResponseDto>> RecordImpression(
        string robotCode,
        [FromBody] RouteImpressionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await analyticsService.RecordRoutePassAsync(robotCode, request, cancellationToken);
        return Ok(result);
    }
}

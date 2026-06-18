using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// Phase B - Flow 1: Sponsored Recommendations cho Member App.
/// Base route: api/members/{memberId}/sponsored-recommendations
/// </summary>
[ApiController]
[Route("api/members")]
public sealed class MemberSponsoredController(IAdRecommendationService recService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách Sponsored product đang chạy cho Member, sắp xếp theo điểm ưu tiên.
    /// Nếu truyền <c>?slotId=...</c> sẽ ưu tiên Sponsored cho Product đặt tại Slot đó
    /// và xác định Zone để lọc theo <c>AdCampaign.RobotZoneId</c>.
    /// </summary>
    [HttpGet("{memberId:int}/sponsored-recommendations")]
    [AllowAnonymous]
    public async Task<ActionResult<SponsoredRecommendationsResponseDto>> GetSponsored(
        int memberId,
        [FromQuery] int? slotId,
        CancellationToken cancellationToken)
    {
        var result = await recService.GetRecommendationsAsync(memberId, slotId, cancellationToken);
        return Ok(result);
    }
}

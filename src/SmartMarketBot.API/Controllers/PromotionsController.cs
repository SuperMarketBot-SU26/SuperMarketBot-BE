using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Promotions;

namespace SmartMarketBot.API.Controllers;

/// <summary>Flow 5 — Ads Monetization: gợi ý sản phẩm tài trợ cá nhân hóa với Priority Score.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PromotionsController(IPromotionService promotionService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách sản phẩm gợi ý cá nhân hóa (sponsored + non-sponsored) đã tính Priority Score.
    /// Priority Score = AdScore + CustomerMatchScore (SearchMode/Allergy) + PromotionScore.
    /// </summary>
    [HttpGet("sponsored-recommendations")]
    [AllowAnonymous]
    public async Task<ActionResult<SponsoredRecommendationResponseDto>> GetSponsoredRecommendations(
        [FromQuery] int memberId,
        [FromQuery] string? query,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await promotionService.GetSponsoredRecommendationsAsync(
            new SponsoredRecommendationQueryDto(memberId, query, limit),
            cancellationToken);
        return Ok(result);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/ad-campaign")]
public sealed class AdCampaignController(IAdCampaignService adCampaignService) : ControllerBase
{
    [HttpGet("robot-playlist/{robotId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<RobotPlaylistResponseDto>> GetRobotPlaylist(
        int robotId,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.GetRobotPlaylistAsync(robotId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("log-interaction")]
    [AllowAnonymous]
    public async Task<ActionResult<LogInteractionResponseDto>> LogInteraction(
        [FromBody] LogInteractionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.LogInteractionAsync(request, cancellationToken);
        return Ok(result);
    }
}

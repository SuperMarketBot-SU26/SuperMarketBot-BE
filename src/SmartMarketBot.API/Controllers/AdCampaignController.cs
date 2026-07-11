using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/ad-campaign")]
public sealed class AdCampaignController(
    IAdCampaignService adCampaignService) : ControllerBase
{
    [HttpGet("robot-playlist/{robotId}")]
    [AllowAnonymous]
    public async Task<ActionResult<RobotPlaylistResponseDto>> GetRobotPlaylist(
        int robotId,
        [FromQuery] int? semanticObjectId,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.GetRobotPlaylistAsync(robotId, semanticObjectId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("robot-playlist/{robotId}/zone/{zoneId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ZonePlaylistResponseDto>> GetZonePlaylist(
        int robotId,
        int zoneId,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.GetZonePlaylistAsync(robotId, zoneId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("robot-playlist/{robotId}/autonomous")]
    [AllowAnonymous]
    public async Task<ActionResult<AutonomousRouteDto>> GetAutonomousRoute(
        int robotId,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.GetAutonomousRouteAsync(robotId, cancellationToken);
        if (result == null)
            return NotFound(new { message = "No active route assigned to this robot." });
        return Ok(result);
    }

    [HttpGet("robot-playlist/{robotId}/node/{nodeId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<RobotPlaylistResponseDto>> GetPlaylistForNode(
        int robotId,
        int nodeId,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.GetPlaylistForNodeAsync(robotId, nodeId, cancellationToken);
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

    [HttpPut("session/bind")]
    [AllowAnonymous]
    public async Task<ActionResult<SessionBindResponseDto>> BindSession(
        [FromBody] SessionBindRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.BindSessionAsync(request, cancellationToken);
        return Ok(result);
    }
}

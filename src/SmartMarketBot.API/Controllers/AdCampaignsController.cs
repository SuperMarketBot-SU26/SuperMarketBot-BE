using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/ad-campaigns")]
public sealed class AdCampaignsController(IAdCampaignService adCampaignService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CampaignResponseDto>>> GetList(
        [FromQuery] CampaignListRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.GetListAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{campaignId:int}")]
    public async Task<ActionResult<CampaignResponseDto>> GetById(int campaignId, CancellationToken cancellationToken)
    {
        var campaign = await adCampaignService.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null)
            return NotFound();
        return Ok(campaign);
    }

    [HttpPost]
    public async Task<ActionResult<CampaignResponseDto>> Create(
        [FromBody] CreateCampaignRequestDto request,
        CancellationToken cancellationToken)
    {
        var campaign = await adCampaignService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { campaignId = campaign.AdCampaignId }, campaign);
    }

    [HttpPut("{campaignId:int}")]
    public async Task<ActionResult<CampaignResponseDto>> Update(
        int campaignId,
        [FromBody] UpdateCampaignRequestDto request,
        CancellationToken cancellationToken)
    {
        var campaign = await adCampaignService.UpdateAsync(campaignId, request, cancellationToken);
        return Ok(campaign);
    }

    [HttpDelete("{campaignId:int}")]
    public async Task<ActionResult> Delete(int campaignId, CancellationToken cancellationToken)
    {
        await adCampaignService.DeleteAsync(campaignId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{campaignId:int}/activate")]
    public async Task<ActionResult<ActivateCampaignResponseDto>> Activate(
        int campaignId,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.ActivateAsync(campaignId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{campaignId:int}/pause")]
    public async Task<ActionResult<PauseCampaignResponseDto>> Pause(
        int campaignId,
        [FromBody] PauseCampaignRequestDto? request,
        CancellationToken cancellationToken)
    {
        var reason = request?.Reason ?? "Manual pause by admin";
        var result = await adCampaignService.PauseAsync(campaignId, reason, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{campaignId:int}/cancel")]
    public async Task<ActionResult<CancelCampaignResponseDto>> Cancel(
        int campaignId,
        CancellationToken cancellationToken)
    {
        var result = await adCampaignService.CancelAsync(campaignId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{campaignId:int}/logs")]
    public async Task<ActionResult<PaginatedResponse<AdCampaignLogDto>>> GetLogs(
        int campaignId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await adCampaignService.GetCampaignLogsAsync(campaignId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }
}

public sealed record PauseCampaignRequestDto(string? Reason);

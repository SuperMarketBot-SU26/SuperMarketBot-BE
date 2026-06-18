using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers.Admin;

/// <summary>Flow 6 - Admin: CRUD AdCampaign.</summary>
[ApiController]
[Route("api/admin/ad-campaigns")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminAdCampaignsController(IAdminAdCampaignService campaignService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdCampaignDto>>> GetAll(
        [FromQuery] int? brandId, CancellationToken ct)
        => Ok(await campaignService.GetAllAsync(brandId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdCampaignDto>> GetById(int id, CancellationToken ct)
    {
        var c = await campaignService.GetByIdAsync(id, ct);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<ActionResult<AdCampaignDto>> Create([FromBody] CreateAdCampaignRequestDto request, CancellationToken ct)
    {
        var c = await campaignService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = c.AdCampaignId }, c);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdCampaignDto>> Update(int id, [FromBody] UpdateAdCampaignRequestDto request, CancellationToken ct)
        => Ok(await campaignService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await campaignService.DeleteAsync(id, ct);
        return NoContent();
    }
}
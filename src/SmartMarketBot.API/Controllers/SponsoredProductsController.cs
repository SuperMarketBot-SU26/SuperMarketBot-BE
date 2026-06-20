using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/sponsored-products")]
public sealed class SponsoredProductsController(ISponsoredProductService sponsoredProductService) : ControllerBase
{
    [HttpGet("campaign/{campaignId:int}")]
    public async Task<ActionResult<IReadOnlyList<SponsoredProductDto>>> GetByCampaign(
        int campaignId,
        CancellationToken cancellationToken)
    {
        var products = await sponsoredProductService.GetByCampaignIdAsync(campaignId, cancellationToken);
        return Ok(products);
    }

    [HttpGet("{sponsoredId:int}")]
    public async Task<ActionResult<SponsoredProductDto>> GetById(int sponsoredId, CancellationToken cancellationToken)
    {
        var product = await sponsoredProductService.GetByIdAsync(sponsoredId, cancellationToken);
        if (product is null)
            return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<SponsoredProductDto>> Create(
        [FromBody] AddSponsoredProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var product = await sponsoredProductService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { sponsoredId = product.SponsoredId }, product);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<SponsoredProductDto>>> BulkCreate(
        [FromBody] BulkAddSponsoredProductRequestDto request,
        CancellationToken cancellationToken)
    {
        var products = await sponsoredProductService.BulkCreateAsync(request, cancellationToken);
        return Ok(products);
    }

    [HttpPut("{sponsoredId:int}/priority")]
    public async Task<ActionResult<SponsoredProductDto>> UpdatePriority(
        int sponsoredId,
        [FromBody] UpdateSponsoredProductPriorityDto request,
        CancellationToken cancellationToken)
    {
        var product = await sponsoredProductService.UpdatePriorityAsync(sponsoredId, request, cancellationToken);
        return Ok(product);
    }

    [HttpDelete("{sponsoredId:int}")]
    public async Task<ActionResult> Delete(int sponsoredId, CancellationToken cancellationToken)
    {
        await sponsoredProductService.DeleteAsync(sponsoredId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{sponsoredId:int}/status")]
    public async Task<ActionResult> UpdateStatus(
        int sponsoredId,
        [FromBody] UpdateStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        await sponsoredProductService.UpdateStatusAsync(sponsoredId, request.Status, cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateStatusRequestDto(string Status);

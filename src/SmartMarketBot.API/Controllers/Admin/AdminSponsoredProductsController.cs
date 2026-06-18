using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers.Admin;

/// <summary>Flow 6 - Admin: CRUD SponsoredProduct (mapping AdCampaign ↔ Product).</summary>
[ApiController]
[Route("api/admin/sponsored-products")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminSponsoredProductsController(IAdminSponsoredProductService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SponsoredProductDto>>> GetAll(
        [FromQuery] int? campaignId, [FromQuery] int? productId, CancellationToken ct)
        => Ok(await service.GetAllAsync(campaignId, productId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SponsoredProductDto>> GetById(int id, CancellationToken ct)
    {
        var sp = await service.GetByIdAsync(id, ct);
        return sp is null ? NotFound() : Ok(sp);
    }

    [HttpPost]
    public async Task<ActionResult<SponsoredProductDto>> Create([FromBody] CreateSponsoredProductRequestDto request, CancellationToken ct)
    {
        var sp = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = sp.SponsoredId }, sp);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SponsoredProductDto>> Update(int id, [FromBody] UpdateSponsoredProductRequestDto request, CancellationToken ct)
        => Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers.Admin;

/// <summary>Flow 6 - Admin: quản lý Brand và ví tiền.</summary>
[ApiController]
[Route("api/admin/brands")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminBrandsController(IAdminBrandService brandService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> GetAll(CancellationToken ct)
        => Ok(await brandService.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BrandDto>> GetById(int id, CancellationToken ct)
    {
        var brand = await brandService.GetByIdAsync(id, ct);
        return brand is null ? NotFound() : Ok(brand);
    }

    [HttpPost]
    public async Task<ActionResult<BrandDto>> Create([FromBody] CreateBrandRequestDto request, CancellationToken ct)
    {
        var brand = await brandService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = brand.BrandId }, brand);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BrandDto>> Update(int id, [FromBody] UpdateBrandRequestDto request, CancellationToken ct)
        => Ok(await brandService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await brandService.DeleteAsync(id, ct);
        return NoContent();
    }
}
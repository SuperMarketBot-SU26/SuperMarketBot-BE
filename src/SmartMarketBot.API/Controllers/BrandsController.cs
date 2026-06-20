using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/brands")]
public sealed class BrandsController(IBrandService brandService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> GetAll(CancellationToken cancellationToken)
    {
        var brands = await brandService.GetAllAsync(cancellationToken);
        return Ok(brands);
    }

    [HttpGet("{brandId:int}")]
    public async Task<ActionResult<BrandDto>> GetById(int brandId, CancellationToken cancellationToken)
    {
        var brand = await brandService.GetByIdAsync(brandId, cancellationToken);
        if (brand is null)
            return NotFound();
        return Ok(brand);
    }

    [HttpPost]
    public async Task<ActionResult<BrandDto>> Create(
        [FromBody] CreateBrandRequestDto request,
        CancellationToken cancellationToken)
    {
        var brand = await brandService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { brandId = brand.BrandId }, brand);
    }

    [HttpPut("{brandId:int}")]
    public async Task<ActionResult<BrandDto>> Update(
        int brandId,
        [FromBody] UpdateBrandRequestDto request,
        CancellationToken cancellationToken)
    {
        var brand = await brandService.UpdateAsync(brandId, request, cancellationToken);
        return Ok(brand);
    }

    [HttpDelete("{brandId:int}")]
    public async Task<ActionResult> Delete(int brandId, CancellationToken cancellationToken)
    {
        await brandService.DeleteAsync(brandId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{brandId:int}/wallet/topup")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TopUpWalletResponseDto>> TopUpWallet(
        int brandId,
        [FromBody] TopUpWalletRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await brandService.TopUpWalletAsync(brandId, request, cancellationToken);
        return Ok(result);
    }
}

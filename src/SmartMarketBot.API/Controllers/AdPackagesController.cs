using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/ad-packages")]
public sealed class AdPackagesController(IAdPackageService adPackageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdPackageDto>>> GetAll(CancellationToken cancellationToken)
    {
        var packages = await adPackageService.GetAllAsync(cancellationToken);
        return Ok(packages);
    }

    [HttpGet("{packageId:int}")]
    public async Task<ActionResult<AdPackageDto>> GetById(int packageId, CancellationToken cancellationToken)
    {
        var package = await adPackageService.GetByIdAsync(packageId, cancellationToken);
        if (package is null)
            return NotFound();
        return Ok(package);
    }

    [HttpPost]
    public async Task<ActionResult<AdPackageDto>> Create(
        [FromBody] CreateAdPackageRequestDto request,
        CancellationToken cancellationToken)
    {
        var package = await adPackageService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { packageId = package.PackageId }, package);
    }

    [HttpPut("{packageId:int}")]
    public async Task<ActionResult<AdPackageDto>> Update(
        int packageId,
        [FromBody] UpdateAdPackageRequestDto request,
        CancellationToken cancellationToken)
    {
        var package = await adPackageService.UpdateAsync(packageId, request, cancellationToken);
        return Ok(package);
    }

    [HttpDelete("{packageId:int}")]
    public async Task<ActionResult> Delete(int packageId, CancellationToken cancellationToken)
    {
        await adPackageService.DeleteAsync(packageId, cancellationToken);
        return NoContent();
    }
}

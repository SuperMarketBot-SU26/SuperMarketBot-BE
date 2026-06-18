using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Admin;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers.Admin;

/// <summary>Flow 6 - Admin: CRUD AdPackage.</summary>
[ApiController]
[Route("api/admin/ad-packages")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminAdPackagesController(IAdminAdPackageService packageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdPackageDto>>> GetAll(CancellationToken ct)
        => Ok(await packageService.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdPackageDto>> GetById(int id, CancellationToken ct)
    {
        var pkg = await packageService.GetByIdAsync(id, ct);
        return pkg is null ? NotFound() : Ok(pkg);
    }

    [HttpPost]
    public async Task<ActionResult<AdPackageDto>> Create([FromBody] CreateAdPackageRequestDto request, CancellationToken ct)
    {
        var pkg = await packageService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = pkg.PackageId }, pkg);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdPackageDto>> Update(int id, [FromBody] UpdateAdPackageRequestDto request, CancellationToken ct)
        => Ok(await packageService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await packageService.DeleteAsync(id, ct);
        return NoContent();
    }
}
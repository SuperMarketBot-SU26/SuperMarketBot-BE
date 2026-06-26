using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Roles = Roles.AdminOrStaff)]
public sealed class AdminProductsController(IAdminProductService adminProductService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var created = await adminProductService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Update), new { productId = created.ProductId }, created);
    }

    [HttpPut("{productId:int}")]
    public async Task<ActionResult<ProductDto>> Update(int productId, [FromBody] UpdateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var updated = await adminProductService.UpdateProductAsync(productId, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{productId:int}/status")]
    public async Task<ActionResult<ProductDto>> UpdateStatus(int productId, [FromBody] UpdateProductStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var updated = await adminProductService.UpdateProductStatusAsync(productId, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Delete(int productId, CancellationToken cancellationToken = default)
    {
        var deleted = await adminProductService.DeleteProductAsync(productId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

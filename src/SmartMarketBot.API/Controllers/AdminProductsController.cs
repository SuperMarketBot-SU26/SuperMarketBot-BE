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
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProductDto>> Create(
        [FromForm] CreateProductRequestDto request,
        IFormFile? imageFile,
        CancellationToken cancellationToken = default)
    {
        Stream? stream = null;
        string? fileName = null;
        if (imageFile is { Length: > 0 })
        {
            stream = imageFile.OpenReadStream();
            fileName = imageFile.FileName;
        }

        try
        {
            var created = await adminProductService.CreateProductAsync(request, stream, fileName, cancellationToken);
            return CreatedAtAction(nameof(Update), new { productId = created.ProductId }, created);
        }
        finally
        {
            if (stream is not null) await stream.DisposeAsync();
        }
    }

    [HttpPut("{productId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProductDto>> Update(
        int productId,
        [FromForm] UpdateProductRequestDto request,
        IFormFile? imageFile,
        CancellationToken cancellationToken = default)
    {
        Stream? stream = null;
        string? fileName = null;
        if (imageFile is { Length: > 0 })
        {
            stream = imageFile.OpenReadStream();
            fileName = imageFile.FileName;
        }

        try
        {
            var updated = await adminProductService.UpdateProductAsync(productId, request, stream, fileName, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        finally
        {
            if (stream is not null) await stream.DisposeAsync();
        }
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

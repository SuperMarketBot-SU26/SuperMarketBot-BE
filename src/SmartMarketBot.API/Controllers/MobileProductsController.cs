using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Products;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class MobileProductsController(IProductService productService) : ControllerBase
{
    /// <summary>
    /// API Mobile — Tìm kiếm sản phẩm kèm vị trí kệ hàng trên bản đồ.
    /// Trả về semanticObjectId để Mobile App có thể gọi tiếp Navigation API.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<MobileProductSearchResultDto>>> SearchProducts(
        [FromQuery] string keyword,
        [FromQuery] int? categoryId,
        [FromQuery] int? productTypeId,
        [FromQuery] int? floorId,
        [FromQuery] bool? availableOnly,
        CancellationToken cancellationToken)
    {
        var results = await productService.SearchProductsWithLocationAsync(
            keyword,
            categoryId,
            productTypeId,
            floorId,
            availableOnly,
            cancellationToken);

        return Ok(results);
    }
}

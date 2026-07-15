using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class MobileProductsController(
    IProductService productService,
    IGeneralDealService generalDealService) : ControllerBase
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

    /// <summary>
    /// ⚡ General Deals — dành cho Guest (chưa đăng nhập) và Member đã đăng nhập.
    /// Trả về tất cả sản phẩm đang giảm giá trên toàn siêu thị.
    /// Nguồn: Product.PromotionPrice + SponsoredProducts trong AdCampaign Active.
    /// </summary>
    [HttpGet("deals")]
    [AllowAnonymous]
    public async Task<ActionResult<GeneralDealsResponseDto>> GetGeneralDeals(
        [FromQuery] int? productTypeId,
        [FromQuery] int? categoryId,
        [FromQuery] int? minDiscountPercent,
        [FromQuery] int? memberId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new GeneralDealsFilterDto(
            productTypeId,
            categoryId,
            minDiscountPercent,
            pageNumber,
            pageSize);

        var result = await generalDealService.GetDealsAsync(filter, memberId, cancellationToken);
        return Ok(result);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Products;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(
        [FromQuery] string? keyword,
        [FromQuery] int? categoryId,
        [FromQuery] int? subcategoryId,
        [FromQuery] int? productTypeId,
        [FromQuery] int[]? healthTagIds,
        [FromQuery] bool? availableOnly,
        CancellationToken cancellationToken)
    {
        var products = await productService.SearchProductsAsync(
            keyword,
            categoryId,
            subcategoryId,
            productTypeId,
            healthTagIds?.ToList(),
            availableOnly,
            cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("{id:int}/detail")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDetailDto>> GetProductDetail(
        int id,
        [FromQuery] int? memberId,
        CancellationToken cancellationToken)
    {
        var product = await productService.GetProductDetailByIdAsync(id, memberId, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>
    /// Flow 4 — Lấy danh sách sản phẩm thay thế an toàn cho hội viên.
    /// Lọc theo: cùng ProductType, không chứa thành phần dị ứng của member, phân khúc giá tương đương.
    /// </summary>
    [HttpGet("{id:int}/alternatives")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAlternatives(
        int id,
        [FromQuery] int? memberId,
        CancellationToken cancellationToken)
    {
        var alternatives = await productService.GetAlternativeProductsAsync(id, memberId, cancellationToken);
        return Ok(alternatives);
    }

    /// <summary>Trả về các sản phẩm chưa được gán vào SemanticObject (kệ hàng) nào trên map.</summary>
    [HttpGet("unmapped")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetUnmappedProducts(CancellationToken cancellationToken)
    {
        var products = await productService.GetUnmappedProductsAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var result = await productService.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("subcategories")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<SubcategoryDto>>> GetSubcategories(CancellationToken cancellationToken)
    {
        var result = await productService.GetSubcategoriesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("product-types")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductTypeDto>>> GetProductTypes(CancellationToken cancellationToken)
    {
        var result = await productService.GetProductTypesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("health-tags")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<HealthTagDto>>> GetHealthTags(CancellationToken cancellationToken)
    {
        var result = await productService.GetHealthTagsAsync(cancellationToken);
        return Ok(result);
    }
}

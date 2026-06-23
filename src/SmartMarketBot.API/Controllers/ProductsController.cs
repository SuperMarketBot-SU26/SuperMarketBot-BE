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
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await productService.GetProductsAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductByIdAsync(id, cancellationToken);
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
}

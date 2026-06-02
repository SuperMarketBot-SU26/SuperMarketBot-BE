using SmartMarketBot.Application.Models.Products;

namespace SmartMarketBot.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flow 4 — Trả về sản phẩm thay thế an toàn: cùng ProductType,
    /// không chứa thành phần dị ứng của member (nếu memberId != null), phân khúc giá tương đương.
    /// </summary>
    Task<IReadOnlyList<ProductDto>> GetAlternativeProductsAsync(int productId, int? memberId, CancellationToken cancellationToken = default);
}

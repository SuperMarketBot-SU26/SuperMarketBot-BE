using SmartMarketBot.Application.Models.Products;

namespace SmartMarketBot.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> SearchProductsAsync(
        string? keyword,
        int? categoryId,
        int? subcategoryId,
        int? productTypeId,
        IReadOnlyList<int>? healthTagIds,
        bool? availableOnly,
        CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductDetailByIdAsync(int productId, int? memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flow 4 — Trả về sản phẩm thay thế an toàn: cùng ProductType,
    /// không chứa thành phần dị ứng của member (nếu memberId != null), phân khúc giá tương đương.
    /// </summary>
    Task<IReadOnlyList<ProductDto>> GetAlternativeProductsAsync(int productId, int? memberId, CancellationToken cancellationToken = default);

    /// <summary>Trả về các sản phẩm chưa được gán (assign) vào bất kỳ SemanticObject nào trên map.</summary>
    Task<IReadOnlyList<ProductDto>> GetUnmappedProductsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubcategoryDto>> GetSubcategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductTypeDto>> GetProductTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HealthTagDto>> GetHealthTagsAsync(CancellationToken cancellationToken = default);
}

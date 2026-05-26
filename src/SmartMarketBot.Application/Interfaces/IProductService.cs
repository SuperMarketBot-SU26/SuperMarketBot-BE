using SmartMarketBot.Application.Models.Products;

namespace SmartMarketBot.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByIdAsync(int productId, CancellationToken cancellationToken = default);
}

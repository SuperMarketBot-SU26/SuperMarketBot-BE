using SmartMarketBot.Application.Models.Products;

namespace SmartMarketBot.Application.Interfaces;

public interface IAdminProductService
{
    Task<ProductDto> CreateProductAsync(CreateProductRequestDto request, Stream? imageStream, string? imageFileName, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateProductAsync(int productId, UpdateProductRequestDto request, Stream? imageStream, string? imageFileName, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateProductStatusAsync(int productId, UpdateProductStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default);
}

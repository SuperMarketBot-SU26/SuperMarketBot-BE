using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BrandDto?> GetByIdAsync(int brandId, CancellationToken cancellationToken = default);
    Task<BrandDto> CreateAsync(CreateBrandRequestDto request, CancellationToken cancellationToken = default);
    Task<BrandDto> UpdateAsync(int brandId, UpdateBrandRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int brandId, CancellationToken cancellationToken = default);
    Task<TopUpWalletResponseDto> TopUpWalletAsync(int brandId, TopUpWalletRequestDto request, CancellationToken cancellationToken = default);
}

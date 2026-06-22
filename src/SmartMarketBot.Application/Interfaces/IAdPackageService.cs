using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

public interface IAdPackageService
{
    Task<IReadOnlyList<AdPackageDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AdPackageDto?> GetByIdAsync(int packageId, CancellationToken cancellationToken = default);
    Task<AdPackageDto> CreateAsync(CreateAdPackageRequestDto request, CancellationToken cancellationToken = default);
    Task<AdPackageDto> UpdateAsync(int packageId, UpdateAdPackageRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int packageId, CancellationToken cancellationToken = default);
}

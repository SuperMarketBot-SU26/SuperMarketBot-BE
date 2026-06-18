using SmartMarketBot.Application.Models.Admin;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Flow 6 - Admin: CRUD AdPackage (gói quảng cáo).</summary>
public interface IAdminAdPackageService
{
    Task<IReadOnlyList<AdPackageDto>> GetAllAsync(CancellationToken ct = default);
    Task<AdPackageDto?> GetByIdAsync(int packageId, CancellationToken ct = default);
    Task<AdPackageDto> CreateAsync(CreateAdPackageRequestDto request, CancellationToken ct = default);
    Task<AdPackageDto> UpdateAsync(int packageId, UpdateAdPackageRequestDto request, CancellationToken ct = default);
    Task DeleteAsync(int packageId, CancellationToken ct = default);
}
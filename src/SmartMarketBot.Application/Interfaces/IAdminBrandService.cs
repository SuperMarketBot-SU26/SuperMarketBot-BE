using SmartMarketBot.Application.Models.Admin;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Flow 6 - Admin: CRUD Brand + nạp ví.</summary>
public interface IAdminBrandService
{
    Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken ct = default);
    Task<BrandDto?> GetByIdAsync(int brandId, CancellationToken ct = default);
    Task<BrandDto> CreateAsync(CreateBrandRequestDto request, CancellationToken ct = default);
    Task<BrandDto> UpdateAsync(int brandId, UpdateBrandRequestDto request, CancellationToken ct = default);
    Task DeleteAsync(int brandId, CancellationToken ct = default);
}
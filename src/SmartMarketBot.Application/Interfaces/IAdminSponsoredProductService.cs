using SmartMarketBot.Application.Models.Admin;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Flow 6 - Admin: CRUD SponsoredProduct (mapping AdCampaign ↔ Product).</summary>
public interface IAdminSponsoredProductService
{
    Task<IReadOnlyList<SponsoredProductDto>> GetAllAsync(int? campaignId, int? productId, CancellationToken ct = default);
    Task<SponsoredProductDto?> GetByIdAsync(int sponsoredId, CancellationToken ct = default);
    Task<SponsoredProductDto> CreateAsync(CreateSponsoredProductRequestDto request, CancellationToken ct = default);
    Task<SponsoredProductDto> UpdateAsync(int sponsoredId, UpdateSponsoredProductRequestDto request, CancellationToken ct = default);
    Task DeleteAsync(int sponsoredId, CancellationToken ct = default);
}
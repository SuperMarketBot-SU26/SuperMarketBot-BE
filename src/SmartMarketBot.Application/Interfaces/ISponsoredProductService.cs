using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

public interface ISponsoredProductService
{
    Task<IReadOnlyList<SponsoredProductDto>> GetByCampaignIdAsync(int campaignId, CancellationToken cancellationToken = default);
    Task<SponsoredProductDto?> GetByIdAsync(int sponsoredId, CancellationToken cancellationToken = default);
    Task<SponsoredProductDto> CreateAsync(AddSponsoredProductRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SponsoredProductDto>> BulkCreateAsync(BulkAddSponsoredProductRequestDto request, CancellationToken cancellationToken = default);
    Task<SponsoredProductDto> UpdatePriorityAsync(int sponsoredId, UpdateSponsoredProductPriorityDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int sponsoredId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int sponsoredId, string status, CancellationToken cancellationToken = default);
}

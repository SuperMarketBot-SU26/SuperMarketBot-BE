using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

public interface IAdResourceService
{
    Task<PaginatedResponse<AdResourceDto>> GetByCampaignAsync(int campaignId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<AdResourceDto?> GetByIdAsync(int resourceId, CancellationToken cancellationToken = default);
    Task<AdResourceDto> CreateAsync(CreateAdResourceRequestDto request, CancellationToken cancellationToken = default);
    Task<AdResourceDto> UpdateStatusAsync(int resourceId, string status, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int resourceId, CancellationToken cancellationToken = default);
}

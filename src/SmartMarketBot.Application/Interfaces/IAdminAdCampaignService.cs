using SmartMarketBot.Application.Models.Admin;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Flow 6 - Admin: CRUD AdCampaign.</summary>
public interface IAdminAdCampaignService
{
    Task<IReadOnlyList<AdCampaignDto>> GetAllAsync(int? brandId, CancellationToken ct = default);
    Task<AdCampaignDto?> GetByIdAsync(int campaignId, CancellationToken ct = default);
    Task<AdCampaignDto> CreateAsync(CreateAdCampaignRequestDto request, CancellationToken ct = default);
    Task<AdCampaignDto> UpdateAsync(int campaignId, UpdateAdCampaignRequestDto request, CancellationToken ct = default);
    Task DeleteAsync(int campaignId, CancellationToken ct = default);
}
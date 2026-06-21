using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

public interface IAdCampaignService
{
    Task<PaginatedResponse<CampaignResponseDto>> GetListAsync(CampaignListRequestDto request, CancellationToken cancellationToken = default);
    Task<CampaignResponseDto?> GetByIdAsync(int campaignId, CancellationToken cancellationToken = default);

    Task<CampaignResponseDto> CreateAsync(CreateCampaignRequestDto request, CancellationToken cancellationToken = default);
    Task<CampaignResponseDto> CreateWithProductsAsync(CreateCampaignWithProductsRequestDto request, CancellationToken cancellationToken = default);

    Task<CampaignResponseDto> UpdateAsync(int campaignId, UpdateCampaignRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int campaignId, CancellationToken cancellationToken = default);

    Task<ActivateCampaignResponseDto> ActivateAsync(int campaignId, CancellationToken cancellationToken = default);
    Task<PauseCampaignResponseDto> PauseAsync(int campaignId, string reason, CancellationToken cancellationToken = default);
    Task<CancelCampaignResponseDto> CancelAsync(int campaignId, CancellationToken cancellationToken = default);

    Task<SessionBindResponseDto> BindSessionAsync(SessionBindRequestDto request, CancellationToken cancellationToken = default);

    Task ProcessExpiredCampaignsAsync(CancellationToken cancellationToken = default);
    Task ProcessWalletLowBalanceAsync(int brandId, CancellationToken cancellationToken = default);
    Task ProcessOutOfStockAsync(int campaignId, CancellationToken cancellationToken = default);

    Task<RobotPlaylistResponseDto> GetRobotPlaylistAsync(int robotId, CancellationToken cancellationToken = default);
    Task<LogInteractionResponseDto> LogInteractionAsync(LogInteractionRequestDto request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<AdCampaignLogDto>> GetCampaignLogsAsync(int campaignId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

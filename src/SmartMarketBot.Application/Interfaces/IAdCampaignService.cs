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

    Task<RobotPlaylistResponseDto> GetRobotPlaylistAsync(int robotId, int? semanticObjectId, CancellationToken cancellationToken = default);
    Task<ZonePlaylistResponseDto> GetZonePlaylistAsync(int robotId, int zoneId, CancellationToken cancellationToken = default);
    Task<AutonomousRouteDto?> GetAutonomousRouteAsync(int robotId, CancellationToken cancellationToken = default);
    Task<RobotPlaylistResponseDto> GetPlaylistForNodeAsync(int robotId, int nodeId, CancellationToken cancellationToken = default);
    Task<LogInteractionResponseDto> LogInteractionAsync(LogInteractionRequestDto request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<AdCampaignLogDto>> GetCampaignLogsAsync(int campaignId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gán danh sách RobotRoute vào campaign. Charge thêm PriceRoute cho mỗi route mới.
    /// Chỉ thao tác được khi campaign đang Inactive / Paused và đã mua package.
    /// </summary>
    Task<CampaignRoutesResponseDto> AssignRoutesAsync(int campaignId, AssignCampaignRoutesRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách route mà campaign đã mua quyền phát.
    /// </summary>
    Task<CampaignRoutesResponseDto> GetAssignedRoutesAsync(int campaignId, CancellationToken cancellationToken = default);
}

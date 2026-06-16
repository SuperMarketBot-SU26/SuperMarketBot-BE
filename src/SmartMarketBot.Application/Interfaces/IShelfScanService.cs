using SmartMarketBot.Application.Models.AisleScans;

namespace SmartMarketBot.Application.Interfaces;

public interface IAisleScanService
{
    Task<IReadOnlyList<ShelfScanDto>> GetRecentScansAsync(int take = 20, CancellationToken cancellationToken = default);
    Task<ShelfScanDto> CreateScanAsync(CreateAisleScanRequestDto request, CancellationToken cancellationToken = default);
}

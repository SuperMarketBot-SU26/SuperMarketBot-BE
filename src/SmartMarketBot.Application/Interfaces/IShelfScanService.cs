using SmartMarketBot.Application.Models.ShelfScans;

namespace SmartMarketBot.Application.Interfaces;

public interface IShelfScanService
{
    Task<IReadOnlyList<ShelfScanDto>> GetRecentScansAsync(int take = 20, CancellationToken cancellationToken = default);
    Task<ShelfScanDto> CreateScanAsync(CreateShelfScanRequestDto request, CancellationToken cancellationToken = default);
}

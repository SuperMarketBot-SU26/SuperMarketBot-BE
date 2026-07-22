using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Application.Models.SemanticObjects;

namespace SmartMarketBot.Application.Interfaces;

public interface ISemanticObjectService
{
    Task<SemanticObjectListResponseDto> GetAllAsync(int? mapId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<SemanticObjectDto?> GetByIdAsync(int objectId, CancellationToken cancellationToken = default);
    Task<SemanticObjectDto> AssignProductTypeAsync(int objectId, int productTypeId, CancellationToken cancellationToken = default);
    Task<SemanticObjectDto?> UnassignProductTypeAsync(int objectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trả về toàn bộ SemanticObject có ObjectType == "shelf" trong 1 map, kèm
    /// thông tin ProductType. Không phân trang — dùng cho single-fetch UI
    /// (TargetingSelector) thay vì bắt FE gọi GetAllAsync với pageSize lớn.
    /// </summary>
    Task<IReadOnlyList<ShelfSummaryDto>> GetShelvesByMapAsync(int mapId, CancellationToken cancellationToken = default);
}

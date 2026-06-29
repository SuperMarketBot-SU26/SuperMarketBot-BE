using SmartMarketBot.Application.Models.SemanticObjects;

namespace SmartMarketBot.Application.Interfaces;

public interface ISemanticObjectService
{
    Task<SemanticObjectListResponseDto> GetAllAsync(int? mapId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<SemanticObjectDto?> GetByIdAsync(int objectId, CancellationToken cancellationToken = default);
    Task<SemanticObjectDto> AssignProductTypeAsync(int objectId, int productTypeId, CancellationToken cancellationToken = default);
    Task<SemanticObjectDto?> UnassignProductTypeAsync(int objectId, CancellationToken cancellationToken = default);
}

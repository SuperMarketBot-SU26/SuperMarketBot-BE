using SmartMarketBot.Application.Models.SemanticObjects;

namespace SmartMarketBot.Application.Interfaces;

public interface ISemanticObjectService
{
    Task<SemanticObjectListResponseDto> GetAllAsync(int? mapId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<SemanticObjectDto?> GetByIdAsync(int objectId, CancellationToken cancellationToken = default);
    Task<SemanticObjectDto> AssignProductAsync(int objectId, int productId, CancellationToken cancellationToken = default);
    Task<SemanticObjectDto?> UnassignProductAsync(int objectId, CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.SemanticObjects;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class SemanticObjectService(
    AppDbContext db,
    ILocalizationService localizer) : ISemanticObjectService
{
    public async Task<SemanticObjectListResponseDto> GetAllAsync(
        int? mapId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.SemanticObjects.AsNoTracking().AsQueryable();

        if (mapId.HasValue)
            query = query.Where(s => s.MapId == mapId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderBy(s => s.ObjectId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SemanticObjectDto(
                s.ObjectId,
                s.MapId,
                s.ObjectType,
                s.XMin, s.YMin, s.XMax, s.YMax,
                s.Label,
                s.Confidence,
                s.DetectedAt,
                s.ImageUrl,
                s.ProductId,
                s.Product != null ? s.Product.ProductName : null,
                s.Product != null ? s.Product.UnitPrice : null,
                s.Product != null ? s.Product.ImageUrl : null))
            .ToListAsync(cancellationToken);

        return new SemanticObjectListResponseDto(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<SemanticObjectDto?> GetByIdAsync(int objectId, CancellationToken cancellationToken = default)
    {
        return await db.SemanticObjects
            .AsNoTracking()
            .Where(s => s.ObjectId == objectId)
            .Select(s => new SemanticObjectDto(
                s.ObjectId,
                s.MapId,
                s.ObjectType,
                s.XMin, s.YMin, s.XMax, s.YMax,
                s.Label,
                s.Confidence,
                s.DetectedAt,
                s.ImageUrl,
                s.ProductId,
                s.Product != null ? s.Product.ProductName : null,
                s.Product != null ? s.Product.UnitPrice : null,
                s.Product != null ? s.Product.ImageUrl : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SemanticObjectDto> AssignProductAsync(
        int objectId, int productId, CancellationToken cancellationToken = default)
    {
        var semanticObj = await db.SemanticObjects.FindAsync([objectId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("SemanticObjectNotFound", objectId));

        var product = await db.Products.FindAsync([productId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("ProductNotFound", productId));

        semanticObj.ProductId = productId;
        await db.SaveChangesAsync(cancellationToken);

        return new SemanticObjectDto(
            semanticObj.ObjectId,
            semanticObj.MapId,
            semanticObj.ObjectType,
            semanticObj.XMin, semanticObj.YMin, semanticObj.XMax, semanticObj.YMax,
            semanticObj.Label,
            semanticObj.Confidence,
            semanticObj.DetectedAt,
            semanticObj.ImageUrl,
            product.ProductId,
            product.ProductName,
            product.UnitPrice,
            product.ImageUrl);
    }

    public async Task<SemanticObjectDto?> UnassignProductAsync(int objectId, CancellationToken cancellationToken = default)
    {
        var semanticObj = await db.SemanticObjects.FindAsync([objectId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("SemanticObjectNotFound", objectId));

        semanticObj.ProductId = null;
        await db.SaveChangesAsync(cancellationToken);

        return new SemanticObjectDto(
            semanticObj.ObjectId,
            semanticObj.MapId,
            semanticObj.ObjectType,
            semanticObj.XMin, semanticObj.YMin, semanticObj.XMax, semanticObj.YMax,
            semanticObj.Label,
            semanticObj.Confidence,
            semanticObj.DetectedAt,
            semanticObj.ImageUrl,
            null, null, null, null);
    }
}

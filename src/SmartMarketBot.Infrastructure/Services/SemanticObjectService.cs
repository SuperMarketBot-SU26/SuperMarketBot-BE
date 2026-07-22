using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
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
        var query = db.SemanticObjects
            .AsNoTracking()
            .Include(s => s.ProductType)
                .ThenInclude(pt => pt!.Subcategory)
                    .ThenInclude(sc => sc!.Category)
            .AsQueryable();

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
                s.ProductTypeId,
                s.ProductType != null ? s.ProductType.TypeName : null,
                s.ProductType != null ? s.ProductType.Subcategory != null ? s.ProductType.Subcategory.SubcategoryName : null : null,
                s.ProductType != null ? s.ProductType.Subcategory != null ? s.ProductType.Subcategory.Category != null ? s.ProductType.Subcategory.Category.CategoryName : null : null : null))
            .ToListAsync(cancellationToken);

        return new SemanticObjectListResponseDto(items, totalCount, pageNumber, pageSize, totalPages);
    }

    public async Task<SemanticObjectDto?> GetByIdAsync(int objectId, CancellationToken cancellationToken = default)
    {
        return await db.SemanticObjects
            .AsNoTracking()
            .Include(s => s.ProductType)
                .ThenInclude(pt => pt!.Subcategory)
                    .ThenInclude(sc => sc!.Category)
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
                s.ProductTypeId,
                s.ProductType != null ? s.ProductType.TypeName : null,
                s.ProductType != null ? s.ProductType.Subcategory != null ? s.ProductType.Subcategory.SubcategoryName : null : null,
                s.ProductType != null ? s.ProductType.Subcategory != null ? s.ProductType.Subcategory.Category != null ? s.ProductType.Subcategory.Category.CategoryName : null : null : null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SemanticObjectDto> AssignProductTypeAsync(
        int objectId, int productTypeId, CancellationToken cancellationToken = default)
    {
        var semanticObj = await db.SemanticObjects.FindAsync([objectId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("SemanticObjectNotFound", objectId));

        var productType = await db.ProductTypes
            .Include(pt => pt.Subcategory)
                .ThenInclude(sc => sc!.Category)
            .FirstOrDefaultAsync(pt => pt.ProductTypeId == productTypeId, cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("ProductTypeNotFound", productTypeId));

        semanticObj.ProductTypeId = productTypeId;
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
            productType.ProductTypeId,
            productType.TypeName,
            productType.Subcategory?.SubcategoryName,
            productType.Subcategory?.Category?.CategoryName);
    }

    public async Task<SemanticObjectDto?> UnassignProductTypeAsync(int objectId, CancellationToken cancellationToken = default)
    {
        var semanticObj = await db.SemanticObjects.FindAsync([objectId], cancellationToken)
            ?? throw new KeyNotFoundException(localizer.Get("SemanticObjectNotFound", objectId));

        semanticObj.ProductTypeId = null;
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

    public async Task<IReadOnlyList<ShelfSummaryDto>> GetShelvesByMapAsync(
        int mapId, CancellationToken cancellationToken = default)
    {
        return await db.SemanticObjects
            .AsNoTracking()
            .Include(s => s.ProductType)
            .Where(s => s.MapId == mapId && s.ObjectType == "shelf")
            .OrderBy(s => s.ObjectId)
            .Select(s => new ShelfSummaryDto(
                s.ObjectId,
                s.Label,
                s.ObjectType,
                s.ProductTypeId,
                s.ProductType != null ? s.ProductType.TypeName : null))
            .ToListAsync(cancellationToken);
    }
}

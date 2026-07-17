using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.MealSuggestions;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>Flow 2 — Smart Menu Assistant.</summary>
public sealed class MealSuggestionService(AppDbContext db) : IMealSuggestionService
{
    public async Task<IReadOnlyList<RecipeDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.MealSuggestions
            .AsNoTracking()
            .OrderBy(r => r.MealName)
            .Select(r => new RecipeDto(
                r.MealSuggestionId,
                r.MealName,
                r.Description,
                r.YieldPortions,
                r.ImageUrl,
                r.Calories,
                r.HealthyScore,
                r.AlternativeSuggestion))
            .ToListAsync(ct);
    }

    public async Task<RecipeDto?> GetByIdAsync(int recipeId, CancellationToken ct = default)
    {
        return await db.MealSuggestions
            .AsNoTracking()
            .Where(r => r.MealSuggestionId == recipeId)
            .Select(r => new RecipeDto(
                r.MealSuggestionId,
                r.MealName,
                r.Description,
                r.YieldPortions,
                r.ImageUrl,
                r.Calories,
                r.HealthyScore,
                r.AlternativeSuggestion))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<MenuAssistantResponseDto?> GetMenuAssistantAsync(
        int recipeId, int portions, CancellationToken ct = default)
    {
        var mealSuggestion = await db.MealSuggestions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.MealSuggestionId == recipeId, ct);

        if (mealSuggestion is null) return null;

        // Lấy nguyên liệu và tính định lượng theo số phần ăn
        var items = await db.MealItems
            .AsNoTracking()
            .Where(ri => ri.MealSuggestionId == recipeId)
            .Join(db.Products, ri => ri.ProductId, p => p.ProductId,
                (ri, p) => new { ri, p })
            .ToListAsync(ct);

        // Tìm NodeID cho từng sản phẩm qua Slots → Shelves → Aisles → NavigationNodes
        var productIds = items.Select(x => x.p.ProductId).ToList();
        var productNodeMap = await db.Slots
            .AsNoTracking()
            .Where(s => s.ProductSlots.Any(ps => productIds.Contains(ps.ProductId)))
            .Include(s => s.ProductSlots)
            .Join(db.Shelves, s => s.ShelfId, sl => sl.ShelfId, (s, sl) => new { s, sl })
            .Join(db.NavigationNodes, x => x.sl.AisleId, n => n.NodeId,
                  (x, n) => new { x.s, n.NodeId, n.XCoord, n.YCoord, x.sl.AisleId })
            .ToListAsync(ct);

        var productNodeDict = productNodeMap
            .GroupBy(x => x.s.ProductSlots.FirstOrDefault(ps => productIds.Contains(ps.ProductId))?.ProductId ?? 0)
            .Where(g => g.Key != 0)
            .ToDictionary(g => g.Key, g => g.First());

        decimal portionMultiplier = mealSuggestion.YieldPortions > 0 ? (decimal)portions / mealSuggestion.YieldPortions : portions;
        decimal totalCost = 0;
        var ingredients = new List<RecipeIngredientDto>();

        foreach (var item in items)
        {
            var adjustedQty = item.ri.QuantityRequired * portionMultiplier;
            totalCost += item.p.UnitPrice * (decimal)adjustedQty;
            productNodeDict.TryGetValue(item.p.ProductId, out var pn);

            var nodeId = pn?.NodeId;
            var nodeName = pn != null ? $"Node {pn.NodeId}" : null;

            ingredients.Add(new RecipeIngredientDto(
                item.p.ProductId,
                item.p.ProductName,
                item.p.UnitPrice,
                item.p.ImageUrl,
                adjustedQty,
                item.ri.UnitOfMeasure,
                pn != null,
                pn?.s.Quantity ?? 0,
                nodeId,
                nodeName));
        }

        // Lộ trình tối ưu gom hàng (NodeId theo thứ tự xuất hiện trong Nearest Neighbour đơn giản)
        var routeNodeIds = ingredients
            .Where(i => i.LocationNodeId.HasValue)
            .Select(i => i.LocationNodeId!.Value)
            .Distinct()
            .ToList();

        return new MenuAssistantResponseDto(
            mealSuggestion.MealSuggestionId,
            mealSuggestion.MealName,
            portions,
            mealSuggestion.Calories.HasValue ? mealSuggestion.Calories * portions : null,
            mealSuggestion.HealthyScore,
            mealSuggestion.AlternativeSuggestion,
            Math.Round(totalCost, 0),
            ingredients,
            routeNodeIds);
    }
}

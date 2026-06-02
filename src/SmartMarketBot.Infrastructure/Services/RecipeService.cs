using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Recipes;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>Flow 2 — Smart Menu Assistant.</summary>
public sealed class RecipeService(AppDbContext db) : IRecipeService
{
    public async Task<IReadOnlyList<RecipeDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Recipes
            .AsNoTracking()
            .OrderBy(r => r.RecipeName)
            .Select(r => new RecipeDto(
                r.RecipeID,
                r.RecipeName,
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
        return await db.Recipes
            .AsNoTracking()
            .Where(r => r.RecipeID == recipeId)
            .Select(r => new RecipeDto(
                r.RecipeID,
                r.RecipeName,
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
        var recipe = await db.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RecipeID == recipeId, ct);

        if (recipe is null) return null;

        // Lấy nguyên liệu và tính định lượng theo số phần ăn
        var items = await db.RecipeItems
            .AsNoTracking()
            .Where(ri => ri.RecipeID == recipeId)
            .Join(db.Products, ri => ri.ProductID, p => p.ProductID,
                (ri, p) => new { ri, p })
            .ToListAsync(ct);

        // Tìm NodeID cho từng sản phẩm qua Slots → ShelfLevels → Aisles → NavigationNodes
        var productIds = items.Select(x => x.p.ProductID).ToList();
        var productNodeMap = await db.Slots
            .AsNoTracking()
            .Where(s => productIds.Contains(s.ProductID ?? 0) && s.ProductID != null)
            .Join(db.ShelfLevels, s => s.ShelfLevelID, sl => sl.ShelfLevelID, (s, sl) => new { s, sl })
            .Join(db.NavigationNodes, x => x.sl.AisleID, n => n.LinkedAisleID,
                  (x, n) => new { x.s.ProductID, n.NodeID, x.s.Quantity, AisleCode = n.NodeName })
            .GroupBy(x => x.ProductID)
            .Select(g => g.First())
            .ToDictionaryAsync(x => x.ProductID!.Value, ct);

        decimal portionMultiplier = portions;
        decimal totalCost = 0;
        var ingredients = new List<RecipeIngredientDto>();

        foreach (var item in items)
        {
            var adjustedQty = item.ri.QuantityRequired * portionMultiplier;
            totalCost += item.p.UnitPrice * (decimal)adjustedQty;
            productNodeMap.TryGetValue(item.p.ProductID, out var pn);

            ingredients.Add(new RecipeIngredientDto(
                item.p.ProductID,
                item.p.ProductName,
                item.p.UnitPrice,
                item.p.ImageUrl,
                adjustedQty,
                item.ri.UnitOfMeasure,
                pn?.Quantity > 0,
                pn?.Quantity ?? 0,
                pn?.NodeID,
                pn is null ? null : $"Node {pn.NodeID} — {pn.AisleCode}"));
        }

        // Lộ trình tối ưu gom hàng (NodeId theo thứ tự xuất hiện trong Nearest Neighbour đơn giản)
        var routeNodeIds = ingredients
            .Where(i => i.LocationNodeId.HasValue)
            .Select(i => i.LocationNodeId!.Value)
            .Distinct()
            .ToList();

        return new MenuAssistantResponseDto(
            recipe.RecipeID,
            recipe.RecipeName,
            portions,
            recipe.Calories.HasValue ? recipe.Calories * portions : null,
            recipe.HealthyScore,
            recipe.AlternativeSuggestion,
            Math.Round(totalCost, 0),
            ingredients,
            routeNodeIds);
    }
}

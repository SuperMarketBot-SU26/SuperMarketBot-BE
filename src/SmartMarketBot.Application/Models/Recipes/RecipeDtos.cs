namespace SmartMarketBot.Application.Models.Recipes;

// ─── Flow 2: Smart Menu Assistant ────────────────────────────────────────────

public sealed record RecipeDto(
    int RecipeId,
    string RecipeName,
    string? Description,
    int YieldPortions,
    string? ImageUrl,
    int? Calories,
    int? HealthyScore,
    string? AlternativeSuggestion);

/// <summary>Một nguyên liệu trong công thức đã tính toán theo số phần ăn.</summary>
public sealed record RecipeIngredientDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    string? ImageUrl,
    decimal QuantityRequired,
    string UnitOfMeasure,
    bool InStock,
    int CurrentStock,
    int? LocationNodeId,
    string? ShelfLocation);

/// <summary>Response đầy đủ của Menu Assistant: nguyên liệu + lộ trình gom hàng.</summary>
public sealed record MenuAssistantResponseDto(
    int RecipeId,
    string RecipeName,
    int Portions,
    int? Calories,
    int? HealthyScore,
    string? AlternativeSuggestion,
    decimal EstimatedTotalCost,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    /// <summary>Danh sách NodeId được tối ưu hoá để robot/khách gom nguyên liệu (Dijkstra)</summary>
    IReadOnlyList<int> OptimizedShoppingRoute);

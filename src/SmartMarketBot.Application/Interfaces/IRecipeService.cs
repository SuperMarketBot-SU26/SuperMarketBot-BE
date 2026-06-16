using SmartMarketBot.Application.Models.MealSuggestions;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Flow 2 — Smart Menu Assistant: gợi ý nguyên liệu và lộ trình gom hàng.</summary>
public interface IRecipeService
{
    Task<IReadOnlyList<RecipeDto>> GetAllAsync(CancellationToken ct = default);

    Task<RecipeDto?> GetByIdAsync(int recipeId, CancellationToken ct = default);

    /// <summary>
    /// Tính toán định lượng nguyên liệu theo số phần ăn, kiểm tra tồn kho từng nguyên liệu
    /// và gợi ý lộ trình tối ưu gom hàng (danh sách NodeId Dijkstra).
    /// </summary>
    Task<MenuAssistantResponseDto?> GetMenuAssistantAsync(
        int recipeId,
        int portions,
        CancellationToken ct = default);
}

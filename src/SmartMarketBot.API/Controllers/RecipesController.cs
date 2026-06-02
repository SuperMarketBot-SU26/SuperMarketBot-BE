using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Recipes;

namespace SmartMarketBot.API.Controllers;

/// <summary>Flow 2 — Smart Menu Assistant: thực đơn dinh dưỡng + lộ trình gom nguyên liệu.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class RecipesController(IRecipeService recipeService) : ControllerBase
{
    /// <summary>Lấy danh sách tất cả công thức nấu ăn.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<RecipeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var recipes = await recipeService.GetAllAsync(cancellationToken);
        return Ok(recipes);
    }

    /// <summary>Lấy chi tiết một công thức.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<RecipeDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var recipe = await recipeService.GetByIdAsync(id, cancellationToken);
        return recipe is null ? NotFound() : Ok(recipe);
    }

    /// <summary>
    /// Smart Menu Assistant: tính định lượng nguyên liệu theo số phần ăn,
    /// kiểm tra tồn kho từng nguyên liệu và gợi ý lộ trình tối ưu gom hàng (Dijkstra NodeIds).
    /// </summary>
    [HttpGet("menu-assistant")]
    [AllowAnonymous]
    public async Task<ActionResult<MenuAssistantResponseDto>> GetMenuAssistant(
        [FromQuery] int recipeId,
        [FromQuery] int portions = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await recipeService.GetMenuAssistantAsync(recipeId, portions, cancellationToken);
        return result is null ? NotFound($"Recipe {recipeId} not found.") : Ok(result);
    }
}

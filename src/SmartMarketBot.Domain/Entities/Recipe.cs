namespace SmartMarketBot.Domain.Entities;

public class Recipe
{
    public int RecipeID { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int YieldPortions { get; set; } = 1;
    public string? ImageUrl { get; set; }

    /// <summary>Lượng calo ước tính toàn công thức (Buổi 13)</summary>
    public int? Calories { get; set; }

    /// <summary>Điểm lành mạnh 1-100 (Buổi 13)</summary>
    public int? HealthyScore { get; set; }

    /// <summary>Đề xuất thay thế lành mạnh hơn (Buổi 13)</summary>
    public string? AlternativeSuggestion { get; set; }

    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}

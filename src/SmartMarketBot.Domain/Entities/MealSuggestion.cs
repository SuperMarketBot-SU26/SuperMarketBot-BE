namespace SmartMarketBot.Domain.Entities;

public class MealSuggestion
{
    public int MealSuggestionId { get; set; }
    public string MealName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int YieldPortions { get; set; } = 1;
    public string? ImageUrl { get; set; }
    public int? Calories { get; set; }
    public int? HealthyScore { get; set; }
    public string? AlternativeSuggestion { get; set; }

    public virtual ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();
}

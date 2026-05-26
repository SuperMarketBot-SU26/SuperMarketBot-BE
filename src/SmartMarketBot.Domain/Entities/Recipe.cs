namespace SmartMarketBot.Domain.Entities;

public class Recipe
{
    public int RecipeID { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int YieldPortions { get; set; } = 1;
    public string? ImageUrl { get; set; }

    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}

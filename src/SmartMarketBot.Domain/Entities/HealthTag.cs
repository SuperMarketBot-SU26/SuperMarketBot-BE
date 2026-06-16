namespace SmartMarketBot.Domain.Entities;

public class HealthTag
{
    public int HealthTagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string TagType { get; set; } = "diet";

    public virtual ICollection<MemberHealthPreference> MemberHealthPreferences { get; set; } = new List<MemberHealthPreference>();
    public virtual ICollection<ProductHealthTag> ProductHealthTags { get; set; } = new List<ProductHealthTag>();
}

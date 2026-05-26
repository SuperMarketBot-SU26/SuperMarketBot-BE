namespace SmartMarketBot.Domain.Entities;

public class HealthTag
{
    public int TagID { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string TagType { get; set; } = string.Empty;

    public virtual ICollection<ProductHealthTag> ProductHealthTags { get; set; } = new List<ProductHealthTag>();
    public virtual ICollection<MemberHealthPreference> MemberHealthPreferences { get; set; } = new List<MemberHealthPreference>();
}

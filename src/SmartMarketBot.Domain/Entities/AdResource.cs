namespace SmartMarketBot.Domain.Entities;

public sealed class AdResource
{
    public int ResourceId { get; set; }
    public int AdCampaignId { get; set; }
    public string ResourceType { get; set; } = AdResourceType.Image;
    public string ResourceUrl { get; set; } = string.Empty;
    public string? ContentText { get; set; }
    public string? Resolution { get; set; }
    public string Status { get; set; } = AdResourceStatus.Active;

    public AdCampaign? AdCampaign { get; set; }
}

public static class AdResourceType
{
    public const string Image = "IMAGE";
    public const string Video = "VIDEO";
    public const string VoiceText = "VOICE_TEXT";
}

public static class AdResourceStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}

namespace SmartMarketBot.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SmartMarketBot";
    public string Audience { get; set; } = "SmartMarketBot.Client";
    public string SecretKey { get; set; } = "change_this_secret_key_very_long_1234567890";
    public int ExpiryMinutes { get; set; } = 120;
}

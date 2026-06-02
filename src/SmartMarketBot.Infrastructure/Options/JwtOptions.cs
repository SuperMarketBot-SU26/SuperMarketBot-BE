namespace SmartMarketBot.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SmartMarketBot";
    public string Audience { get; set; } = "SmartMarketBot.Client";
    public string SecretKey { get; set; } = "REPLACE_IN_AZURE_APP_SETTINGS_OR_ENV";

    /// <summary>Access token hết hạn sau N phút (mặc định 15 phút)</summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>Refresh token hết hạn sau N ngày (mặc định 7 ngày)</summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

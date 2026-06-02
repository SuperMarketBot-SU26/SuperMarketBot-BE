namespace SmartMarketBot.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "SmartMarketBot";
    public bool EnableSsl { get; set; } = true;

    /// <summary>OTP hết hạn sau N phút (mặc định 10)</summary>
    public int OtpExpiryMinutes { get; set; } = 10;

    /// <summary>Phải chờ ít nhất N giây trước khi resend OTP</summary>
    public int OtpResendCooldownSeconds { get; set; } = 60;
}

namespace SmartMarketBot.Infrastructure.Options;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string ClientId { get; set; } = "smartmarketbot-backend";

    /// <summary>true = kết nối TLS/SSL (HiveMQ Cloud / EMQX Cloud dùng port 8883)</summary>
    public bool UseTls { get; set; } = false;

    /// <summary>Bỏ qua xác minh certificate (chỉ dùng khi test / self-signed cert)</summary>
    public bool AllowUntrustedCertificates { get; set; } = false;
}

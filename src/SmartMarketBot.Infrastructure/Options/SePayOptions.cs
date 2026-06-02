namespace SmartMarketBot.Infrastructure.Options;

public sealed class SePayOptions
{
    public const string SectionName = "SePay";

    public bool Enabled { get; set; } = false;
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.sepay.vn";

    public BankInfo Bank { get; set; } = new();

    /// <summary>QR thanh toán hết hạn sau N phút</summary>
    public int PaymentExpiryMinutes { get; set; } = 15;

    public sealed class BankInfo
    {
        /// <summary>Mã ngân hàng (vd: "MB", "VCB")</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Số tài khoản nhận tiền</summary>
        public string AccountNumber { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;
    }
}

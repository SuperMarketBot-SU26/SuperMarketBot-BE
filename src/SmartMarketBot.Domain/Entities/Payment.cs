namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Giao dịch thanh toán SePay (chuyển khoản ngân hàng).
/// OrderCode = mã tham chiếu duy nhất gửi lên SePay.
/// </summary>
public class Payment
{
    public Guid PaymentId { get; set; } = Guid.NewGuid();
    public int UserId { get; set; }

    /// <summary>Mã đơn duy nhất — nhúng vào nội dung chuyển khoản (pattern SMB + timestamp)</summary>
    public string OrderCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";

    /// <summary>"Pending" | "Success" | "Failed" | "Cancelled"</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Mô tả đơn hàng (hiện trong nội dung CK)</summary>
    public string? Description { get; set; }

    /// <summary>Link QR/deeplink trả về từ SePay</summary>
    public string? QrCodeUrl { get; set; }

    /// <summary>SePay transaction ID nhận từ webhook</summary>
    public string? SepayTransactionId { get; set; }

    /// <summary>JSON raw từ webhook (log)</summary>
    public string? WebhookPayload { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    public virtual User User { get; set; } = null!;
}

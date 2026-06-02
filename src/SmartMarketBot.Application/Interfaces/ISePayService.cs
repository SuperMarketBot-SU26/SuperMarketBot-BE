using SmartMarketBot.Application.Models.Auth;

namespace SmartMarketBot.Application.Interfaces;

public interface ISePayService
{
    /// <summary>Tạo đơn thanh toán mới → trả về QR + mã chuyển khoản</summary>
    Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentDto request, int userId, CancellationToken ct = default);

    /// <summary>Kiểm tra trạng thái đơn theo OrderCode</summary>
    Task<PaymentStatusDto?> GetPaymentStatusAsync(string orderCode, int userId, CancellationToken ct = default);

    /// <summary>Xử lý IPN webhook từ SePay khi có tiền vào</summary>
    Task<(bool Success, string? Error)> ProcessWebhookAsync(SePayWebhookDto webhook, CancellationToken ct = default);
}

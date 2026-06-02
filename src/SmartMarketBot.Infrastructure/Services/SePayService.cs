using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Auth;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Options;
using SmartMarketBot.Infrastructure.Persistence;
using System.Text.Json;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class SePayService(
    AppDbContext db,
    IOptions<SePayOptions> sePayOptions,
    ILogger<SePayService> logger) : ISePayService
{
    private readonly SePayOptions _opts = sePayOptions.Value;

    public async Task<CreatePaymentResponseDto> CreatePaymentAsync(
        CreatePaymentDto request, int userId, CancellationToken ct = default)
    {
        var orderCode = $"SMB{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var expiresAt = DateTime.UtcNow.AddMinutes(_opts.PaymentExpiryMinutes);

        // Nội dung chuyển khoản nhúng orderCode — SePay dùng để match
        var content = $"{orderCode}";

        // QR Vietcombank/MB: dùng VietQR template (không cần call API SePay nếu chỉ QR static)
        var qrUrl = BuildVietQrUrl(_opts.Bank.Code, _opts.Bank.AccountNumber, request.Amount, content);

        var payment = new Payment
        {
            UserId = userId,
            OrderCode = orderCode,
            Amount = request.Amount,
            Description = request.Description,
            Status = "Pending",
            QrCodeUrl = qrUrl
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[SePay] Payment created: {OrderCode} {Amount}₫ UserId={UserId}",
            orderCode, request.Amount, userId);

        return new CreatePaymentResponseDto(
            PaymentId: payment.PaymentId,
            OrderCode: orderCode,
            Amount: request.Amount,
            Status: "Pending",
            QrCodeUrl: qrUrl,
            BankAccountNumber: _opts.Bank.AccountNumber,
            BankAccountName: _opts.Bank.AccountName,
            BankCode: _opts.Bank.Code,
            TransferContent: content,
            ExpiresAt: expiresAt);
    }

    public async Task<PaymentStatusDto?> GetPaymentStatusAsync(
        string orderCode, int userId, CancellationToken ct = default)
    {
        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.OrderCode == orderCode && p.UserId == userId, ct);

        if (payment is null) return null;

        return new PaymentStatusDto(
            payment.PaymentId,
            payment.OrderCode,
            payment.Status,
            payment.Amount,
            payment.CreatedAt,
            payment.PaidAt);
    }

    public async Task<(bool Success, string? Error)> ProcessWebhookAsync(
        SePayWebhookDto webhook, CancellationToken ct = default)
    {
        try
        {
            // SePay nhúng OrderCode vào Content hoặc ReferenceCode
            var content = webhook.Content ?? webhook.ReferenceCode ?? string.Empty;
            var payment = await db.Payments
                .FirstOrDefaultAsync(p => content.Contains(p.OrderCode) && p.Status == "Pending", ct);

            if (payment is null)
            {
                logger.LogWarning("[SePay] Webhook: no matching Pending payment for content '{Content}'", content);
                return (true, null); // trả 200 cho SePay (tránh retry vô ích)
            }

            if (webhook.TransferAmount < payment.Amount)
            {
                logger.LogWarning("[SePay] Webhook: amount mismatch — expected {Expected}, got {Got}",
                    payment.Amount, webhook.TransferAmount);
                return (false, "Amount mismatch");
            }

            payment.Status = "Success";
            payment.PaidAt = DateTime.UtcNow;
            payment.SepayTransactionId = webhook.Id.ToString();
            payment.WebhookPayload = JsonSerializer.Serialize(webhook);

            await db.SaveChangesAsync(ct);
            logger.LogInformation("[SePay] Payment confirmed: {OrderCode}", payment.OrderCode);
            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SePay] Webhook processing error");
            return (false, ex.Message);
        }
    }

    // VietQR deeplink — không cần API key, dùng được ngay
    private static string BuildVietQrUrl(string bankCode, string accountNumber,
        decimal amount, string addInfo)
    {
        var amountInt = (long)amount;
        return $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.png" +
               $"?amount={amountInt}&addInfo={Uri.EscapeDataString(addInfo)}&accountName=";
    }
}

namespace SmartMarketBot.Application.Models.Auth;

// ── SePay Request ───────────────────────────────────────────────────

public sealed record CreatePaymentDto(
    decimal Amount,
    string Description,
    string? ReturnUrl = null);

// ── SePay Response ──────────────────────────────────────────────────

public sealed record CreatePaymentResponseDto(
    Guid PaymentId,
    string OrderCode,
    decimal Amount,
    string Status,
    string? QrCodeUrl,
    string BankAccountNumber,
    string BankAccountName,
    string BankCode,
    string TransferContent,
    DateTime ExpiresAt);

public sealed record PaymentStatusDto(
    Guid PaymentId,
    string OrderCode,
    string Status,
    decimal Amount,
    DateTime CreatedAt,
    DateTime? PaidAt);

// ── SePay Webhook ───────────────────────────────────────────────────

public sealed record SePayWebhookDto(
    long Id,
    string Gateway,
    string TransactionDate,
    string AccountNumber,
    string? SubAccount,
    string Code,
    string Content,
    decimal TransferAmount,
    decimal? AccumulatedAmount,
    string ReferenceCode,
    string Description,
    decimal TransferType,
    string? TransactionContent,
    decimal? Paid);

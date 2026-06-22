using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure.Options;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class EmailService(
    IOptions<EmailOptions> emailOptions,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailOptions _opts = emailOptions.Value;

    public Task SendRegistrationOtpAsync(string toEmail, string fullName, string otpCode, CancellationToken ct = default)
    {
        var subject = "Xác minh tài khoản SmartMarketBot";
        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:auto">
              <h2 style="color:#2563eb">SmartMarketBot</h2>
              <p>Xin chào <strong>{HtmlEncode(fullName)}</strong>,</p>
              <p>Mã OTP xác minh tài khoản của bạn:</p>
              <div style="font-size:2rem;font-weight:bold;letter-spacing:.4em;
                          background:#f0f4ff;border-radius:8px;padding:16px 24px;
                          text-align:center;color:#1d4ed8">{otpCode}</div>
              <p style="color:#64748b;font-size:.85rem">
                Mã có hiệu lực trong <strong>{_opts.OtpExpiryMinutes} phút</strong>.
                Không chia sẻ mã này cho bất kỳ ai.
              </p>
            </div>
            """;
        return SendEmailAsync(toEmail, subject, body, ct);
    }

    public Task SendPasswordResetOtpAsync(string toEmail, string otpCode, CancellationToken ct = default)
    {
        var subject = "Đặt lại mật khẩu SmartMarketBot";
        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:auto">
              <h2 style="color:#dc2626">Đặt lại mật khẩu</h2>
              <p>Mã OTP để đặt lại mật khẩu của bạn:</p>
              <div style="font-size:2rem;font-weight:bold;letter-spacing:.4em;
                          background:#fff1f2;border-radius:8px;padding:16px 24px;
                          text-align:center;color:#b91c1c">{otpCode}</div>
              <p style="color:#64748b;font-size:.85rem">
                Mã có hiệu lực trong <strong>{_opts.OtpExpiryMinutes} phút</strong>.
                Nếu bạn không yêu cầu, hãy bỏ qua email này.
              </p>
            </div>
            """;
        return SendEmailAsync(toEmail, subject, body, ct);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.SmtpUser) || 
            _opts.SmtpUser.Contains("REPLACE_") || 
            string.IsNullOrWhiteSpace(_opts.FromEmail) || 
            _opts.FromEmail.Contains("REPLACE_"))
        {
            logger.LogWarning("[Email] SmtpUser hoặc FromEmail chưa cấu hình đúng. Bỏ qua gửi email tới {Email}. [DEBUG OTP] Nội dung Email: {Body}", toEmail, htmlBody);
            return;
        }

        using var client = new SmtpClient(_opts.SmtpHost, _opts.SmtpPort)
        {
            EnableSsl = _opts.EnableSsl,
            Credentials = new NetworkCredential(_opts.SmtpUser, _opts.SmtpPassword)
        };

        using var message = new MailMessage(
            from: new MailAddress(_opts.FromEmail, _opts.FromName),
            to: new MailAddress(toEmail))
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        try
        {
            await client.SendMailAsync(message, ct);
            logger.LogInformation("[Email] Sent '{Subject}' → {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Email] Failed to send '{Subject}' → {Email}", subject, toEmail);
            throw;
        }
    }

    private static string HtmlEncode(string s) =>
        System.Net.WebUtility.HtmlEncode(s);
}

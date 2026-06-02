using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Auth;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentController(
    ISePayService sePayService,
    IConfiguration configuration,
    ILogger<PaymentController> logger) : ControllerBase
{
    /// <summary>Tạo đơn thanh toán SePay (VietQR)</summary>
    [Authorize]
    [HttpPost("create")]
    [ProducesResponseType(typeof(CreatePaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreatePaymentResponseDto>> CreatePayment(
        [FromBody] CreatePaymentDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();
        var result = await sePayService.CreatePaymentAsync(request, userId.Value, ct);
        return Ok(result);
    }

    /// <summary>Kiểm tra trạng thái đơn theo OrderCode</summary>
    [Authorize]
    [HttpGet("status/{orderCode}")]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentStatusDto>> GetStatus(string orderCode, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();
        var dto = await sePayService.GetPaymentStatusAsync(orderCode, userId.Value, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// SePay IPN Webhook — gọi khi có tiền vào tài khoản.
    /// Xác thực bằng Authorization: Apikey {WebhookSecret}
    /// </summary>
    [AllowAnonymous]
    [HttpPost("sepay/webhook")]
    public async Task<IActionResult> SePayWebhook(
        [FromBody] SePayWebhookDto webhook, CancellationToken ct)
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var secret = configuration["SePay:WebhookSecret"]
                  ?? configuration["SePay:ApiKey"]
                  ?? string.Empty;

        var receivedKey = authHeader?.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase) == true
            ? authHeader["Apikey ".Length..].Trim()
            : null;

        if (string.IsNullOrEmpty(secret) || receivedKey != secret)
        {
            logger.LogWarning("[SePay] Webhook auth failed from {IP}", HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { success = false });
        }

        var (ok, error) = await sePayService.ProcessWebhookAsync(webhook, ct);
        return ok
            ? Ok(new { success = true })
            : BadRequest(new { success = false, reason = error });
    }

    private int? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var id) ? id : null;
    }
}

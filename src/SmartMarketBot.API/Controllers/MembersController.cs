using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Members;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// Flow 3 — Budget &amp; Health + Flow 2 Deal Hunter + Member Alerts + Profile.
/// Base route: api/members/...
/// </summary>
[ApiController]
[Route("api/members")]
public sealed class MembersController(IMemberService memberService) : ControllerBase
{
    // ── Profile ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy thông tin cá nhân (profile) của member đang đăng nhập.
    /// Yêu cầu: JWT Bearer token hợp lệ.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MemberProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberProfileDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.GetProfileAsync(accountId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân: FullName và/hoặc Phone.
    /// Yêu cầu: JWT Bearer token hợp lệ.
    /// </summary>
    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType(typeof(MemberProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberProfileDto>> UpdateMyProfile(
        [FromBody] UpdateProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.UpdateProfileAsync(accountId.Value, request, cancellationToken);
        return Ok(result);
    }

    // ── Flow 3: Budget & Health ─────────────────────────────────────────────

    /// <summary>
    /// Thiết lập ngân sách stop-loss cho phiên mua sắm.
    /// Robot/App lưu lại để kiểm tra khi quét từng sản phẩm.
    /// </summary>
    [HttpPost("{memberId:int}/shopping-session/budget")]
    [AllowAnonymous]
    public async Task<ActionResult<SetBudgetResponseDto>> SetBudget(
        int memberId,
        [FromBody] SetBudgetRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await memberService.SetShoppingBudgetAsync(memberId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Quét barcode sản phẩm vào giỏ hàng — kiểm tra đồng thời 3 cảnh báo:
    /// (1) Allergy — sản phẩm chứa thành phần dị ứng,
    /// (2) BudgetExceeded — vượt ngân sách đã cài,
    /// (3) DuplicatePurchase — đã mua trong vòng 7 ngày gần nhất.
    /// </summary>
    [HttpPost("{memberId:int}/shopping-session/scan-item")]
    [AllowAnonymous]
    public async Task<ActionResult<ScanItemResponseDto>> ScanItem(
        int memberId,
        [FromBody] ScanItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await memberService.ScanItemAsync(memberId, request, cancellationToken);
        return Ok(result);
    }

    // ── Flow 2: Deal Hunter ─────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách ưu đãi cá nhân hóa cho hội viên: dựa trên lịch sử mua sắm,
    /// chế độ ăn (SearchMode), sự kiện sinh nhật/kỷ niệm và chương trình khuyến mãi hiện hành.
    /// </summary>
    [HttpGet("{memberId:int}/deals")]
    [AllowAnonymous]
    public async Task<ActionResult<MemberDealsResponseDto>> GetDeals(
        int memberId,
        CancellationToken cancellationToken)
    {
        var result = await memberService.GetPersonalizedDealsAsync(memberId, cancellationToken);
        return Ok(result);
    }

    // ── Alerts ───────────────────────────────────────────────────────────────

    /// <summary>Lấy danh sách cảnh báo (Allergy, Budget, DuplicatePurchase, OutOfStock) của hội viên.</summary>
    [HttpGet("{memberId:int}/alerts")]
    [AllowAnonymous]
    public async Task<ActionResult<MemberAlertsResponseDto>> GetAlerts(
        int memberId,
        CancellationToken cancellationToken)
    {
        var result = await memberService.GetAlertsAsync(memberId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Đánh dấu đã đọc các cảnh báo được chỉ định.</summary>
    [HttpPatch("{memberId:int}/alerts/mark-read")]
    [AllowAnonymous]
    public async Task<IActionResult> MarkAlertsRead(
        int memberId,
        [FromBody] MarkAlertsReadRequestDto request,
        CancellationToken cancellationToken)
    {
        await memberService.MarkAlertsReadAsync(memberId, request, cancellationToken);
        return NoContent();
    }

    // ── Budget (self-service) ─────────────────────────────────────────────────

    /// <summary>Lấy ngân sách mua sắm hiện tại của member đang đăng nhập.</summary>
    [Authorize]
    [HttpGet("me/budget")]
    [ProducesResponseType(typeof(MemberBudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberBudgetDto>> GetMyBudget(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.GetBudgetAsync(accountId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật ngân sách mua sắm. Đặt SpendingLimit = null để bỏ giới hạn.
    /// </summary>
    [Authorize]
    [HttpPut("me/budget")]
    [ProducesResponseType(typeof(MemberBudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberBudgetDto>> UpdateMyBudget(
        [FromBody] UpdateBudgetRequestDto request,
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.UpdateBudgetAsync(accountId.Value, request, cancellationToken);
        return Ok(result);
    }

    // ── Health Preferences ──────────────────────────────────────────────────

    /// <summary>
    /// Lấy toàn bộ chế độ ăn &amp; dị ứng của member đang đăng nhập, nhóm theo Status.
    /// </summary>
    [Authorize]
    [HttpGet("me/health-preferences")]
    [ProducesResponseType(typeof(MemberHealthPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberHealthPreferencesDto>> GetMyHealthPreferences(
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.GetHealthPreferencesAsync(accountId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật chế độ ăn &amp; dị ứng. Danh sách mới THAY THẾ HOÀN TOÀN danh sách cũ.
    /// Gửi danh sách rỗng [] để xóa hết tất cả.
    /// </summary>
    [Authorize]
    [HttpPut("me/health-preferences")]
    [ProducesResponseType(typeof(MemberHealthPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberHealthPreferencesDto>> UpdateMyHealthPreferences(
        [FromBody] UpdateHealthPreferencesRequestDto request,
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.UpdateHealthPreferencesAsync(
            accountId.Value, request, cancellationToken);
        return Ok(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────

    private int? GetCurrentAccountId()
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var id) ? id : null;
    }
}

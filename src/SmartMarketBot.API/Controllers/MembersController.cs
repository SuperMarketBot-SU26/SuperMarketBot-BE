using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Members;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// Flow 3 — Budget &amp; Health + Flow 2 Deal Hunter + Member Alerts.
/// Base route: api/members/{memberId}/...
/// </summary>
[ApiController]
[Route("api/members")]
public sealed class MembersController(IMemberService memberService) : ControllerBase
{
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
}

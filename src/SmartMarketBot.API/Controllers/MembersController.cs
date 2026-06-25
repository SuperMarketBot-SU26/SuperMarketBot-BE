using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Members;
using SmartMarketBot.Application.Models.MealSuggestions;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Application.Models.Realtime;
using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.API.Controllers;

/// <summary>
/// Flow 3 — Budget &amp; Health + Flow 2 Deal Hunter + Member Alerts + Profile.
/// Base route: api/members/...
/// </summary>
[ApiController]
[Route("api/members")]
public sealed class MembersController(
    IMemberService memberService,
    IMemberRealtimeNotifier realtimeNotifier) : ControllerBase
{
    // ── Notifications (SignalR) ──────────────────────────────────────────────────────

    /// <summary>
    /// [DEV] Gửi thử một notification realtime tới member đang đăng nhập.
    /// FE dùng để test kết nối SignalR hub /hubs/member.
    /// Flow: connect hub → JoinMemberGroup(memberId) → gọi endpoint này → nhận event "memberUpdate".
    /// </summary>
    [Authorize]
    [HttpPost("me/notifications/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendTestNotification(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var profile = await memberService.GetProfileAsync(accountId.Value, cancellationToken);

        await realtimeNotifier.PushToMemberAsync(profile.MemberId,
            new MemberRealtimeUpdateDto(
                MemberId:   profile.MemberId,
                UpdateType: "TestNotification",
                Title:      "🔔 Kết nối thành công",
                Message:    $"Xin chào {profile.FullName}! SignalR realtime notification hoạt động bình thường.",
                Payload:    new { AccountId = accountId.Value, MemberId = profile.MemberId },
                Timestamp:  VnDateTime.Now),
            cancellationToken);

        return Ok(new { success = true, message = "Đã gửi test notification qua SignalR." });
    }

    /// <summary>
    /// Lấy danh sách notification của member, mới nhất trước.
    /// Hỗ trợ phân trang qua query ?page=1&amp;pageSize=20.
    /// </summary>
    [Authorize]
    [HttpGet("me/notifications")]
    [ProducesResponseType(typeof(NotificationListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationListDto>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.GetNotificationsAsync(accountId.Value, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Số lượng notification chưa đọc — dùng cho badge UI.
    /// </summary>
    [Authorize]
    [HttpGet("me/notifications/unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.GetUnreadCountAsync(accountId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Đánh dấu tất cả notification đã đọc (khi member mở màn hình thông báo).
    /// </summary>
    [Authorize]
    [HttpPatch("me/notifications/read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        await memberService.MarkAllNotificationsReadAsync(accountId.Value, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Đánh dấu một notification cụ thể đã đọc.
    /// </summary>
    [Authorize]
    [HttpPatch("me/notifications/{notificationId:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkOneRead(int notificationId, CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        await memberService.MarkNotificationReadAsync(accountId.Value, notificationId, cancellationToken);
        return NoContent();
    }

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

    /// <summary>
    /// Lấy danh sách gợi ý món ăn cá nhân hóa dựa trên chế độ sức khỏe và dị ứng.
    /// </summary>
    [Authorize]
    [HttpGet("me/personalized-meals")]
    [ProducesResponseType(typeof(IReadOnlyList<RecipeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<RecipeDto>>> GetPersonalizedMeals(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.GetPersonalizedMealsAsync(accountId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm cá nhân hóa dựa trên lịch sử mua sắm và sức khỏe.
    /// </summary>
    [Authorize]
    [HttpGet("me/personalized-products")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetPersonalizedProducts(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await memberService.GetPersonalizedProductsAsync(accountId.Value, cancellationToken);
        return Ok(result);
    }

    // ── Avatar ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Upload ảnh đại diện (avatar) hiển thị UI cho tài khoản đang đăng nhập.
    /// Chấp nhận file ảnh multipart/form-data (jpg, jpeg, png, webp). Tối đa 5MB.
    /// </summary>
    [Authorize]
    [HttpPut("me/avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AvatarUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AvatarUploadResponseDto>> UploadAvatar(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest("Vui lòng chọn file ảnh.");

        // Kiểm tra content type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest("Chỉ chấp nhận ảnh định dạng JPG, PNG hoặc WebP.");

        await using var stream = file.OpenReadStream();
        var result = await memberService.UploadAvatarAsync(
            accountId.Value, stream, file.FileName, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Xóa ảnh đại diện hiện tại (set AvatarUrl = null).
    /// </summary>
    [Authorize]
    [HttpDelete("me/avatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        await memberService.DeleteAvatarAsync(accountId.Value, cancellationToken);
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────

    private int? GetCurrentAccountId()
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var id) ? id : null;
    }
}

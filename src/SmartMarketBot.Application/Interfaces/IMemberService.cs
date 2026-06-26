using SmartMarketBot.Application.Models.Members;
using SmartMarketBot.Application.Models.MealSuggestions;
using SmartMarketBot.Application.Models.Products;
using HealthTagDto = SmartMarketBot.Application.Models.Members.HealthTagDto;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Flow 3 — Budget &amp; Health + Flow 2 Deal Hunter + Member Alerts.</summary>
public interface IMemberService
{
    // ── Flow 3: Budget &amp; Health ──────────────────────────────────────────────

    /// <summary>Cài đặt ngân sách stop-loss cho phiên mua sắm (dùng bởi robot flow).</summary>
    Task<SetBudgetResponseDto> SetShoppingBudgetAsync(int memberId, SetBudgetRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Quét barcode sản phẩm vào giỏ hàng.
    /// Kiểm tra: (1) dị ứng, (2) vượt ngân sách, (3) mua trùng sản phẩm.
    /// </summary>
    Task<ScanItemResponseDto> ScanItemAsync(int memberId, ScanItemRequestDto request, CancellationToken ct = default);

    // ── Flow 2: Deal Hunter ──────────────────────────────────────────────────

    /// <summary>Lấy danh sách deal/ưu đãi cá nhân hóa dựa trên lịch sử mua sắm.</summary>
    Task<MemberDealsResponseDto> GetPersonalizedDealsAsync(int memberId, CancellationToken ct = default);

    // ── Alerts ───────────────────────────────────────────────────────────────

    Task<MemberAlertsResponseDto> GetAlertsAsync(int memberId, CancellationToken ct = default);

    Task MarkAlertsReadAsync(int memberId, MarkAlertsReadRequestDto request, CancellationToken ct = default);

    // ── Profile ──────────────────────────────────────────────────────────────

    /// <summary>Lấy thông tin profile đầy đủ của member (kèm Account + Membership).</summary>
    Task<MemberProfileDto> GetProfileAsync(int accountId, CancellationToken ct = default);

    /// <summary>Cập nhật FullName và Phone của member.</summary>
    Task<MemberProfileDto> UpdateProfileAsync(int accountId, UpdateProfileRequestDto request, CancellationToken ct = default);

    // ── Budget (self-service) ─────────────────────────────────────────────────

    /// <summary>Lấy ngân sách mua sắm hiện tại của member.</summary>
    Task<MemberBudgetDto> GetBudgetAsync(int accountId, CancellationToken ct = default);

    /// <summary>Cập nhật (hoặc xóa) ngân sách mua sắm. SpendingLimit = null → bỏ giới hạn.</summary>
    Task<MemberBudgetDto> UpdateBudgetAsync(int accountId, UpdateBudgetRequestDto request, CancellationToken ct = default);

    // ── Health Preferences (chế độ ăn & dị ứng) ──────────────────────────────

    /// <summary>Lấy gợi ý món ăn cá nhân hóa dựa trên dị ứng và sở thích.</summary>
    Task<IReadOnlyList<RecipeDto>> GetPersonalizedMealsAsync(int accountId, CancellationToken ct = default);

    /// <summary>Lấy gợi ý sản phẩm cá nhân hóa dựa trên lịch sử mua sắm và dị ứng.</summary>
    Task<IReadOnlyList<ProductDto>> GetPersonalizedProductsAsync(int accountId, CancellationToken ct = default);

    /// <summary>Lấy toàn bộ chế độ ăn &amp; dị ứng của member, nhóm theo Status.</summary>
    Task<MemberHealthPreferencesDto> GetHealthPreferencesAsync(int accountId, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật chế độ ăn &amp; dị ứng. Danh sách mới sẽ THAY THẾ HOÀN TOÀN danh sách cũ (upsert + delete).
    /// </summary>
    Task<MemberHealthPreferencesDto> UpdateHealthPreferencesAsync(int accountId, UpdateHealthPreferencesRequestDto request, CancellationToken ct = default);

    /// <summary>Lấy toàn bộ danh sách HealthTag trong hệ thống (để FE hiển thị picker).</summary>
    Task<IReadOnlyList<HealthTagDto>> GetAllHealthTagsAsync(CancellationToken ct = default);

    /// <summary>Lấy danh sách sản phẩm yêu thích của member đang đăng nhập.</summary>
    Task<IReadOnlyList<ProductDto>> GetFavoriteProductsAsync(int accountId, CancellationToken ct = default);

    /// <summary>Thêm hoặc xóa sản phẩm khỏi danh sách yêu thích.</summary>
    Task<FavoriteToggleResponseDto> ToggleFavoriteProductAsync(int accountId, int productId, CancellationToken ct = default);

    /// <summary>Lấy lịch sử mua hàng gần đây của member.</summary>
    Task<IReadOnlyList<PurchaseHistoryItemDto>> GetPurchaseHistoryAsync(int accountId, int limit = 10, CancellationToken ct = default);

    // ── Avatar ───────────────────────────────────────────────────────────────

    /// <summary>Upload ảnh đại diện UI lên Cloudinary, lưu URL vào ACCOUNT.AvatarUrl.</summary>
    Task<AvatarUploadResponseDto> UploadAvatarAsync(int accountId, Stream imageStream, string fileName, CancellationToken ct = default);

    /// <summary>Xóa ảnh đại diện hiện tại (set AvatarUrl = null).</summary>
    Task DeleteAvatarAsync(int accountId, CancellationToken ct = default);

    // ── Notifications ────────────────────────────────────────────────────────

    /// <summary>Lấy danh sách notification của member có phân trang, mới nhất trước.</summary>
    Task<NotificationListDto> GetNotificationsAsync(int accountId, int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>Số lượng notification chưa đọc (dùng cho badge UI).</summary>
    Task<UnreadCountDto> GetUnreadCountAsync(int accountId, CancellationToken ct = default);

    /// <summary>Đánh dấu tất cả notification đã đọc.</summary>
    Task MarkAllNotificationsReadAsync(int accountId, CancellationToken ct = default);

    /// <summary>Đánh dấu một notification cụ thể đã đọc.</summary>
    Task MarkNotificationReadAsync(int accountId, int notificationId, CancellationToken ct = default);
}

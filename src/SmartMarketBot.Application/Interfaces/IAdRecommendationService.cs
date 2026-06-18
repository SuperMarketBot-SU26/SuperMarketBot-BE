using SmartMarketBot.Application.Models.Ads;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>Phase B - Flow 1: Sponsored Recommendations cho Member App (Slot scope).</summary>
public interface IAdRecommendationService
{
    /// <summary>
    /// Trả danh sách SponsoredProduct được sắp xếp theo điểm ưu tiên (Priority + Profile + Weekend).
    /// Member App tự lọc Allergy client-side; BE không drop Sponsored gây dị ứng — chỉ gắn cờ <c>HasAllergenConflict</c>.
    /// </summary>
    /// <param name="slotId">Nếu có: ưu tiên Sponsored cho Product đặt tại Slot này; nếu null: trả theo toàn bộ Zone.</param>
    Task<SponsoredRecommendationsResponseDto> GetRecommendationsAsync(int memberId, int? slotId, CancellationToken ct = default);
}

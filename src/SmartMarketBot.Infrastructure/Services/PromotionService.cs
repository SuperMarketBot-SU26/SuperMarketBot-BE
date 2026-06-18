using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Promotions;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// Flow 5 — Ads Monetization.
/// Priority Score = AdScore (AdPackage) + CustomerMatchScore (SearchMode/Allergy) + PromotionScore.
/// </summary>
public sealed class PromotionService(AppDbContext db, ILocalizationService localizer) : IPromotionService
{
    public async Task<SponsoredRecommendationResponseDto> GetSponsoredRecommendationsAsync(
        SponsoredRecommendationQueryDto query,
        CancellationToken ct = default)
    {
        // 1. Lấy SearchMode + allergy tags của member
        var member = await db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == query.MemberId, ct);

        // SearchMode không còn trong schema mới — dùng "Normal" mặc định.
        const string searchMode = "Normal";

        var allergyTagIds = member is null ? new HashSet<int>() :
            (await db.MemberHealthPreferences
                .AsNoTracking()
                .Where(mhp => mhp.MemberId == query.MemberId && mhp.Status == "Allergy")
                .Select(mhp => mhp.HealthTagId)
                .ToListAsync(ct))
            .ToHashSet();

        var allergenProductIds = allergyTagIds.Count == 0 ? new HashSet<int>() :
            (await db.ProductHealthTags
                .AsNoTracking()
                .Where(pht => allergyTagIds.Contains(pht.HealthTagId))
                .Select(pht => pht.ProductId)
                .ToListAsync(ct))
            .ToHashSet();

        // 2. Lấy sản phẩm khớp từ khoá (Status = 'Available')
        var productsQuery = db.Products
            .AsNoTracking()
            .Where(p => p.Status == "Available");

        if (!string.IsNullOrWhiteSpace(query.Query))
            productsQuery = productsQuery.Where(p => p.ProductName.Contains(query.Query));

        var products = await productsQuery
            .Take(query.Limit * 4)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.UnitPrice,
                p.PromotionPrice,
                p.ImageUrl,
                p.ProductTypeId
            })
            .ToListAsync(ct);

        // 3. Lấy SponsoredProducts đang active, join với Brand và AdPackage qua AdCampaign
        var today = DateOnly.FromDateTime(VnDateTime.Now);
        var sponsored = await db.SponsoredProducts
            .AsNoTracking()
            .Include(sp => sp.AdCampaign)
                .ThenInclude(c => c!.Package)
            .Where(sp => sp.Status == "Active" && sp.AdCampaign != null
                && sp.AdCampaign.Status == "Running"
                && DateOnly.FromDateTime(sp.AdCampaign.StartDate) <= today && DateOnly.FromDateTime(sp.AdCampaign.EndDate) >= today)
            .Select(sp => new
            {
                sp.ProductId,
                AdScore = sp.AdCampaign != null && sp.AdCampaign.Package != null ? sp.AdCampaign.Package.AdScore : 0,
                BrandName = sp.AdCampaign != null && sp.AdCampaign.Brand != null ? sp.AdCampaign.Brand.BrandName : ""
            })
            .ToDictionaryAsync(sp => sp.ProductId, ct);

        // 4. Lấy PromotionScore từ PromotionPrice của Product (schema mới không còn PROMOTION_PRODUCT)
        // PromotionPrice != null → sản phẩm đang khuyến mãi; giá chiết khấu = PromotionPrice
        var activePromos = products
            .Where(p => p.PromotionPrice.HasValue && p.PromotionPrice.Value < p.UnitPrice)
            .ToDictionary(p => p.ProductId, p => new { DiscountPct = (p.UnitPrice - p.PromotionPrice!.Value) / p.UnitPrice * 100m });

        // 5. Tính Priority Score
        var recommendations = products.Select(p =>
        {
            int adScore = 0;
            string? sponsorBrand = null;
            if (sponsored.TryGetValue(p.ProductId, out var sp))
            {
                adScore = sp.AdScore;
                sponsorBrand = sp.BrandName;
            }

            int customerMatchScore = 0;
            bool hasAllergyWarning = allergenProductIds.Contains(p.ProductId);

            if (hasAllergyWarning)
                customerMatchScore = -100;
            else if (searchMode == "Healthy")
                customerMatchScore = 20;
            else if (searchMode == "Budget" && p.UnitPrice < 50000)
                customerMatchScore = 15;

            int promotionScore = 0;
            decimal? discountedPrice = null;
            if (activePromos.TryGetValue(p.ProductId, out var promo))
            {
                // promotionScore tỉ lệ thuận với % giảm giá (cao hơn = ưu tiên hơn)
                promotionScore = (int)(promo.DiscountPct / 5);
                discountedPrice = p.UnitPrice - (p.UnitPrice * promo.DiscountPct / 100m);
            }

            int totalScore = adScore + customerMatchScore + promotionScore;

            return new SponsoredRecommendationDto(
                p.ProductId,
                p.ProductName,
                p.UnitPrice,
                p.ImageUrl,
                totalScore,
                adScore,
                customerMatchScore,
                promotionScore,
                sponsored.ContainsKey(p.ProductId),
                activePromos.ContainsKey(p.ProductId),
                discountedPrice,
                sponsorBrand,
                hasAllergyWarning,
                hasAllergyWarning ? localizer.Get("AllergenRegistered") : null);
        })
        .OrderByDescending(r => r.PriorityScore)
        .Take(query.Limit)
        .ToList();

        return new SponsoredRecommendationResponseDto(
            query.MemberId,
            searchMode,
            recommendations,
            recommendations.Count);
    }
}

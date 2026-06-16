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
        // Đánh giá động mỗi request, dùng giờ Việt Nam — tránh stale time khi deploy Azure (UTC)
        var isWeekend = VnDateTime.Today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        // 1. Lấy SearchMode + allergy tags của member
        var member = await db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == query.MemberId, ct);

        var searchMode = member?.SearchMode ?? "Normal";

        var allergyTagIds = member is null ? new HashSet<int>() :
            (await db.MemberHealthPreferences
                .AsNoTracking()
                .Where(mhp => mhp.MemberId == query.MemberId && mhp.IsAllergy)
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

        // 2. Lấy sản phẩm khớp từ khóa (null = tất cả)
        var productsQuery = db.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Query))
            productsQuery = productsQuery.Where(p => p.ProductName.Contains(query.Query));

        var products = await productsQuery
            .Take(query.Limit * 4)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.UnitPrice,
                p.ImageUrl,
                p.Barcode,
                p.ProductTypeId
            })
            .ToListAsync(ct);

        // 3. Lấy SponsoredProducts đang active, join với Brand và AdPackage qua AdCampaign
        var today = DateOnly.FromDateTime(VnDateTime.Now);
        var sponsored = await db.SponsoredProducts
            .AsNoTracking()
            .Include(sp => sp.Brand)
            .Include(sp => sp.AdCampaign)
                .ThenInclude(c => c!.Package)
            .Where(sp => sp.IsActive && sp.AdCampaign != null
                && DateOnly.FromDateTime(sp.AdCampaign.StartDate) <= today && DateOnly.FromDateTime(sp.AdCampaign.EndDate) >= today)
            .Select(sp => new
            {
                sp.ProductId,
                BrandName = sp.Brand != null ? sp.Brand.BrandName : "",
                AdScore = sp.AdCampaign != null && sp.AdCampaign.Package != null ? sp.AdCampaign.Package.AdScore : 0,
                IsWeekendOnly = sp.AdCampaign != null && sp.AdCampaign.Package != null ? sp.AdCampaign.Package.IsWeekendOnly : false
            })
            .ToDictionaryAsync(sp => sp.ProductId, ct);

        // 4. Lấy PromotionProducts đang active (DateTime -> DateOnly comparison)
        var activePromos = await db.PromotionProducts
            .AsNoTracking()
            .Join(db.Promotions.Where(pr => pr.IsActive && DateOnly.FromDateTime(pr.StartDate) <= today && DateOnly.FromDateTime(pr.EndDate) >= today),
                pp => pp.PromotionId, pr => pr.PromotionId,
                (pp, pr) => new { pp.ProductId, pp.Priority, pr.DiscountValue })
            .ToDictionaryAsync(x => x.ProductId, ct);

        // 5. Tính Priority Score
        var recommendations = products.Select(p =>
        {
            int adScore = 0;
            string? sponsorBrand = null;
            if (sponsored.TryGetValue(p.ProductId, out var sp))
            {
                bool weekendOk = !sp.IsWeekendOnly || isWeekend;
                if (weekendOk)
                {
                    adScore = sp.AdScore;
                    sponsorBrand = sp.BrandName;
                }
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
                promotionScore = promo.Priority * 5;
                discountedPrice = p.UnitPrice * (1 - promo.DiscountValue / 100m);
            }

            int totalScore = adScore + customerMatchScore + promotionScore;

            return new SponsoredRecommendationDto(
                p.ProductId,
                p.ProductName,
                p.UnitPrice,
                p.ImageUrl,
                p.Barcode,
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

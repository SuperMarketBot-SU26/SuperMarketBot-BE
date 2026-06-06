using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Promotions;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// Flow 5 — Ads Monetization.
/// Priority Score = AdScore (AdPackage) + CustomerMatchScore (SearchMode/Allergy) + PromotionScore.
/// </summary>
public sealed class PromotionService(AppDbContext db) : IPromotionService
{
    public async Task<SponsoredRecommendationResponseDto> GetSponsoredRecommendationsAsync(
        SponsoredRecommendationQueryDto query,
        CancellationToken ct = default)
    {
        // Đánh giá động mỗi request — tránh stale time khi API chạy lâu dài trên Azure
        var currentTime = TimeOnly.FromDateTime(DateTime.Now);
        var isWeekend = DateTime.Today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        // 1. Lấy SearchMode + allergy tags của member
        var member = await db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberID == query.MemberId, ct);

        var searchMode = member?.SearchMode ?? "Normal";

        var allergyTagIds = member is null ? new HashSet<int>() :
            (await db.MemberHealthPreferences
                .AsNoTracking()
                .Where(mhp => mhp.MemberID == query.MemberId && mhp.IsAllergy)
                .Select(mhp => mhp.TagID)
                .ToListAsync(ct))
            .ToHashSet();

        var allergenProductIds = allergyTagIds.Count == 0 ? new HashSet<int>() :
            (await db.ProductHealthTags
                .AsNoTracking()
                .Where(pht => allergyTagIds.Contains(pht.TagID))
                .Select(pht => pht.ProductID)
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
                p.ProductID,
                p.ProductName,
                p.UnitPrice,
                p.ImageUrl,
                p.Barcode,
                p.ProductTypeID
            })
            .ToListAsync(ct);

        // 3. Lấy SponsoredProducts đang active, join với Brand và AdPackage
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sponsored = await db.SponsoredProducts
            .AsNoTracking()
            .Include(sp => sp.Brand)
            .Include(sp => sp.AdPackage)
            .Where(sp => sp.IsActive && sp.StartDate <= today && sp.EndDate >= today)
            .Select(sp => new
            {
                sp.ProductID,
                BrandName = sp.Brand.BrandName,
                AdScore = sp.AdPackage.AdScore,
                TimeSlotStart = sp.AdPackage.TimeSlotStart,
                TimeSlotEnd = sp.AdPackage.TimeSlotEnd,
                IsWeekendOnly = sp.AdPackage.IsWeekendOnly
            })
            .ToDictionaryAsync(sp => sp.ProductID, ct);

        // 4. Lấy PromotionProducts đang active
        var activePromos = await db.PromotionProducts
            .AsNoTracking()
            .Join(db.Promotions.Where(pr => pr.IsActive && pr.StartDate <= today && pr.EndDate >= today),
                pp => pp.PromotionID, pr => pr.PromotionID,
                (pp, pr) => new { pp.ProductID, pp.Priority, pr.DiscountValue })
            .ToDictionaryAsync(x => x.ProductID, ct);

        // 5. Tính Priority Score
        var recommendations = products.Select(p =>
        {
            int adScore = 0;
            string? sponsorBrand = null;
            if (sponsored.TryGetValue(p.ProductID, out var sp))
            {
                bool timeOk = (sp.TimeSlotStart == null && sp.TimeSlotEnd == null)
                    || (sp.TimeSlotStart <= currentTime && currentTime <= sp.TimeSlotEnd);
                bool weekendOk = !sp.IsWeekendOnly || isWeekend;
                if (timeOk && weekendOk)
                {
                    adScore = sp.AdScore;
                    sponsorBrand = sp.BrandName;
                }
            }

            int customerMatchScore = 0;
            bool hasAllergyWarning = allergenProductIds.Contains(p.ProductID);

            if (hasAllergyWarning)
                customerMatchScore = -100;
            else if (searchMode == "Healthy")
                customerMatchScore = 20;
            else if (searchMode == "Budget" && p.UnitPrice < 50000)
                customerMatchScore = 15;

            int promotionScore = 0;
            decimal? discountedPrice = null;
            if (activePromos.TryGetValue(p.ProductID, out var promo))
            {
                promotionScore = promo.Priority * 5;
                discountedPrice = p.UnitPrice * (1 - promo.DiscountValue / 100m);
            }

            int totalScore = adScore + customerMatchScore + promotionScore;

            return new SponsoredRecommendationDto(
                p.ProductID,
                p.ProductName,
                p.UnitPrice,
                p.ImageUrl,
                p.Barcode,
                totalScore,
                adScore,
                customerMatchScore,
                promotionScore,
                sponsored.ContainsKey(p.ProductID),
                activePromos.ContainsKey(p.ProductID),
                discountedPrice,
                sponsorBrand,
                hasAllergyWarning,
                hasAllergyWarning ? "Sản phẩm chứa thành phần dị ứng đã đăng ký." : null);
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

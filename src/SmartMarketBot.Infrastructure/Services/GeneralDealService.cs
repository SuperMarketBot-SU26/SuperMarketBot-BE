using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// Trả về deals từ 2 nguồn:
///   1. Product.PromotionPrice != null  → deal thường / flash sale
///   2. SponsoredProducts trong AdCampaign Active → deal quảng cáo
/// Deduplicate theo ProductId, ưu tiên Sponsored (vì có AdScore cao).
/// </summary>
public sealed class GeneralDealService(AppDbContext db, ILocalizationService localizer) : IGeneralDealService
{
    public async Task<GeneralDealsResponseDto> GetDealsAsync(
        GeneralDealsFilterDto filter,
        int? memberId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // ── Nguồn 1: Products có PromotionPrice ───────────────────────────────
        var promoProductsQuery =
            from p in db.Products.AsNoTracking()
            join pt in db.ProductTypes.AsNoTracking() on p.ProductTypeId equals pt.ProductTypeId into ptJoin
            from pt in ptJoin.DefaultIfEmpty()
            join sub in db.Subcategories.AsNoTracking() on pt.SubcategoryId equals sub.SubcategoryId into subJoin
            from sub in subJoin.DefaultIfEmpty()
            where p.PromotionPrice.HasValue
                  && p.PromotionPrice < p.UnitPrice
                  && p.Status == "Available"
            select new { p, ProductType = pt, Subcategory = sub, IsSponsored = false, AdCampaignId = (int?)null, IsSystemBrand = false, BrandId = (int?)null, BrandName = (string?)null };

        // ── Nguồn 2: SponsoredProducts trong AdCampaign Active ─────────────────
        var sponsoredProductsQuery =
            from sp in db.SponsoredProducts.AsNoTracking()
            join ac in db.AdCampaigns.AsNoTracking()
                on sp.AdCampaignId equals ac.AdCampaignId
            join pkg in db.AdPackages.AsNoTracking()
                on ac.PackageId equals pkg.PackageId
            join br in db.Brands.AsNoTracking()
                on ac.BrandId equals br.BrandId
            join p in db.Products.AsNoTracking()
                on sp.ProductId equals p.ProductId
            join pt in db.ProductTypes.AsNoTracking()
                on p.ProductTypeId equals pt.ProductTypeId into ptJoin
            from pt in ptJoin.DefaultIfEmpty()
            join sub in db.Subcategories.AsNoTracking() on pt.SubcategoryId equals sub.SubcategoryId into subJoin
            from sub in subJoin.DefaultIfEmpty()
            where sp.Status == SponsoredProductStatus.Active
                  && ac.Status == CampaignStatus.Active
                  && ac.StartDate <= now
                  && ac.EndDate >= now
            select new { p, ProductType = pt, Subcategory = sub, IsSponsored = true, AdCampaignId = (int?)ac.AdCampaignId, IsSystemBrand = br.IsSystemBrand, BrandId = (int?)br.BrandId, BrandName = br.BrandName };

        var combined = await sponsoredProductsQuery
            .Union(promoProductsQuery)
            .ToListAsync(ct);

        // Lấy allergy tags nếu có memberId
        var allergyTagIds = memberId.HasValue
            ? await db.MemberHealthPreferences
                .AsNoTracking()
                .Where(mhp => mhp.MemberId == memberId && mhp.Status == "Allergy")
                .Select(mhp => mhp.HealthTagId)
                .ToListAsync(ct)
            : new List<int>();

        var productIds = combined.Select(c => c.p.ProductId).Distinct().ToList();

        var tagMap = await db.ProductHealthTags
            .AsNoTracking()
            .Where(pht => productIds.Contains(pht.ProductId))
            .Select(pht => new { pht.ProductId, pht.HealthTagId })
            .ToListAsync(ct);

        var allergyTagNames = allergyTagIds.Count > 0
            ? await db.HealthTags
                .AsNoTracking()
                .Where(t => allergyTagIds.Contains(t.HealthTagId))
                .ToDictionaryAsync(t => t.HealthTagId, t => t.TagName, ct)
            : new Dictionary<int, string>();

        var allergenConflicts = tagMap
            .Where(x => allergyTagIds.Contains(x.HealthTagId))
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => allergyTagNames.GetValueOrDefault(x.HealthTagId) ?? "").ToList());

        // ── Apply filters ─────────────────────────────────────────────────────
        var items = combined
            .Where(c => filter.ProductTypeId == null || c.p.ProductTypeId == filter.ProductTypeId)
            .Where(c => filter.CategoryId == null || (c.Subcategory?.CategoryId ?? 0) == filter.CategoryId)
            .Select(c =>
            {
                var originalPrice = c.p.UnitPrice;
                var dealPrice = c.p.PromotionPrice ?? originalPrice;
                var discountPct = originalPrice > 0
                    ? (int)Math.Round((originalPrice - dealPrice) / originalPrice * 100)
                    : 0;

                var allergenList = allergenConflicts.GetValueOrDefault(c.p.ProductId) ?? [];

                return new
                {
                    c.p,
                    c.ProductType,
                    c.IsSponsored,
                    c.AdCampaignId,
                    c.IsSystemBrand,
                    c.BrandId,
                    c.BrandName,
                    OriginalPrice = originalPrice,
                    DealPrice = dealPrice,
                    DiscountPercent = discountPct,
                    AllergenList = allergenList,
                    HasAllergenConflict = allergenList.Count > 0
                };
            })
            .Where(x => filter.MinDiscountPercent == null || x.DiscountPercent >= filter.MinDiscountPercent)
            .OrderByDescending(x => x.IsSponsored)   // Sponsored deal lên đầu
            .ThenByDescending(x => x.DiscountPercent)
            .ThenBy(x => x.p.ProductName)
            .ToList();

        var totalCount = items.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

        var paged = items
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new GeneralDealDto(
                x.p.ProductId,
                x.p.ProductName,
                x.OriginalPrice,
                x.DealPrice < x.OriginalPrice ? x.DealPrice : null,
                x.DiscountPercent > 0 ? x.DiscountPercent : null,
                x.IsSponsored ? localizer.Get("DealLabel_Sponsored") : localizer.Get("DealLabel_Promotion"),
                x.p.ImageUrl,
                x.ProductType?.TypeName,
                x.p.ProductTypeId,
                x.BrandName,
                x.BrandId,
                x.IsSystemBrand,
                [],
                x.HasAllergenConflict,
                x.AllergenList,
                null,
                x.AdCampaignId,
                null,
                null))
            .ToList();

        return new GeneralDealsResponseDto(
            totalCount,
            filter.PageNumber,
            filter.PageSize,
            totalPages,
            paged);
    }
}

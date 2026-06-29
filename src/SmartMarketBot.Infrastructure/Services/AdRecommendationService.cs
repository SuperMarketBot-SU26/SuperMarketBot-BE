using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// Phase B - Flow 1: Sponsored Recommendations dựa trên SemanticObject.
/// Robot gửi tọa độ (X, Y) → tìm SemanticObject loại "shelf" chứa tọa độ
/// → lấy ProductTypeId → tìm SponsoredProducts cùng ProductType → trả về quảng cáo.
/// </summary>
public sealed class AdRecommendationService(AppDbContext db, ILocalizationService localizer) : IAdRecommendationService
{
    public async Task<SponsoredRecommendationsResponseDto> GetRecommendationsAsync(
        int memberId, int? slotId, CancellationToken ct = default)
    {
        var member = await db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == memberId, ct)
            ?? throw new KeyNotFoundException(localizer.Get("MemberNotFound", memberId));

        var nowUtc = DateTime.UtcNow;

        // Lấy tất cả campaign active
        var activeCampaigns = await db.AdCampaigns.AsNoTracking()
            .Where(ac => ac.Status == CampaignStatus.Active
                         && ac.StartDate <= nowUtc
                         && ac.EndDate >= nowUtc)
            .Select(ac => new { ac.AdCampaignId, ac.SemanticObjectId })
            .ToListAsync(ct);

        // Lấy SemanticObjectIds từ các campaign
        var campaignSemanticObjectIds = activeCampaigns
            .Where(c => c.SemanticObjectId.HasValue)
            .Select(c => c.SemanticObjectId!.Value)
            .Distinct()
            .ToList();

        // Lấy ProductTypeIds từ các SemanticObject
        var productTypeIds = await db.SemanticObjects
            .AsNoTracking()
            .Where(so => campaignSemanticObjectIds.Contains(so.ObjectId) && so.ProductTypeId.HasValue)
            .Select(so => so.ProductTypeId!.Value)
            .Distinct()
            .ToListAsync(ct);

        // Lấy SponsoredProducts có Product cùng ProductType
        var sponsoredQuery =
            from sp in db.SponsoredProducts.AsNoTracking()
            join ac in db.AdCampaigns.AsNoTracking() on sp.AdCampaignId equals ac.AdCampaignId
            join br in db.Brands.AsNoTracking() on ac.BrandId equals br.BrandId
            join pkg in db.AdPackages.AsNoTracking() on ac.PackageId equals pkg.PackageId
            join p in db.Products.AsNoTracking() on sp.ProductId equals p.ProductId
            where sp.Status == SponsoredProductStatus.Active
                  && ac.Status == CampaignStatus.Active
                  && ac.StartDate <= nowUtc
                  && ac.EndDate >= nowUtc
                  && productTypeIds.Contains(p.ProductTypeId)
            select new
            {
                sp.SponsoredId,
                sp.AdCampaignId,
                ac.CampaignName,
                ac.SemanticObjectId,
                br.BrandId,
                br.BrandName,
                p.ProductId,
                p.ProductName,
                p.ProductTypeId,
                p.UnitPrice,
                p.PromotionPrice,
                p.ImageUrl,
                sp.Priority,
                AdScore = pkg.AdScore
            };

        var raw = await sponsoredQuery.ToListAsync(ct);

        // Lấy tập HealthTag mà Member dị ứng
        var allergyTagIds = await db.MemberHealthPreferences
            .AsNoTracking()
            .Where(mhp => mhp.MemberId == memberId && mhp.Status == "Allergy")
            .Select(mhp => mhp.HealthTagId)
            .ToListAsync(ct);

        var tagNameMap = await db.HealthTags
            .AsNoTracking()
            .Where(t => allergyTagIds.Contains(t.HealthTagId))
            .ToDictionaryAsync(t => t.HealthTagId, t => t.TagName, ct);

        var productIds = raw.Select(r => r.ProductId).Distinct().ToList();
        var productTagMap = await db.ProductHealthTags
            .AsNoTracking()
            .Where(pht => productIds.Contains(pht.ProductId))
            .Select(pht => new { pht.ProductId, pht.HealthTagId })
            .ToListAsync(ct);
        var productAllergens = productTagMap
            .Where(x => allergyTagIds.Contains(x.HealthTagId))
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => tagNameMap[x.HealthTagId]).ToList());

        // Lấy Slot info cho mỗi Product
        var productSlotMap = await db.ProductSlots
            .AsNoTracking()
            .Where(ps => productIds.Contains(ps.ProductId))
            .Join(db.Slots.AsNoTracking(), ps => ps.SlotId, s => s.SlotId, (ps, s) => new { ps.ProductId, s.SlotId, s.SlotCode, s.ShelfId })
            .Join(db.Shelves.AsNoTracking(), x => x.ShelfId, sh => sh.ShelfId, (x, sh) => new { x.ProductId, x.SlotId, x.SlotCode, sh.AisleId })
            .Join(db.Aisles.AsNoTracking(), x => x.AisleId, ai => ai.AisleId, (x, ai) => new { x.ProductId, x.SlotId, x.SlotCode, ai.ZoneId })
            .Join(db.Zones.AsNoTracking(), x => x.ZoneId, z => z.ZoneId, (x, z) => new { x.ProductId, x.SlotId, x.SlotCode, x.ZoneId, z.ZoneName })
            .ToListAsync(ct);

        var slotFirstByProduct = productSlotMap
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.First());

        var isWeekend = (DateTime.UtcNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        var profileBase = 20;
        var weekendBonus = isWeekend ? 10 : 0;

        var items = raw
            .Select(r =>
            {
                var slotInfo = slotFirstByProduct.GetValueOrDefault(r.ProductId);
                var allergens = productAllergens.GetValueOrDefault(r.ProductId) ?? [];
                var total = r.Priority + r.AdScore + profileBase + weekendBonus;
                return new SponsoredRecommendationDto(
                    r.SponsoredId,
                    r.AdCampaignId,
                    r.CampaignName,
                    r.BrandId,
                    r.BrandName,
                    r.ProductId,
                    r.ProductName,
                    r.UnitPrice,
                    r.PromotionPrice,
                    r.ImageUrl,
                    slotInfo?.SlotId,
                    slotInfo?.SlotCode,
                    slotInfo?.ZoneId,
                    slotInfo?.ZoneName,
                    r.Priority,
                    profileBase,
                    weekendBonus,
                    total,
                    allergens.Count > 0,
                    allergens);
            })
            .OrderByDescending(x => x.TotalScore)
            .ThenBy(x => x.ProductName)
            .ToList();

        return new SponsoredRecommendationsResponseDto(
            memberId,
            slotId,
            null,
            null,
            items.Count,
            items);
    }
}

using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Ads;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// Phase B - Flow 1: Sponsored Recommendations (Slot scope, ranked list).
/// Công thức: <c>TotalScore = Priority (AdPackage.AdScore + SponsoredProduct.Priority) + ProfileScore + WeekendBonus</c>.
/// Member App tự lọc Allergy client-side — BE chỉ gắn cờ <c>HasAllergenConflict</c> + liệt kê <c>AllergenConflicts</c>.
/// </summary>
public sealed class AdRecommendationService(AppDbContext db, ILocalizationService localizer) : IAdRecommendationService
{
    public async Task<SponsoredRecommendationsResponseDto> GetRecommendationsAsync(
        int memberId, int? slotId, CancellationToken ct = default)
    {
        // 1. Lấy Member để check tồn tại + suy ra hồ sơ cá nhân
        var member = await db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == memberId, ct)
            ?? throw new KeyNotFoundException(localizer.Get("MemberNotFound", memberId));

        // 2. Suy ra ZoneID từ SlotID (nếu có)
        int? contextZoneId = null;
        string? contextZoneName = null;
        if (slotId is { } sid)
        {
            var slotChain = await (
                from s in db.Slots.AsNoTracking()
                join sh in db.Shelves.AsNoTracking() on s.ShelfId equals sh.ShelfId
                join ai in db.Aisles.AsNoTracking() on sh.AisleId equals ai.AisleId
                join z in db.Zones.AsNoTracking() on ai.ZoneId equals z.ZoneId
                where s.SlotId == sid
                select new { z.ZoneId, z.ZoneName }
            ).FirstOrDefaultAsync(ct);

            contextZoneId = slotChain?.ZoneId;
            contextZoneName = slotChain?.ZoneName;
        }

        // 3. Query Sponsored products đang active và còn trong thời gian
        var nowUtc = DateTime.UtcNow;

        // Bước 3a: lấy tất cả campaign active
        var activeCampaigns = await db.AdCampaigns.AsNoTracking()
            .Where(ac => ac.Status == "Running"
                         && ac.StartDate <= nowUtc
                         && ac.EndDate >= nowUtc)
            .Select(ac => new { ac.AdCampaignId, ac.RobotZoneId })
            .ToListAsync(ct);

        // Bước 3b: lấy tập RobotZoneID ứng với contextZoneId (nếu có)
        var campaignRobotZoneIds = activeCampaigns.Select(c => c.RobotZoneId).Where(x => x != null).Distinct().ToList();
        var allowedRobotZoneIds = new HashSet<int?>(activeCampaigns.Select(c => c.RobotZoneId));
        if (contextZoneId is { } cz)
        {
            var matched = await db.RobotZones.AsNoTracking()
                .Where(rz => rz.ZoneId == cz)
                .Select(rz => (int?)rz.RobotZoneId)
                .ToListAsync(ct);
            // Chỉ giữ campaign có RobotZoneId trong matched HOẶC RobotZoneId = null (campaign tổng quát)
            allowedRobotZoneIds = new HashSet<int?>(activeCampaigns
                .Where(c => c.RobotZoneId == null || matched.Contains(c.RobotZoneId))
                .Select(c => c.RobotZoneId));
        }

        var allowedCampaignIds = activeCampaigns
            .Where(c => allowedRobotZoneIds.Contains(c.RobotZoneId))
            .Select(c => c.AdCampaignId)
            .ToList();

        // Bước 3c: Sponsored join Brand + Package + Product
        var sponsoredQuery =
            from sp in db.SponsoredProducts.AsNoTracking()
            join ac in db.AdCampaigns.AsNoTracking() on sp.AdCampaignId equals ac.AdCampaignId
            join br in db.Brands.AsNoTracking() on ac.BrandId equals br.BrandId
            join pkg in db.AdPackages.AsNoTracking() on ac.PackageId equals pkg.PackageId
            join p in db.Products.AsNoTracking() on sp.ProductId equals p.ProductId
            where sp.Status == "Active"
                  && allowedCampaignIds.Contains(sp.AdCampaignId)
            select new
            {
                sp.SponsoredId,
                sp.AdCampaignId,
                ac.CampaignName,
                br.BrandId,
                br.BrandName,
                p.ProductId,
                p.ProductName,
                p.UnitPrice,
                p.PromotionPrice,
                p.ImageUrl,
                sp.Priority,
                AdScore = pkg.AdScore
            };

        var raw = await sponsoredQuery.ToListAsync(ct);

        // 4. Lấy tập HealthTag mà Member dị ứng (status='Allergy')
        var allergyTagIds = await db.MemberHealthPreferences
            .AsNoTracking()
            .Where(mhp => mhp.MemberId == memberId && mhp.Status == "Allergy")
            .Select(mhp => mhp.HealthTagId)
            .ToListAsync(ct);

        // 5. Lấy map HealthTagId → TagName cho conflict message
        var tagNameMap = await db.HealthTags
            .AsNoTracking()
            .Where(t => allergyTagIds.Contains(t.HealthTagId))
            .ToDictionaryAsync(t => t.HealthTagId, t => t.TagName, ct);

        // 6. Lấy danh sách ProductId → list HealthTag mà Sponsored product thuộc về (chỉ cho các product trong raw)
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

        // 7. Snapshot vị trí Slot cho mỗi Sponsored product (qua ProductSlot join)
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

        // 8. Profile score: ưu tiên Sponsored cùng Brand mà Member từng mua (AdPackage.AdScore * 2)
        //    Đơn giản hóa Phase B: cứ +20 nếu Member có MemberId, Weekend +10
        var profileBase = 20;
        var isWeekend = (DateTime.UtcNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
        var weekendBonus = isWeekend ? 10 : 0;

        // 9. Build DTO + ranking
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
            contextZoneId,
            contextZoneName,
            items.Count,
            items);
    }
}

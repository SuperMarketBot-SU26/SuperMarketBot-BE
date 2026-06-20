using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Members;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>Flow 3 — Budget &amp; Health + Flow 2 Deal Hunter + Member Alerts.</summary>
public sealed class MemberService(AppDbContext db, ILocalizationService localizer) : IMemberService
{
    // ── Budget ───────────────────────────────────────────────────────────────

    public async Task<SetBudgetResponseDto> SetShoppingBudgetAsync(
        int memberId, SetBudgetRequestDto request, CancellationToken ct = default)
    {
        var member = await db.Members.FindAsync([memberId], ct)
            ?? throw new KeyNotFoundException(localizer.Get("MemberNotFound", memberId));

        member.SpendingLimit = request.Budget;
        await db.SaveChangesAsync(ct);

        return new SetBudgetResponseDto(
            memberId,
            request.Budget,
            "Normal",
            localizer.Get("BudgetSet", request.Budget));
    }

    // ── Scan Item ────────────────────────────────────────────────────────────

    public async Task<ScanItemResponseDto> ScanItemAsync(
        int memberId, ScanItemRequestDto request, CancellationToken ct = default)
    {
        var member = await db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == memberId, ct)
            ?? throw new KeyNotFoundException(localizer.Get("MemberNotFound", memberId));

        var product = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId, ct);

        if (product is null)
            return new ScanItemResponseDto(false, null, localizer.Get("ProductNotFound", request.ProductId),
                0, "Unknown", 0, request.CurrentCartTotal, null, []);

        // 1. Kiểm tra dị ứng (status = 'Allergy' trong MEMBERHEALTH_PREFERENCE)
        var allergyTagIds = await db.MemberHealthPreferences
            .AsNoTracking()
            .Where(mhp => mhp.MemberId == memberId && mhp.Status == "Allergy")
            .Select(mhp => mhp.HealthTagId)
            .ToListAsync(ct);

        bool hasAllergy = allergyTagIds.Count > 0 && await db.ProductHealthTags
            .AsNoTracking()
            .AnyAsync(pht => pht.ProductId == product.ProductId && allergyTagIds.Contains(pht.HealthTagId), ct);

        // 2. Kiểm tra vượt ngân sách (SpendingLimit)
        var newTotal = request.CurrentCartTotal + product.UnitPrice * request.Quantity;
        bool overBudget = member.SpendingLimit.HasValue && newTotal > member.SpendingLimit.Value;

        // 3. Kiểm tra mua trùng trong 7 ngày gần nhất
        var sevenDaysAgo = VnDateTime.Now.AddDays(-7);
        bool isDuplicate = await db.InvoiceHistoryItems
            .AsNoTracking()
            .Where(hi => hi.ProductId == product.ProductId
                && hi.InvoiceHistory != null
                && hi.InvoiceHistory.MemberId == memberId
                && hi.InvoiceHistory.PurchaseDate >= sevenDaysAgo)
            .AnyAsync(ct);

        // Xác định cảnh báo (ưu tiên dị ứng > ngân sách > trùng)
        string? alertType = null;
        string? alertMessage = null;

        if (hasAllergy)
        {
            alertType = "Allergy";
            alertMessage = localizer.Get("AllergyAlert", product.ProductName);
        }
        else if (overBudget)
        {
            alertType = "BudgetExceeded";
            alertMessage = localizer.Get("BudgetExceededAlert", product.ProductName, product.UnitPrice, member.SpendingLimit.GetValueOrDefault());
        }
        else if (isDuplicate)
        {
            alertType = "DuplicatePurchase";
            alertMessage = localizer.Get("DuplicatePurchaseAlert", product.ProductName);
        }

        // Lấy sản phẩm thay thế nếu bị block
        var alternatives = new List<AlternativeProductDto>();
        bool isAllowed = alertType is null or "DuplicatePurchase"; // Duplicate chỉ cảnh báo, không chặn

        if (!isAllowed)
        {
            var altProducts = await db.Products
                .AsNoTracking()
                .Where(p => p.ProductTypeId == product.ProductTypeId
                    && p.ProductId != product.ProductId
                    && p.Status == "Available")
                .Take(3)
                .ToListAsync(ct);

            alternatives = altProducts.Select(p => new AlternativeProductDto(
                p.ProductId, p.ProductName, p.UnitPrice, p.ImageUrl,
                alertType == "Allergy" ? localizer.Get("AltReasonAllergy") : localizer.Get("AltReasonBudget"))).ToList();
        }

        return new ScanItemResponseDto(
            isAllowed,
            alertType,
            alertMessage,
            product.ProductId,
            product.ProductName,
            product.UnitPrice,
            isAllowed ? newTotal : request.CurrentCartTotal,
            member.SpendingLimit.HasValue ? member.SpendingLimit.Value - (isAllowed ? newTotal : request.CurrentCartTotal) : null,
            alternatives);
    }

    // ── Deals ────────────────────────────────────────────────────────────────

    public async Task<MemberDealsResponseDto> GetPersonalizedDealsAsync(
        int memberId, CancellationToken ct = default)
    {
        var member = await db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.MemberId == memberId, ct);
        if (member is null) return new MemberDealsResponseDto(memberId, [], 0);

        var today = DateOnly.FromDateTime(VnDateTime.Now);
        var deals = new List<MemberDealDto>();

        // 1. Khuyến mãi: sản phẩm có PromotionPrice < UnitPrice (schema mới dùng PromotionPrice thay vì bảng PROMOTION/PROMOTION_PRODUCT)
        var promoProducts = await db.Products
            .AsNoTracking()
            .Where(p => p.PromotionPrice.HasValue
                && p.PromotionPrice.Value < p.UnitPrice
                && p.Status == "Available")
            .Take(5)
            .ToListAsync(ct);

        foreach (var p in promoProducts)
        {
            var discountPct = (p.UnitPrice - p.PromotionPrice!.Value) / p.UnitPrice * 100m;
            deals.Add(new MemberDealDto(
                p.ProductId, p.ProductName, p.UnitPrice,
                p.PromotionPrice.Value,
                discountPct, "Promotion",
                localizer.Get("PromotionDeal", discountPct), p.ImageUrl));
        }

        // 2. Sponsored: sản phẩm tài trợ đang chạy
        var sponsoredProducts = await db.SponsoredProducts
            .AsNoTracking()
            .Include(sp => sp.AdCampaign)
            .Where(sp => sp.Status == "Active"
                && sp.AdCampaign != null
                && sp.AdCampaign.Status == "Active"
                && sp.AdCampaign.StartDate.Date <= DateTime.UtcNow.Date
                && sp.AdCampaign.EndDate.Date >= DateTime.UtcNow.Date)
            .Take(5)
            .ToListAsync(ct);

        foreach (var sp in sponsoredProducts)
        {
            if (sp.Product is null) continue;
            var p = sp.Product;
            deals.Add(new MemberDealDto(
                p.ProductId, p.ProductName, p.UnitPrice,
                p.PromotionPrice ?? p.UnitPrice,
                0, "Sponsored",
                localizer.Get("SponsoredDeal", sp.AdCampaign?.CampaignName ?? "Brand"), p.ImageUrl));
        }

        return new MemberDealsResponseDto(memberId, deals, deals.Count);
    }

    // ── Alerts ───────────────────────────────────────────────────────────────

    public Task<MemberAlertsResponseDto> GetAlertsAsync(int memberId, CancellationToken ct = default)
    {
        // Bảng MEMBER_ALERT không còn trong schema mới — trả về danh sách trống
        return Task.FromResult(new MemberAlertsResponseDto(memberId, 0, []));
    }

    public Task MarkAlertsReadAsync(int memberId, MarkAlertsReadRequestDto request, CancellationToken ct = default)
    {
        // Bảng MEMBER_ALERT không còn trong schema mới — no-op
        return Task.CompletedTask;
    }
}

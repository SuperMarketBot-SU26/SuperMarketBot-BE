using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Members;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
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

        member.ShoppingBudget = request.Budget;
        await db.SaveChangesAsync(ct);

        return new SetBudgetResponseDto(
            memberId,
            request.Budget,
            member.SearchMode,
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
            .FirstOrDefaultAsync(p => p.Barcode == request.Barcode, ct);

        if (product is null)
            return new ScanItemResponseDto(false, null, localizer.Get("ProductNotFound", request.Barcode),
                0, "Unknown", 0, request.CurrentCartTotal, null, []);

        // 1. Kiểm tra dị ứng
        var allergyTagIds = await db.MemberHealthPreferences
            .AsNoTracking()
            .Where(mhp => mhp.MemberId == memberId && mhp.IsAllergy)
            .Select(mhp => mhp.HealthTagId)
            .ToListAsync(ct);

        bool hasAllergy = allergyTagIds.Count > 0 && await db.ProductHealthTags
            .AsNoTracking()
            .AnyAsync(pht => pht.ProductId == product.ProductId && allergyTagIds.Contains(pht.HealthTagId), ct);

        // 2. Kiểm tra vượt ngân sách
        var newTotal = request.CurrentCartTotal + product.UnitPrice * request.Quantity;
        bool overBudget = member.ShoppingBudget.HasValue && newTotal > member.ShoppingBudget.Value;

        // 3. Kiểm tra mua trùng trong 7 ngày gần nhất
        var sevenDaysAgo = VnDateTime.Now.AddDays(-7);
        bool isDuplicate = await db.InvoiceHistoryItems
            .AsNoTracking()
            .AnyAsync(hi => hi.ProductId == product.ProductId
                && hi.InvoiceHistory.MemberId == memberId
                && hi.InvoiceHistory.PurchaseDate >= sevenDaysAgo, ct);

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
            alertMessage = localizer.Get("BudgetExceededAlert", product.ProductName, product.UnitPrice, member.ShoppingBudget.GetValueOrDefault());
        }
        else if (isDuplicate)
        {
            alertType = "DuplicatePurchase";
            alertMessage = localizer.Get("DuplicatePurchaseAlert", product.ProductName);
        }

        // Lưu alert nếu có
        if (alertType != null)
        {
            db.MemberAlerts.Add(new MemberAlert
            {
                MemberId = memberId,
                AlertType = alertType,
                AlertMessage = alertMessage!
            });
            await db.SaveChangesAsync(ct);
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
                    && p.IsActive)
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
            member.ShoppingBudget.HasValue ? member.ShoppingBudget.Value - (isAllowed ? newTotal : request.CurrentCartTotal) : null,
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

        // 1. Khuyến mãi đang active
        var promos = await db.PromotionProducts
            .AsNoTracking()
            .Join(db.Promotions.Where(pr => pr.IsActive && DateOnly.FromDateTime(pr.StartDate) <= today && DateOnly.FromDateTime(pr.EndDate) >= today),
                pp => pp.PromotionId, pr => pr.PromotionId,
                (pp, pr) => new { pp.ProductId, pr.DiscountValue, pr.PromotionName })
            .Join(db.Products, x => x.ProductId, p => p.ProductId,
                (x, p) => new { p.ProductId, p.ProductName, p.UnitPrice, p.ImageUrl, x.DiscountValue, x.PromotionName })
            .Take(5)
            .ToListAsync(ct);

        foreach (var p in promos)
        {
            deals.Add(new MemberDealDto(
                p.ProductId, p.ProductName, p.UnitPrice,
                p.UnitPrice * (1 - p.DiscountValue / 100m),
                p.DiscountValue, "Promotion", p.PromotionName, p.ImageUrl));
        }

        // 2. Birthday/Anniversary deal
        var events = await db.MemberEvents
            .AsNoTracking()
            .Where(me => me.MemberId == memberId && !me.IsProcessed && me.DiscountPct.HasValue
                && DateOnly.FromDateTime(me.EventDate) >= today && DateOnly.FromDateTime(me.EventDate) <= today.AddDays(7))
            .ToListAsync(ct);

        foreach (var ev in events)
        {
            deals.Add(new MemberDealDto(0, ev.EventName == "Birthday" ? localizer.Get("BdayDeal") : localizer.Get("AnniversaryDeal"),
                0, 0, ev.DiscountPct!.Value, ev.EventName,
                localizer.Get("EventDealReason", ev.DiscountPct!.Value, ev.EventName), null));
        }

        return new MemberDealsResponseDto(memberId, deals, deals.Count);
    }

    // ── Alerts ───────────────────────────────────────────────────────────────

    public async Task<MemberAlertsResponseDto> GetAlertsAsync(int memberId, CancellationToken ct = default)
    {
        var alerts = await db.MemberAlerts
            .AsNoTracking()
            .Where(a => a.MemberId == memberId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new MemberAlertDto(a.AlertId, a.AlertType, a.AlertMessage, a.CreatedAt, a.IsRead))
            .ToListAsync(ct);

        return new MemberAlertsResponseDto(memberId, alerts.Count(a => !a.IsRead), alerts);
    }

    public async Task MarkAlertsReadAsync(int memberId, MarkAlertsReadRequestDto request, CancellationToken ct = default)
    {
        await db.MemberAlerts
            .Where(a => a.MemberId == memberId && request.AlertIds.Contains(a.AlertId))
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsRead, true), ct);
    }
}

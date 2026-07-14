using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Members;
using SmartMarketBot.Application.Models.Products;
using SmartMarketBot.Application.Models.MealSuggestions;
using HealthTagDto = SmartMarketBot.Application.Models.Members.HealthTagDto;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Options;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>Flow 3 — Budget &amp; Health + Flow 2 Deal Hunter + Member Alerts + Profile.</summary>
public sealed class MemberService(
    AppDbContext db,
    ILocalizationService localizer,
    ICloudStorageService cloudStorage,
    IOptions<CloudinaryOptions> cloudinaryOptions,
    IMemberRealtimeNotifier realtimeNotifier,
    IServiceScopeFactory scopeFactory,
    ILogger<MemberService> logger) : IMemberService
{
    private readonly CloudinaryOptions _cloudinaryOpts = cloudinaryOptions.Value;
    // ── Budget (Robot Flow) ───────────────────────────────────────────────────

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

        // Push realtime notification nếu có cảnh báo
        if (alertType is not null)
        {
            // Tạo scope riêng để tránh dùng DbContext của request (đã dispose khi response trả về).
            // Bỏ Task.Run để await theo response — response sẽ trả về sau khi notification được
            // push + lưu DB xong, tránh race condition trên EF Core change tracker.
            var alertTitle = alertType switch
            {
                "Allergy"           => "⚠️ Cảnh báo dị ứng",
                "BudgetExceeded"    => "💰 Vượt ngân sách",
                "DuplicatePurchase" => "🔄 Sản phẩm đã mua",
                _                   => "Thông báo"
            };

            try
            {
                await realtimeNotifier.PushToMemberAsync(memberId, new Application.Models.Realtime.MemberRealtimeUpdateDto(
                    MemberId:   memberId,
                    UpdateType: alertType,
                    Title:      alertTitle,
                    Message:    alertMessage!,
                    Payload:    new { ProductId = product.ProductId, ProductName = product.ProductName },
                    Timestamp:  Domain.Common.VnDateTime.Now));

                // Lưu vào DB trong scope riêng, không truyền ct (tránh cancel khi client disconnect)
                await using var scope = scopeFactory.CreateAsyncScope();
                var notifier = scope.ServiceProvider.GetRequiredService<IMemberNotificationWriter>();
                await notifier.SaveAsync(
                    memberId:    memberId,
                    notifType:   alertType,
                    title:       alertTitle,
                    message:     alertMessage!,
                    payloadJson: System.Text.Json.JsonSerializer.Serialize(
                        new { ProductId = product.ProductId, ProductName = product.ProductName }),
                    ct:          default);
            }
            catch (Exception ex)
            {
                // Không fail transaction chính — chỉ log để debug
                logger.LogWarning(ex,
                    "[MemberService] Không thể push/lưu notification cho MemberId={Id} Type={Type}",
                    memberId, alertType);
            }
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

        // 1. Khuyến mãi: sản phẩm có PromotionPrice < UnitPrice
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

    // ── Profile ───────────────────────────────────────────────────────────────

    public async Task<MemberProfileDto> GetProfileAsync(int accountId, CancellationToken ct = default)
    {
        var account = await db.Accounts
            .AsNoTracking()
            .Include(a => a.Member)
                .ThenInclude(m => m!.Memberships)
            .FirstOrDefaultAsync(a => a.AccountId == accountId, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản #{accountId}.");

        var member = account.Member
            ?? throw new KeyNotFoundException($"Tài khoản #{accountId} chưa có hồ sơ Member.");

        var activeMembership = member.Memberships
            .FirstOrDefault(ms => ms.Status == "Active");

        return new MemberProfileDto(
            MemberId:       member.MemberId,
            AccountId:      accountId,
            FullName:       account.FullName ?? member.FullName,
            Email:          account.Email,
            Phone:          account.Phone,
            AvatarUrl:      account.AvatarUrl,
            FacePath:       member.FacePath,
            TotalPoints:    member.TotalPoints,
            SpendingLimit:  member.SpendingLimit,
            MembershipTier: activeMembership?.TierName ?? "Bronze",
            AccountStatus:  account.Status,
            CreatedAt:      account.CreatedAt);
    }

    public async Task<MemberProfileDto> UpdateProfileAsync(
        int accountId, UpdateProfileRequestDto request, CancellationToken ct = default)
    {
        var account = await db.Accounts
            .Include(a => a.Member)
                .ThenInclude(m => m!.Memberships)
            .FirstOrDefaultAsync(a => a.AccountId == accountId, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản #{accountId}.");

        var member = account.Member
            ?? throw new KeyNotFoundException($"Tài khoản #{accountId} chưa có hồ sơ Member.");

        // Cập nhật FullName (cả 2 bảng Account và Member đều lưu)
        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            account.FullName = request.FullName.Trim();
            member.FullName  = request.FullName.Trim();
        }

        // Cập nhật Phone (chỉ lưu trên Account)
        if (request.Phone is not null)
            account.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        await db.SaveChangesAsync(ct);

        var activeMembership = member.Memberships.FirstOrDefault(ms => ms.Status == "Active");

        return new MemberProfileDto(
            MemberId:       member.MemberId,
            AccountId:      accountId,
            FullName:       account.FullName ?? member.FullName,
            Email:          account.Email,
            Phone:          account.Phone,
            AvatarUrl:      account.AvatarUrl,
            FacePath:       member.FacePath,
            TotalPoints:    member.TotalPoints,
            SpendingLimit:  member.SpendingLimit,
            MembershipTier: activeMembership?.TierName ?? "Bronze",
            AccountStatus:  account.Status,
            CreatedAt:      account.CreatedAt);
    }

    // ── Budget (self-service) ─────────────────────────────────────────────────

    public async Task<MemberBudgetDto> GetBudgetAsync(int accountId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        return new MemberBudgetDto(
            MemberId:      member.MemberId,
            SpendingLimit: member.SpendingLimit,
            Message:       member.SpendingLimit.HasValue
                ? $"Ngân sách hiện tại: {member.SpendingLimit.Value:N0} VNĐ"
                : "Chưa đặt ngân sách mua sắm.");
    }

    public async Task<MemberBudgetDto> UpdateBudgetAsync(
        int accountId, UpdateBudgetRequestDto request, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct, tracking: true);

        member.SpendingLimit = request.SpendingLimit; // null = bỏ giới hạn
        await db.SaveChangesAsync(ct);

        return new MemberBudgetDto(
            MemberId:      member.MemberId,
            SpendingLimit: member.SpendingLimit,
            Message:       member.SpendingLimit.HasValue
                ? $"Đã cập nhật ngân sách: {member.SpendingLimit.Value:N0} VNĐ"
                : "Đã bỏ giới hạn ngân sách mua sắm.");
    }

    // ── Health Preferences (chế độ ăn & dị ứng) ──────────────────────────────

    public async Task<MemberHealthPreferencesDto> GetHealthPreferencesAsync(
        int accountId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        var prefs = await db.MemberHealthPreferences
            .AsNoTracking()
            .Include(p => p.HealthTag)
            .Where(p => p.MemberId == member.MemberId)
            .ToListAsync(ct);

        return BuildHealthPreferencesDto(member.MemberId, prefs);
    }

    public async Task<MemberHealthPreferencesDto> UpdateHealthPreferencesAsync(
        int accountId, UpdateHealthPreferencesRequestDto request, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        // Xác thực tất cả HealthTagId tồn tại
        var requestedTagIds = request.Preferences.Select(p => p.HealthTagId).Distinct().ToList();
        if (requestedTagIds.Count > 0)
        {
            var existingTagIds = await db.HealthTags
                .AsNoTracking()
                .Where(t => requestedTagIds.Contains(t.HealthTagId))
                .Select(t => t.HealthTagId)
                .ToListAsync(ct);

            var missingIds = requestedTagIds.Except(existingTagIds).ToList();
            if (missingIds.Count > 0)
                throw new KeyNotFoundException(
                    $"HealthTagId không tồn tại: {string.Join(", ", missingIds)}.");
        }

        // Xóa toàn bộ preferences cũ của member
        var oldPrefs = await db.MemberHealthPreferences
            .Where(p => p.MemberId == member.MemberId)
            .ToListAsync(ct);
        db.MemberHealthPreferences.RemoveRange(oldPrefs);

        // Thêm preferences mới (loại trùng theo HealthTagId)
        var newPrefs = request.Preferences
            .DistinctBy(p => p.HealthTagId)
            .Select(p => new MemberHealthPreference
            {
                MemberId    = member.MemberId,
                HealthTagId = p.HealthTagId,
                Status      = p.Status
            })
            .ToList();

        await db.MemberHealthPreferences.AddRangeAsync(newPrefs, ct);
        await db.SaveChangesAsync(ct);

        // Load lại với navigation để trả về đầy đủ thông tin tag
        var savedPrefs = await db.MemberHealthPreferences
            .AsNoTracking()
            .Include(p => p.HealthTag)
            .Where(p => p.MemberId == member.MemberId)
            .ToListAsync(ct);

        return BuildHealthPreferencesDto(member.MemberId, savedPrefs);
    }

    public async Task<IReadOnlyList<HealthTagDto>> GetAllHealthTagsAsync(CancellationToken ct = default)
    {
        return await db.HealthTags
            .AsNoTracking()
            .OrderBy(t => t.TagType)
            .ThenBy(t => t.TagName)
            .Select(t => new HealthTagDto(t.HealthTagId, t.TagName, t.TagType))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductDto>> GetFavoriteProductsAsync(int accountId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        return await db.MemberFavoriteProducts
            .AsNoTracking()
            .Where(x => x.MemberId == member.MemberId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ProductDto(
                x.Product!.ProductId,
                x.Product.ProductName,
                x.Product.UnitPrice,
                x.Product.Status,
                x.Product.ImageUrl,
                x.Product.ProductTypeId))
            .ToListAsync(ct);
    }

    public async Task<FavoriteToggleResponseDto> ToggleFavoriteProductAsync(int accountId, int productId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        var productExists = await db.Products.AnyAsync(x => x.ProductId == productId, ct);
        if (!productExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm #{productId}.");
        }

        var favorite = await db.MemberFavoriteProducts
            .FirstOrDefaultAsync(x => x.MemberId == member.MemberId && x.ProductId == productId, ct);

        if (favorite is not null)
        {
            db.MemberFavoriteProducts.Remove(favorite);
            await db.SaveChangesAsync(ct);
            return new FavoriteToggleResponseDto(productId, false, "Đã xóa khỏi danh sách yêu thích.");
        }

        await db.MemberFavoriteProducts.AddAsync(new MemberFavoriteProduct
        {
            MemberId = member.MemberId,
            ProductId = productId,
            CreatedAt = VnDateTime.Now
        }, ct);

        await db.SaveChangesAsync(ct);
        return new FavoriteToggleResponseDto(productId, true, "Đã thêm vào danh sách yêu thích.");
    }

    public async Task<IReadOnlyList<PurchaseHistoryItemDto>> GetPurchaseHistoryAsync(int accountId, int limit = 10, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        IQueryable<InvoiceHistoryItem> query = db.InvoiceHistoryItems
            .AsNoTracking()
            .Where(x => x.InvoiceHistory!.MemberId == member.MemberId);

        query = query
            .OrderByDescending(x => x.InvoiceHistory!.PurchaseDate)
            .ThenByDescending(x => x.InvoiceHistoryItemId);

        if (limit > 0)
        {
            query = query.Take(limit);
        }

        return await query
            .Select(x => new PurchaseHistoryItemDto(
                x.ProductId,
                x.Product!.ProductName,
                x.UnitPrice,
                x.Product.ImageUrl,
                x.InvoiceHistory!.PurchaseDate,
                x.Quantity))
            .ToListAsync(ct);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<Member> GetMemberByAccountIdAsync(
        int accountId, CancellationToken ct, bool tracking = false)
    {
        var baseQuery = tracking
            ? db.Accounts.Include(a => a.Member)
            : db.Accounts.AsNoTracking().Include(a => a.Member);

        var account = await baseQuery
            .FirstOrDefaultAsync(a => a.AccountId == accountId, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản #{accountId}.");

        return account.Member
            ?? throw new KeyNotFoundException($"Tài khoản #{accountId} chưa có hồ sơ Member.");
    }

    private static MemberHealthPreferencesDto BuildHealthPreferencesDto(
        int memberId,
        IEnumerable<MemberHealthPreference> prefs)
    {
        static MemberHealthPreferenceItemDto ToDto(MemberHealthPreference p) =>
            new(p.HealthTagId, p.HealthTag?.TagName ?? "", p.HealthTag?.TagType ?? "", p.Status);

        var list = prefs.ToList();
        return new MemberHealthPreferencesDto(
            MemberId:   memberId,
            Allergies:  list.Where(p => p.Status == "Allergy").Select(ToDto).ToList(),
            Avoids:     list.Where(p => p.Status == "Avoid").Select(ToDto).ToList(),
            Preferreds: list.Where(p => p.Status == "Preferred").Select(ToDto).ToList());
    }

    public async Task<IReadOnlyList<RecipeDto>> GetPersonalizedMealsAsync(int accountId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        // 1. Get health preferences
        var preferences = await db.MemberHealthPreferences
            .AsNoTracking()
            .Where(mhp => mhp.MemberId == member.MemberId)
            .ToListAsync(ct);

        var allergyTagIds = preferences.Where(p => p.Status == "Allergy").Select(p => p.HealthTagId).ToList();
        var avoidTagIds = preferences.Where(p => p.Status == "Avoid").Select(p => p.HealthTagId).ToList();
        var preferredTagIds = preferences.Where(p => p.Status == "Preferred").Select(p => p.HealthTagId).ToList();

        // 2. Fetch all recipes
        var recipes = await db.MealSuggestions
            .Include(ms => ms.MealItems)
                .ThenInclude(mi => mi.Product)
                    .ThenInclude(p => p!.ProductHealthTags)
            .ToListAsync(ct);

        var personalizedList = new List<(MealSuggestion Recipe, decimal Score)>();

        foreach (var r in recipes)
        {
            bool hasAllergy = false;
            bool hasAvoid = false;
            decimal preferenceBoost = 0;

            foreach (var item in r.MealItems)
            {
                if (item.Product == null) continue;
                var productTagIds = item.Product.ProductHealthTags.Select(pht => pht.HealthTagId).ToList();

                if (allergyTagIds.Intersect(productTagIds).Any())
                {
                    hasAllergy = true;
                    break;
                }
                if (avoidTagIds.Intersect(productTagIds).Any())
                {
                    hasAvoid = true;
                }
                if (preferredTagIds.Intersect(productTagIds).Any())
                {
                    preferenceBoost += 20;
                }
            }

            if (hasAllergy || hasAvoid) continue; // Exclude allergy and avoid tags

            decimal baseScore = r.HealthyScore ?? 50m;
            personalizedList.Add((r, baseScore + preferenceBoost));
        }

        return personalizedList
            .OrderByDescending(x => x.Score)
            .Select(x => new RecipeDto(
                x.Recipe.MealSuggestionId,
                x.Recipe.MealName,
                x.Recipe.Description,
                x.Recipe.YieldPortions,
                x.Recipe.ImageUrl,
                x.Recipe.Calories,
                x.Recipe.HealthyScore.HasValue ? (int)x.Recipe.HealthyScore.Value : null,
                x.Recipe.AlternativeSuggestion))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetPersonalizedProductsAsync(int accountId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        // 1. Load preferences
        var preferences = await db.MemberHealthPreferences
            .AsNoTracking()
            .Where(mhp => mhp.MemberId == member.MemberId)
            .ToListAsync(ct);

        var allergyTagIds = preferences.Where(p => p.Status == "Allergy").Select(p => p.HealthTagId).ToList();
        var avoidTagIds = preferences.Where(p => p.Status == "Avoid").Select(p => p.HealthTagId).ToList();
        var preferredTagIds = preferences.Where(p => p.Status == "Preferred").Select(p => p.HealthTagId).ToList();

        // 2. Fetch past purchases
        var pastPurchases = await db.InvoiceHistoryItems
            .AsNoTracking()
            .Where(i => i.InvoiceHistory != null && i.InvoiceHistory.MemberId == member.MemberId)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Frequency = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => x.Frequency, ct);

        // 3. Load products with tags
        var products = await db.Products
            .Include(p => p.ProductHealthTags)
            .Where(p => p.Status == "Available")
            .ToListAsync(ct);

        var rankedProducts = new List<(Product Product, decimal Score)>();

        foreach (var p in products)
        {
            var productTagIds = p.ProductHealthTags.Select(pht => pht.HealthTagId).ToList();

            if (allergyTagIds.Intersect(productTagIds).Any() || avoidTagIds.Intersect(productTagIds).Any())
                continue; // Exclude allergy or avoid tags

            decimal score = 0;
            if (pastPurchases.TryGetValue(p.ProductId, out var freq))
            {
                score += freq * 10;
            }

            if (preferredTagIds.Intersect(productTagIds).Any())
            {
                score += 15;
            }

            if (p.PromotionPrice.HasValue && p.PromotionPrice < p.UnitPrice)
            {
                score += 5;
            }

            rankedProducts.Add((p, score));
        }

        return rankedProducts
            .OrderByDescending(x => x.Score)
            .Take(20)
            .Select(x => new ProductDto(
                x.Product.ProductId,
                x.Product.ProductName,
                x.Product.UnitPrice,
                x.Product.Status,
                x.Product.ImageUrl,
                x.Product.ProductTypeId))
            .ToList();
    }

    // ── Avatar ───────────────────────────────────────────────────────────────

    public async Task<AvatarUploadResponseDto> UploadAvatarAsync(
        int accountId, Stream imageStream, string fileName, CancellationToken ct = default)
    {
        var account = await db.Accounts.FindAsync([accountId], ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản #{accountId}.");

        // Đọc stream → byte[]
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);
        var imageBytes = ms.ToArray();

        if (imageBytes.Length == 0)
            throw new InvalidOperationException("File ảnh rỗng.");

        // Giới hạn kích thước 5MB
        const int MaxBytes = 5 * 1024 * 1024;
        if (imageBytes.Length > MaxBytes)
            throw new InvalidOperationException("Ảnh không được vượt quá 5MB.");

        // Upload lên Cloudinary
        var publicId = $"avatar-{accountId}-{Guid.NewGuid():N}";
        var avatarUrl = await cloudStorage.UploadImageAsync(
            imageBytes,
            _cloudinaryOpts.MemberAvatarsFolder,
            publicId,
            ct);

        account.AvatarUrl = avatarUrl;
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "[MemberService] Avatar updated for AccountId={Id}. Url={Url}",
            accountId, avatarUrl);

        return new AvatarUploadResponseDto(
            AccountId: accountId,
            AvatarUrl: avatarUrl,
            Message: "Cập nhật ảnh đại diện thành công.");
    }

    public async Task DeleteAvatarAsync(int accountId, CancellationToken ct = default)
    {
        var account = await db.Accounts.FindAsync([accountId], ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản #{accountId}.");

        account.AvatarUrl = null;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[MemberService] Avatar removed for AccountId={Id}", accountId);
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    public async Task<NotificationListDto> GetNotificationsAsync(
        int accountId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        pageSize = Math.Clamp(pageSize, 1, 50);
        page     = Math.Max(1, page);

        var query = db.MemberNotifications
            .AsNoTracking()
            .Where(n => n.MemberId == member.MemberId);

        var totalCount   = await query.CountAsync(ct);
        var unreadCount  = await query.CountAsync(n => !n.IsRead, ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new MemberNotificationDto(
                n.NotificationId,
                n.NotifType,
                n.Title,
                n.Message,
                n.PayloadJson,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync(ct);

        return new NotificationListDto(
            MemberId:    member.MemberId,
            UnreadCount: unreadCount,
            TotalCount:  totalCount,
            Page:        page,
            PageSize:    pageSize,
            Items:       items);
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(int accountId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        var count = await db.MemberNotifications
            .AsNoTracking()
            .CountAsync(n => n.MemberId == member.MemberId && !n.IsRead, ct);

        return new UnreadCountDto(member.MemberId, count);
    }

    public async Task MarkAllNotificationsReadAsync(int accountId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        await db.MemberNotifications
            .Where(n => n.MemberId == member.MemberId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

        logger.LogInformation("[MemberService] MarkAllRead for MemberId={Id}", member.MemberId);
    }

    public async Task MarkNotificationReadAsync(int accountId, int notificationId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);

        var notif = await db.MemberNotifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.MemberId == member.MemberId, ct)
            ?? throw new KeyNotFoundException($"Notification #{notificationId} không tồn tại.");

        if (!notif.IsRead)
        {
            notif.IsRead = true;
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Internal Helper ───────────────────────────────────────────────────────
}


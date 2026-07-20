using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Search;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class SearchService(
    AppDbContext db,
    IGeminiService geminiService,
    ILogger<SearchService> logger) : ISearchService
{
    private IQueryable<Product> BuildBaseSearchQuery(string query)
    {
        var lowerQuery = (query ?? string.Empty).Trim().ToLowerInvariant();
        var productsQuery = db.Products
            .AsNoTracking()
            .Include(p => p.ProductType)
                .ThenInclude(pt => pt!.Subcategory)
                    .ThenInclude(sc => sc!.Category)
            .Include(p => p.ProductHealthTags)
                .ThenInclude(ph => ph.HealthTag)
            .Where(p => p.Status == "Available");

        if (!string.IsNullOrEmpty(lowerQuery))
        {
            productsQuery = productsQuery.Where(p =>
                p.ProductName.ToLower().Contains(lowerQuery)
                || (p.Description != null && p.Description.ToLower().Contains(lowerQuery))
                || (p.ProductType != null && p.ProductType.TypeName.ToLower().Contains(lowerQuery))
                || (p.ProductType != null && p.ProductType.Subcategory != null
                    && p.ProductType.Subcategory.SubcategoryName.ToLower().Contains(lowerQuery))
                || (p.ProductType != null && p.ProductType.Subcategory != null
                    && p.ProductType.Subcategory.Category != null
                    && p.ProductType.Subcategory.Category.CategoryName.ToLower().Contains(lowerQuery)));
        }

        return productsQuery;
    }

    public async Task<SearchResponseDto> SearchAllAsync(string query, int limit, string sortBy, bool useAi, CancellationToken ct = default)
    {
        var cleanQuery = (query ?? string.Empty).Trim();
        var lowerQuery = cleanQuery.ToLowerInvariant();
        var finalLimit = limit <= 0 ? 20 : Math.Min(limit, 100);

        var productsQuery = BuildBaseSearchQuery(cleanQuery);

        var rawResults = await productsQuery
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Description,
                p.UnitPrice,
                p.PromotionPrice,
                p.ImageUrl,
                p.Status,
                CategoryName = p.ProductType!.Subcategory!.Category!.CategoryName,
                SubcategoryName = p.ProductType.Subcategory.SubcategoryName,
                ProductTypeName = p.ProductType.TypeName,
                HealthTags = p.ProductHealthTags
                    .Select(ph => new { ph.HealthTagId, ph.HealthTag!.TagName }).ToList(),
                AisleCode = p.ProductSlots.Select(ps => ps.Slot!.Shelf!.Aisle!.AisleCode).FirstOrDefault(),
                LevelNumber = p.ProductSlots.Select(ps => (int?)ps.Slot!.Shelf!.LevelNumber).FirstOrDefault(),
                SlotCode = p.ProductSlots.Select(ps => ps.Slot!.SlotCode).FirstOrDefault()
            })
            .ToListAsync(ct);

        // Chấm điểm relevance score
        var scored = rawResults.Select(r =>
        {
            double score = 0;
            if (!string.IsNullOrEmpty(cleanQuery))
            {
                var name = r.ProductName.ToLowerInvariant();
                if (name == lowerQuery) score += 1.0;
                else if (name.StartsWith(lowerQuery)) score += 0.8;
                else if (name.Contains(lowerQuery)) score += 0.6;

                if (!string.IsNullOrEmpty(r.Description) && r.Description.ToLowerInvariant().Contains(lowerQuery))
                    score += 0.2;
                if (!string.IsNullOrEmpty(r.ProductTypeName) && r.ProductTypeName.ToLowerInvariant().Contains(lowerQuery))
                    score += 0.3;
                if (!string.IsNullOrEmpty(r.SubcategoryName) && r.SubcategoryName.ToLowerInvariant().Contains(lowerQuery))
                    score += 0.2;
                if (!string.IsNullOrEmpty(r.CategoryName) && r.CategoryName.ToLowerInvariant().Contains(lowerQuery))
                    score += 0.2;
            }
            else
            {
                score = 0.5;
            }

            return new { r, score };
        }).ToList();

        // Sắp xếp
        scored = sortBy switch
        {
            "price_asc" => scored.OrderBy(x => x.r.UnitPrice).ToList(),
            "price_desc" => scored.OrderByDescending(x => x.r.UnitPrice).ToList(),
            "newest" => scored.OrderByDescending(x => x.r.ProductId).ToList(),
            _ => scored.OrderByDescending(x => x.score).ToList()
        };

        // AI rerank (Gemini)
        string? aiExplanation = null;
        if (useAi && !string.IsNullOrEmpty(cleanQuery) && scored.Count > 1)
        {
            try
            {
                var productNames = scored.Take(20).Select(x => $"{x.r.ProductId}:{x.r.ProductName}").ToList();
                aiExplanation = await geminiService.RerankAndExplainAsync(cleanQuery, productNames, null, ct);

                if (!string.IsNullOrEmpty(aiExplanation))
                {
                    var ids = aiExplanation
                        .Split(new[] { ',', ';', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim().TrimStart("id:".ToCharArray()), out var id) ? id : 0)
                        .Where(id => id > 0)
                        .ToList();

                    if (ids.Count > 0)
                    {
                        var orderMap = ids
                            .Select((id, idx) => new { id, idx })
                            .ToDictionary(x => x.id, x => x.idx);
                        scored = scored
                            .OrderBy(x => orderMap.ContainsKey(x.r.ProductId) ? orderMap[x.r.ProductId] : int.MaxValue)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[SearchService] SearchAll AI rerank failed, falling back to DB score");
            }
        }

        var results = scored
            .Take(finalLimit)
            .Select(x => new SearchResultItemDto(
                x.r.ProductId,
                x.r.ProductName,
                x.r.Description,
                x.r.UnitPrice,
                x.r.PromotionPrice,
                x.r.ImageUrl,
                x.r.Status,
                x.r.CategoryName,
                x.r.SubcategoryName,
                x.r.ProductTypeName,
                Math.Min(1.0, x.score),
                x.r.HealthTags.Select(t => t.TagName).ToList(),
                x.r.AisleCode,
                x.r.LevelNumber,
                x.r.SlotCode
            ))
            .ToList();

        return new SearchResponseDto(
            cleanQuery,
            scored.Count,
            results,
            useAi && !string.IsNullOrEmpty(aiExplanation),
            aiExplanation);
    }

    public async Task<SearchResponseDto> SearchPersonalizedAsync(int accountId, string query, int limit, string sortBy, bool useAi, CancellationToken ct = default)
    {
        // 1. Lấy thông tin Member
        var member = await db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.AccountId == accountId, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ hội viên cho tài khoản #{accountId}.");

        return await SearchPersonalizedCoreAsync(member, query, limit, sortBy, useAi, ct);
    }

    public async Task<SearchResponseDto> SearchPersonalizedByMemberIdAsync(int memberId, string query, int limit, string sortBy, bool useAi, CancellationToken ct = default)
    {
        // 1. Lấy thông tin Member
        var member = await db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemberId == memberId, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ hội viên cho member #{memberId}.");

        return await SearchPersonalizedCoreAsync(member, query, limit, sortBy, useAi, ct);
    }

    private async Task<SearchResponseDto> SearchPersonalizedCoreAsync(Member member, string query, int limit, string sortBy, bool useAi, CancellationToken ct)
    {
        var cleanQuery = (query ?? string.Empty).Trim();
        var lowerQuery = cleanQuery.ToLowerInvariant();
        var finalLimit = limit <= 0 ? 20 : Math.Min(limit, 100);

        // 2. Lấy sở thích sức khỏe (MemberHealthPreferences)
        var preferences = await db.MemberHealthPreferences
            .AsNoTracking()
            .Include(p => p.HealthTag)
            .Where(p => p.MemberId == member.MemberId)
            .ToListAsync(ct);

        var allergyTagIds = preferences
            .Where(p => string.Equals(p.Status, "Allergy", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(p.Status, "Avoid", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.HealthTagId)
            .ToHashSet();

        var preferredTagIds = preferences
            .Where(p => string.Equals(p.Status, "Preferred", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(p.Status, "Diet", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.HealthTagId)
            .ToHashSet();

        // Build base search query
        var productsQuery = BuildBaseSearchQuery(cleanQuery);

        var rawResults = await productsQuery
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Description,
                p.UnitPrice,
                p.PromotionPrice,
                p.ImageUrl,
                p.Status,
                CategoryName = p.ProductType!.Subcategory!.Category!.CategoryName,
                SubcategoryName = p.ProductType.Subcategory.SubcategoryName,
                ProductTypeName = p.ProductType.TypeName,
                HealthTags = p.ProductHealthTags
                    .Select(ph => new { ph.HealthTagId, ph.HealthTag!.TagName }).ToList(),
                AisleCode = p.ProductSlots.Select(ps => ps.Slot!.Shelf!.Aisle!.AisleCode).FirstOrDefault(),
                LevelNumber = p.ProductSlots.Select(ps => (int?)ps.Slot!.Shelf!.LevelNumber).FirstOrDefault(),
                SlotCode = p.ProductSlots.Select(ps => ps.Slot!.SlotCode).FirstOrDefault()
            })
            .ToListAsync(ct);

        // 3. Hard Filter: Loại bỏ dị ứng/tránh xa
        if (allergyTagIds.Count > 0)
        {
            rawResults = rawResults
                .Where(r => !r.HealthTags.Any(t => allergyTagIds.Contains(t.HealthTagId)))
                .ToList();
        }

        // 3. Hard Filter: SpendingLimit
        if (member.SpendingLimit.HasValue && member.SpendingLimit.Value > 0)
        {
            rawResults = rawResults
                .Where(r => (r.PromotionPrice ?? r.UnitPrice) <= member.SpendingLimit.Value)
                .ToList();
        }

        // 4. Soft Boost
        var scored = rawResults.Select(r =>
        {
            double score = 0;
            if (!string.IsNullOrEmpty(cleanQuery))
            {
                var name = r.ProductName.ToLowerInvariant();
                if (name == lowerQuery) score += 1.0;
                else if (name.StartsWith(lowerQuery)) score += 0.8;
                else if (name.Contains(lowerQuery)) score += 0.6;

                if (!string.IsNullOrEmpty(r.Description) && r.Description.ToLowerInvariant().Contains(lowerQuery))
                    score += 0.2;
                if (!string.IsNullOrEmpty(r.ProductTypeName) && r.ProductTypeName.ToLowerInvariant().Contains(lowerQuery))
                    score += 0.3;
                if (!string.IsNullOrEmpty(r.SubcategoryName) && r.SubcategoryName.ToLowerInvariant().Contains(lowerQuery))
                    score += 0.2;
                if (!string.IsNullOrEmpty(r.CategoryName) && r.CategoryName.ToLowerInvariant().Contains(lowerQuery))
                    score += 0.2;
            }
            else
            {
                score = 0.5;
            }

            // Chấm điểm ưu tiên (Soft Boost) cho Preferred/Diet
            if (preferredTagIds.Count > 0)
            {
                var matchesCount = r.HealthTags.Count(t => preferredTagIds.Contains(t.HealthTagId));
                if (matchesCount > 0)
                {
                    score += 0.5;
                }
            }

            return new { r, score };
        }).ToList();

        // 5. Sắp xếp
        scored = sortBy switch
        {
            "price_asc" => scored.OrderBy(x => x.r.UnitPrice).ToList(),
            "price_desc" => scored.OrderByDescending(x => x.r.UnitPrice).ToList(),
            "newest" => scored.OrderByDescending(x => x.r.ProductId).ToList(),
            _ => scored.OrderByDescending(x => x.score).ToList()
        };

        // 5. AI Rerank
        string? aiExplanation = null;
        if (useAi && !string.IsNullOrEmpty(cleanQuery) && scored.Count > 1)
        {
            try
            {
                var productNames = scored.Take(20).Select(x => $"{x.r.ProductId}:{x.r.ProductName}").ToList();

                var spendingLimitStr = member.SpendingLimit.HasValue
                    ? $"{member.SpendingLimit.Value:N0} VNĐ"
                    : "Không giới hạn";

                var preferredTagNames = preferences
                    .Where(p => string.Equals(p.Status, "Preferred", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(p.Status, "Diet", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.HealthTag?.TagName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();

                var dietContext = preferredTagNames.Count > 0
                    ? string.Join(", ", preferredTagNames)
                    : "Không có";

                var personalizedContext = $"Người dùng có ngân sách tối đa: {spendingLimitStr} và chế độ ăn / ưu tiên sức khỏe: {dietContext}.";

                aiExplanation = await geminiService.RerankAndExplainAsync(cleanQuery, productNames, personalizedContext, ct);

                if (!string.IsNullOrEmpty(aiExplanation))
                {
                    var ids = aiExplanation
                        .Split(new[] { ',', ';', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim().TrimStart("id:".ToCharArray()), out var id) ? id : 0)
                        .Where(id => id > 0)
                        .ToList();

                    if (ids.Count > 0)
                    {
                        var orderMap = ids
                            .Select((id, idx) => new { id, idx })
                            .ToDictionary(x => x.id, x => x.idx);
                        scored = scored
                            .OrderBy(x => orderMap.ContainsKey(x.r.ProductId) ? orderMap[x.r.ProductId] : int.MaxValue)
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[SearchService] SearchPersonalized AI rerank failed, falling back to DB score");
            }
        }

        var results = scored
            .Take(finalLimit)
            .Select(x => new SearchResultItemDto(
                x.r.ProductId,
                x.r.ProductName,
                x.r.Description,
                x.r.UnitPrice,
                x.r.PromotionPrice,
                x.r.ImageUrl,
                x.r.Status,
                x.r.CategoryName,
                x.r.SubcategoryName,
                x.r.ProductTypeName,
                Math.Min(1.0, x.score),
                x.r.HealthTags.Select(t => t.TagName).ToList(),
                x.r.AisleCode,
                x.r.LevelNumber,
                x.r.SlotCode
            ))
            .ToList();

        return new SearchResponseDto(
            cleanQuery,
            scored.Count,
            results,
            useAi && !string.IsNullOrEmpty(aiExplanation),
            aiExplanation);
    }
}

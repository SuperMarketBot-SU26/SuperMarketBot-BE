using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Search;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class SearchService(
    AppDbContext db,
    IGeminiService geminiService,
    ILogger<SearchService> logger) : ISearchService
{
    public async Task<SearchResponseDto> SearchAsync(SearchRequestDto request, CancellationToken ct = default)
    {
        var query = (request.Query ?? string.Empty).Trim();
        var limit = request.Limit <= 0 ? 20 : Math.Min(request.Limit, 100);

        // ── 1. Lấy danh sách HealthTag cần tránh (nếu có MemberId) ──
        var memberAllergyTagIds = new HashSet<int>();
        if (request.MemberId is int mid && mid > 0)
        {
            memberAllergyTagIds = (await db.MemberHealthPreferences
                .Where(p => p.MemberId == mid && p.Status == "Allergic")
                .Select(p => p.HealthTagId)
                .ToListAsync(ct))
                .ToHashSet();
        }

        // ── 2. Query sản phẩm — match theo tên / mô tả / category ──
        var lowerQuery = query.ToLowerInvariant();
        var productsQuery = db.Products
            .AsNoTracking()
            .Include(p => p.ProductType)
                .ThenInclude(pt => pt!.Subcategory)
                    .ThenInclude(sc => sc!.Category)
            .Include(p => p.ProductHealthTags)
                .ThenInclude(ph => ph.HealthTag)
            .Where(p => p.Status == "Available");

        if (!string.IsNullOrEmpty(query))
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
                    .Select(ph => new { ph.HealthTagId, ph.HealthTag!.TagName }).ToList()
            })
            .ToListAsync(ct);

        // ── 3. Lọc bỏ sản phẩm chứa dị ứng của Member ──
        if (memberAllergyTagIds.Count > 0)
        {
            rawResults = rawResults
                .Where(r => !r.HealthTags.Any(t => memberAllergyTagIds.Contains(t.HealthTagId)))
                .ToList();
        }

        // ── 4. Tính relevance score (DB-side) ──
        var scored = rawResults.Select(r =>
        {
            double score = 0;
            if (!string.IsNullOrEmpty(query))
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
                score = 0.5; // không có query → trả về Available mới nhất
            }

            return new { r, score };
        }).ToList();

        // ── 5. Sort ──
        scored = request.SortBy switch
        {
            "price_asc" => scored.OrderBy(x => x.r.UnitPrice).ToList(),
            "price_desc" => scored.OrderByDescending(x => x.r.UnitPrice).ToList(),
            "newest" => scored.OrderByDescending(x => x.r.ProductId).ToList(),
            _ => scored.OrderByDescending(x => x.score).ToList()
        };

        // ── 6. AI rerank (optional, dùng Gemini) ──
        string? aiExplanation = null;
        if (request.UseAiRanking && !string.IsNullOrEmpty(query) && scored.Count > 1)
        {
            try
            {
                var productNames = scored.Take(20).Select(x => $"{x.r.ProductId}:{x.r.ProductName}").ToList();
                aiExplanation = await geminiService.RerankAndExplainAsync(query, productNames, ct);

                // Gemini trả về chuỗi "id1,id2,id3,..." — áp dụng lại thứ tự
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
                logger.LogWarning(ex, "[SearchService] AI rerank failed, falling back to DB score");
            }
        }

        // ── 7. Map DTO + cắt limit ──
        var results = scored
            .Take(limit)
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
                x.r.HealthTags.Select(t => t.TagName).ToList()
            ))
            .ToList();

        return new SearchResponseDto(
            request.Query,
            scored.Count,
            results,
            request.UseAiRanking && !string.IsNullOrEmpty(aiExplanation),
            aiExplanation);
    }
}

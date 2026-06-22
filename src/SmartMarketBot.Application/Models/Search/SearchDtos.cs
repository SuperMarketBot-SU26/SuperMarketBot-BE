namespace SmartMarketBot.Application.Models.Search;

/// <summary>
/// Request tìm kiếm sản phẩm từ client (FE / Mobile).
/// </summary>
public sealed record SearchRequestDto(
    /// <summary>Từ khóa tìm kiếm (tên sản phẩm, mô tả, category...).</summary>
    string Query,

    /// <summary>Nếu có → lọc bỏ sản phẩm chứa health-tag dị ứng của member.</summary>
    int? MemberId,

    /// <summary>Số kết quả trả về tối đa (default 20).</summary>
    int Limit = 20,

    /// <summary>Sắp xếp: relevance | price_asc | price_desc | newest.</summary>
    string SortBy = "relevance",

    /// <summary>Có dùng Gemini AI để rerank theo ngữ nghĩa không (default false).</summary>
    bool UseAiRanking = false
);

public sealed record SearchResultItemDto(
    int ProductId,
    string ProductName,
    string? Description,
    decimal UnitPrice,
    decimal? PromotionPrice,
    string? ImageUrl,
    string Status,
    string? CategoryName,
    string? SubcategoryName,
    string? ProductTypeName,
    /// <summary>Điểm relevance (0-1) từ DB-side + AI rerank.</summary>
    double RelevanceScore,
    /// <summary>Các health-tag của sản phẩm (để client hiển thị icon dị ứng).</summary>
    IReadOnlyList<string> HealthTags
);

public sealed record SearchResponseDto(
    string Query,
    int TotalMatches,
    IReadOnlyList<SearchResultItemDto> Results,
    bool AiRanked,
    string? AiExplanation
);

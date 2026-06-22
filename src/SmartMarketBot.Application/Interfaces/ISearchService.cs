using SmartMarketBot.Application.Models.Search;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// Tìm kiếm sản phẩm thông minh theo DB khách hàng:
/// - Hỗ trợ free-text query (tên sản phẩm, mô tả)
/// - Lọc theo MemberId: bỏ sản phẩm chứa dị ứng / theo lịch sử mua
/// - Có thể bật/tắt AI ranking (Gemini) để sắp xếp theo ngữ nghĩa
/// </summary>
public interface ISearchService
{
    Task<SearchResponseDto> SearchAsync(SearchRequestDto request, CancellationToken ct = default);
}

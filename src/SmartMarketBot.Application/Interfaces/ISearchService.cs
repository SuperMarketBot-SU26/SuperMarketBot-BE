using SmartMarketBot.Application.Models.Search;

namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// Tìm kiếm sản phẩm thông minh:
/// - SearchAllAsync: Tìm kiếm công cộng
/// - SearchPersonalizedAsync: Tìm kiếm cá nhân hóa theo chế độ ăn và ngân sách của Member (qua AccountId)
/// - SearchPersonalizedByMemberIdAsync: Tìm kiếm cá nhân hóa theo MemberId (tương thích ngược)
/// </summary>
public interface ISearchService
{
    Task<SearchResponseDto> SearchAllAsync(string query, int limit, string sortBy, bool useAi, CancellationToken ct = default);
    Task<SearchResponseDto> SearchPersonalizedAsync(int accountId, string query, int limit, string sortBy, bool useAi, CancellationToken ct = default);
    Task<SearchResponseDto> SearchPersonalizedByMemberIdAsync(int memberId, string query, int limit, string sortBy, bool useAi, CancellationToken ct = default);
}

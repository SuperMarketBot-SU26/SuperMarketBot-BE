namespace SmartMarketBot.Application.Interfaces;

public interface IGeminiService
{
    /// <summary>Sinh câu chào mừng cá nhân hóa sử dụng Google Gemini</summary>
    Task<string> GeneratePersonalizedGreetingAsync(string fullName, string topProducts, CancellationToken ct = default);

    /// <summary>
    /// Sắp xếp lại danh sách sản phẩm theo ngữ nghĩa query người dùng.
    /// Trả về chuỗi danh sách ProductId theo thứ tự relevance giảm dần (vd "12,5,8,1").
    /// </summary>
    Task<string> RerankAndExplainAsync(string query, IReadOnlyList<string> productIdAndNames, CancellationToken ct = default);
}

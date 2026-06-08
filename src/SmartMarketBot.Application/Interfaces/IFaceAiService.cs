namespace SmartMarketBot.Application.Interfaces;

public interface IFaceAiService
{
    /// <summary>Gọi Python service để xác thực khuôn mặt từ ảnh Base64</summary>
    Task<FaceVerifyResultDto?> VerifyFaceAsync(string imageBase64, CancellationToken ct = default);
}

public sealed record FaceVerifyResultDto(string Status, int MemberId, double ConfidenceScore);

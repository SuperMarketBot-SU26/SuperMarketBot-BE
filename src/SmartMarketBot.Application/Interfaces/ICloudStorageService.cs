namespace SmartMarketBot.Application.Interfaces;

/// <summary>
/// Upload ảnh lên Cloudinary và trả về secure URL.
/// Nếu chưa cấu hình Cloudinary, fallback về local filesystem (storage/...).
/// </summary>
public interface ICloudStorageService
{
    /// <summary>Upload bytes (jpg/png) lên Cloudinary, trả về public URL.</summary>
    Task<string> UploadImageAsync(
        byte[] imageBytes,
        string folder,
        string fileName,
        CancellationToken ct = default);

    /// <summary>Upload base64 (kèm prefix "data:image/...;base64,") lên Cloudinary.</summary>
    Task<string> UploadBase64Async(
        string imageBase64OrDataUri,
        string folder,
        string fileName,
        CancellationToken ct = default);
}

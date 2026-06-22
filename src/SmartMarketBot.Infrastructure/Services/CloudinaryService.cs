using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure.Options;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// Upload ảnh lên Cloudinary thông qua REST API.
/// Hỗ trợ cả signed upload (cần API key/secret) và unsigned upload (cần upload_preset đã tạo trên dashboard).
/// Tự động chọn mode theo cấu hình. Nếu cả 2 fail hoặc chưa cấu hình → fallback local filesystem.
/// </summary>
public sealed class CloudinaryService(
    HttpClient httpClient,
    IOptions<CloudinaryOptions> options,
    ILogger<CloudinaryService> logger) : ICloudStorageService
{
    private readonly CloudinaryOptions _options = options.Value;

    public async Task<string> UploadImageAsync(
        byte[] imageBytes,
        string folder,
        string fileName,
        CancellationToken ct = default)
    {
        if (imageBytes is null || imageBytes.Length == 0)
            throw new ArgumentException("imageBytes is empty", nameof(imageBytes));

        if (string.IsNullOrWhiteSpace(_options.CloudName))
        {
            logger.LogWarning("[Cloudinary] CloudName missing → fallback to local storage");
            return await SaveLocalAsync(imageBytes, folder, fileName, ct);
        }

        // Ưu tiên signed upload nếu có đủ API key + secret
        if (!string.IsNullOrWhiteSpace(_options.ApiKey) && !string.IsNullOrWhiteSpace(_options.ApiSecret))
        {
            try
            {
                return await UploadSignedAsync(imageBytes, folder, fileName, ct);
            }
            catch (CloudinaryException ex)
            {
                logger.LogWarning(ex, "[Cloudinary] Signed upload failed: {Msg}", ex.Message);
            }
        }

        // Fallback: unsigned upload (cần upload_preset tồn tại trên Cloudinary)
        if (!string.IsNullOrWhiteSpace(_options.UploadPreset))
        {
            try
            {
                return await UploadUnsignedAsync(imageBytes, folder, fileName, ct);
            }
            catch (CloudinaryException ex)
            {
                logger.LogWarning(ex, "[Cloudinary] Unsigned upload failed: {Msg}", ex.Message);
            }
        }

        logger.LogWarning("[Cloudinary] All modes failed → fallback to local storage");
        return await SaveLocalAsync(imageBytes, folder, fileName, ct);
    }

    private async Task<string> UploadSignedAsync(byte[] bytes, string folder, string fileName, CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Signature theo spec: sort params alphabetically, nối =value&, kết thúc bằng api_secret.
        // Chỉ các params gửi lên (không tính api_key, file, signature).
        var signatureBase = $"folder={folder}&timestamp={timestamp}{_options.ApiSecret}";
        using var sha1 = SHA1.Create();
        var sig = Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(signatureBase))).ToLowerInvariant();

        using var form = new MultipartFormDataContent
        {
            { new StringContent(_options.ApiKey), "api_key" },
            { new StringContent(timestamp.ToString()), "timestamp" },
            { new StringContent(folder), "folder" },
            { new StringContent(sig), "signature" },
            { new ByteArrayContent(bytes), "file", $"{fileName}.jpg" }
        };

        var url = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/image/upload";
        using var response = await httpClient.PostAsync(url, form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new CloudinaryException($"Signed upload {response.StatusCode}: {body}");

        return ExtractSecureUrl(body, "Signed");
    }

    private async Task<string> UploadUnsignedAsync(byte[] bytes, string folder, string fileName, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(_options.UploadPreset), "upload_preset" },
            { new StringContent(folder), "folder" },
            { new ByteArrayContent(bytes), "file", $"{fileName}.jpg" }
        };

        var url = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/image/upload";
        using var response = await httpClient.PostAsync(url, form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new CloudinaryException($"Unsigned upload {response.StatusCode}: {body}");

        return ExtractSecureUrl(body, "Unsigned");
    }

    private string ExtractSecureUrl(string body, string mode)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var secureUrl = doc.RootElement.GetProperty("secure_url").GetString();
        logger.LogInformation("[Cloudinary] {Mode} upload OK: {Url}", mode, secureUrl);
        return secureUrl ?? throw new CloudinaryException("Response missing secure_url");
    }

    public Task<string> UploadBase64Async(
        string imageBase64OrDataUri,
        string folder,
        string fileName,
        CancellationToken ct = default)
    {
        var b64 = imageBase64OrDataUri;
        if (b64.Contains(','))
            b64 = b64.Split(',')[1];
        var bytes = Convert.FromBase64String(b64);
        return UploadImageAsync(bytes, folder, fileName, ct);
    }

    private async Task<string> SaveLocalAsync(byte[] bytes, string folder, string fileName, CancellationToken ct)
    {
        var dir = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "storage", folder));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{fileName}.jpg");
        await File.WriteAllBytesAsync(path, bytes, ct);
        return $"storage/{folder}/{fileName}.jpg";
    }

    private sealed class CloudinaryException : Exception
    {
        public CloudinaryException(string message) : base(message) { }
    }
}

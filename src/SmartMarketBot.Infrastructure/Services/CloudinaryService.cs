using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Infrastructure.Options;

namespace SmartMarketBot.Infrastructure.Services;

/// <summary>
/// Upload ảnh lên Cloudinary thông qua REST API.
/// Tự build multipart raw bytes (thay vì MultipartFormDataContent của .NET) để
/// giống hệt format curl khi test thủ công — tránh Cloudinary parse sai form.
/// Thử unsigned trước (cần upload_preset ở mode Unsigned), fallback sang signed.
/// Nếu cả 2 fail → fallback local filesystem.
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

        // 1. Unsigned upload trước
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

        // 2. Fallback signed
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

        logger.LogWarning("[Cloudinary] All modes failed → fallback to local storage");
        return await SaveLocalAsync(imageBytes, folder, fileName, ct);
    }

    private async Task<string> UploadSignedAsync(byte[] bytes, string folder, string fileName, CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signatureBase = $"folder={folder}&timestamp={timestamp}{_options.ApiSecret}";
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var sig = Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(signatureBase))).ToLowerInvariant();

        var body = BuildMultipart(
            new[]
            {
                ("api_key", _options.ApiKey),
                ("timestamp", timestamp.ToString()),
                ("folder", folder),
                ("signature", sig)
            },
            ("file", $"{fileName}.jpg", "image/jpeg", bytes));

        var url = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/image/upload";
        return await SendAsync(url, body, ct, "Signed");
    }

    private async Task<string> UploadUnsignedAsync(byte[] bytes, string folder, string fileName, CancellationToken ct)
    {
        var body = BuildMultipart(
            new[]
            {
                ("upload_preset", _options.UploadPreset),
                ("folder", folder)
            },
            ("file", $"{fileName}.jpg", "image/jpeg", bytes));

        var url = $"https://api.cloudinary.com/v1_1/{_options.CloudName}/image/upload";
        return await SendAsync(url, body, ct, "Unsigned");
    }

    /// <summary>
    /// Build multipart/form-data thuần (giống format curl) để tránh .NET MultipartFormDataContent
    /// tự thêm Content-Type header cho từng part (gây Cloudinary parse sai form).
    /// </summary>
    private static byte[] BuildMultipart(
        IEnumerable<(string Name, string Value)> stringFields,
        (string Name, string FileName, string ContentType, byte[] Content) fileField)
    {
        var boundary = "----CloudFormBoundary" + Guid.NewGuid().ToString("N");
        var sb = new StringBuilder();

        foreach (var (name, value) in stringFields)
        {
            sb.Append("--").Append(boundary).Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"").Append(name).Append("\"\r\n\r\n");
            sb.Append(value).Append("\r\n");
        }

        sb.Append("--").Append(boundary).Append("\r\n");
        sb.Append("Content-Disposition: form-data; name=\"").Append(fileField.Name)
          .Append("\"; filename=\"").Append(fileField.FileName).Append("\"\r\n");
        sb.Append("Content-Type: ").Append(fileField.ContentType).Append("\r\n\r\n");

        var headerBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var footerBytes = Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");
        var total = new byte[headerBytes.Length + fileField.Content.Length + footerBytes.Length];
        Buffer.BlockCopy(headerBytes, 0, total, 0, headerBytes.Length);
        Buffer.BlockCopy(fileField.Content, 0, total, headerBytes.Length, fileField.Content.Length);
        Buffer.BlockCopy(footerBytes, 0, total, headerBytes.Length + fileField.Content.Length, footerBytes.Length);
        return total;
    }

    private async Task<string> SendAsync(string url, byte[] body, CancellationToken ct, string mode)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(body)
        };
        var boundary = "----CloudFormBoundary"; // prefix; BE build boundary nội bộ
        // Đọc lại boundary từ body[2..] (sau "--") — đơn giản: tìm trong body
        var bMatch = System.Text.RegularExpressions.Regex.Match(
            Encoding.UTF8.GetString(body, 0, Math.Min(200, body.Length)),
            @"----CloudFormBoundary([0-9a-f]+)");
        var actualBoundary = bMatch.Success ? bMatch.Value : boundary;
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse($"multipart/form-data; boundary={actualBoundary}");

        using var response = await httpClient.SendAsync(request, ct);
        var respBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new CloudinaryException($"{mode} upload {response.StatusCode}: {respBody}");

        return ExtractSecureUrl(respBody, mode);
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

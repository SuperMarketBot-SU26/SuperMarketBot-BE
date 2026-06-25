namespace SmartMarketBot.Infrastructure.Options;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    /// <summary>Upload preset (unsigned) — tạo trên Cloudinary dashboard.</summary>
    public string UploadPreset { get; set; } = "smartmarket_unsigned";
    /// <summary>Folder gốc lưu ảnh khuôn mặt member (dùng cho AI face recognition).</summary>
    public string MemberFacesFolder { get; set; } = "member_faces";
    /// <summary>Folder gốc lưu ảnh đại diện hiển thị UI (avatar).</summary>
    public string MemberAvatarsFolder { get; set; } = "member_avatars";
    /// <summary>Folder gốc lưu ảnh quét kệ.</summary>
    public string AisleScansFolder { get; set; } = "aisle_scans";
    /// <summary>Folder gốc lưu ảnh semantic object.</summary>
    public string SemanticFolder { get; set; } = "semantic_objects";

    /// <summary>Có đủ thông tin để upload hay không.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName)
        && !string.IsNullOrWhiteSpace(UploadPreset);
}

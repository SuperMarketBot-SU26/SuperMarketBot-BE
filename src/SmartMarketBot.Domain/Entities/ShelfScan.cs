namespace SmartMarketBot.Domain.Entities;

public class ShelfScan
{
    public int ScanID { get; set; }
    public int AisleID { get; set; }
    public int? ShelfLevelID { get; set; }
    public int RobotID { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public string? ImageUrl { get; set; }
    public decimal EmptyPercentage { get; set; } = 0.00m;

    // Computed column: EmptyPercentage > 30.0 — read-only
    public bool NeedsRestock { get; }

    public string? AiResponseRaw { get; set; }

    /// <summary>Camera bị che khuất do người đứng chắn → robot lên lịch quét lại (Buổi 15)</summary>
    public bool IsOccluded { get; set; } = false;

    /// <summary>Nguyên nhân che khuất (Buổi 15)</summary>
    public string? OcclusionReason { get; set; }

    public virtual Aisle Aisle { get; set; } = null!;
    public virtual ShelfLevel? ShelfLevel { get; set; }
    public virtual Robot Robot { get; set; } = null!;
}

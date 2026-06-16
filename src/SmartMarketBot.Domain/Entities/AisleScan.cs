namespace SmartMarketBot.Domain.Entities;

public class AisleScan
{
    public int ScanId { get; set; }
    public int AisleId { get; set; }
    public int RobotId { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public string? ImageUrl { get; set; }
    public decimal EmptyPercentage { get; set; } = 0.00m;
    // Computed column: needs_restock (AS CASE WHEN EmptyPercentage > 30 THEN 1 ELSE 0)
    public bool NeedsRestock { get; } = false;
    public string? AiResponseRaw { get; set; }

    public virtual Aisle? Aisle { get; set; }
    public virtual Robot? Robot { get; set; }
}

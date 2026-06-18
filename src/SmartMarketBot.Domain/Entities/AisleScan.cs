namespace SmartMarketBot.Domain.Entities;

public class AisleScan
{
    public int ScanId { get; set; }
    public int AisleId { get; set; }
    public int RobotId { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public decimal EmptyPercentage { get; set; } = 0.00m;

    /// <summary>BIT column trong DB - EF sẽ map explicit.</summary>
    public bool NeedsRestock { get; set; } = false;

    public string? ImageUrl { get; set; }

    public virtual Aisle? Aisle { get; set; }
    public virtual Robot? Robot { get; set; }
}

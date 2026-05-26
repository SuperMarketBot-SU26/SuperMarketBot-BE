namespace SmartMarketBot.Domain.Entities;

public class Slot
{
    public int SlotID { get; set; }
    public int ShelfLevelID { get; set; }
    public string SlotCode { get; set; } = string.Empty;
    public int? ProductID { get; set; }
    public int Quantity { get; set; } = 0;
    public DateTime? LastScannedAt { get; set; }

    public virtual ShelfLevel ShelfLevel { get; set; } = null!;
    public virtual Product? Product { get; set; }
}

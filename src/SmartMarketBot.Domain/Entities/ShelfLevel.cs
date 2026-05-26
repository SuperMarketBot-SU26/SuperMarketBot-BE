namespace SmartMarketBot.Domain.Entities;

public class ShelfLevel
{
    public int ShelfLevelID { get; set; }
    public int AisleID { get; set; }
    public int LevelNumber { get; set; }

    public virtual Aisle Aisle { get; set; } = null!;
    public virtual ICollection<Slot> Slots { get; set; } = new List<Slot>();
    public virtual ICollection<ShelfScan> ShelfScans { get; set; } = new List<ShelfScan>();
}

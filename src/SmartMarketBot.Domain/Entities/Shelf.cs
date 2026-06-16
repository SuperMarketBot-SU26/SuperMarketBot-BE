namespace SmartMarketBot.Domain.Entities;

public class Shelf
{
    public int ShelfId { get; set; }
    public int AisleId { get; set; }
    public int LevelNumber { get; set; }

    public virtual Aisle? Aisle { get; set; }
    public virtual ICollection<Slot> Slots { get; set; } = new List<Slot>();
}

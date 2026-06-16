namespace SmartMarketBot.Domain.Entities;

public class ProductSlot
{
    public int ProductSlotId { get; set; }
    public int SlotId { get; set; }
    public int ProductId { get; set; }

    public virtual Slot? Slot { get; set; }
    public virtual Product? Product { get; set; }
}

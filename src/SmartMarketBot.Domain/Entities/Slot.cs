namespace SmartMarketBot.Domain.Entities;

public class Slot
{
    public int SlotId { get; set; }
    public int ShelfId { get; set; }
    public string SlotCode { get; set; } = string.Empty;
    public int Quantity { get; set; } = 0;
    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }
    public DateTime? LastScannedAt { get; set; }

    public virtual Shelf? Shelf { get; set; }
    public virtual ICollection<ProductSlot> ProductSlots { get; set; } = new List<ProductSlot>();
}

using System;

namespace SmartMarketBot.Domain.Entities;

public class CartItem
{
    public int CartItemId { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public virtual Cart? Cart { get; set; }
    public virtual Product? Product { get; set; }
}

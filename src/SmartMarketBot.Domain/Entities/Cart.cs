using System;
using System.Collections.Generic;

namespace SmartMarketBot.Domain.Entities;

public class Cart
{
    public int CartId { get; set; }
    public int MemberId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Member? Member { get; set; }
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}

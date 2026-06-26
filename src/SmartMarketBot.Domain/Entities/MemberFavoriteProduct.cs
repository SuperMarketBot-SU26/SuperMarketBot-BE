namespace SmartMarketBot.Domain.Entities;

public class MemberFavoriteProduct
{
    public int FavoriteId { get; set; }
    public int MemberId { get; set; }
    public int ProductId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Member? Member { get; set; }
    public virtual Product? Product { get; set; }
}

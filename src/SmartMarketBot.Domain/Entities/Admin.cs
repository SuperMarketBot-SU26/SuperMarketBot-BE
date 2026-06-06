namespace SmartMarketBot.Domain.Entities;

public class Admin
{
    public int AdminID { get; set; }
    public int AccountID { get; set; }

    public virtual Account Account { get; set; } = null!;
}

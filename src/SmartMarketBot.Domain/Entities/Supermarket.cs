namespace SmartMarketBot.Domain.Entities;

public class Supermarket
{
    public int SupermarketID { get; set; }
    public string SupermarketName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Status { get; set; } = "Active";

    public virtual ICollection<Floor> Floors { get; set; } = new List<Floor>();
}

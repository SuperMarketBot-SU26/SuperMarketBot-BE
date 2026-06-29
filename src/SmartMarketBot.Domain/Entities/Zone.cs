namespace SmartMarketBot.Domain.Entities;

public class Zone
{
    public int ZoneId { get; set; }
    public int FloorId { get; set; }
    public string? ZoneName { get; set; }
    public string? Description { get; set; }

    public virtual Floor? Floor { get; set; }
    public virtual ICollection<Aisle> Aisles { get; set; } = new List<Aisle>();
}

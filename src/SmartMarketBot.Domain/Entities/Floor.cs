namespace SmartMarketBot.Domain.Entities;

public class Floor
{
    public int FloorID { get; set; }
    public int FloorNumber { get; set; }

    public virtual ICollection<Zone> Zones { get; set; } = new List<Zone>();
    public virtual ICollection<Map> Maps { get; set; } = new List<Map>();
}

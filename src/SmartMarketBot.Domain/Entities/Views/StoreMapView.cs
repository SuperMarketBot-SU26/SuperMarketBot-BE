namespace SmartMarketBot.Domain.Entities.Views;

public class StoreMapView
{
    public int MapID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ShelfLocation { get; set; }
    public string? Landmark { get; set; }
    public string? AisleNote { get; set; }
}

namespace SmartMarketBot.Domain.Entities.Views;

public class StoreMapView
{
    public int MapID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ShelfLocation { get; set; } = string.Empty;
    public string Landmark { get; set; } = string.Empty;
    public string AisleNote { get; set; } = string.Empty;
}

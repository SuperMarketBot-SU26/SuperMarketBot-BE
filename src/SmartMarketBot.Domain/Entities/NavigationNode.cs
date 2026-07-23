namespace SmartMarketBot.Domain.Entities;

public class NavigationNode
{
    public int NodeId { get; set; }
    public int MapId { get; set; }
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Mã định danh vật lý tại node — dùng cho line-scanning navigation.
    /// Firmware đọc mã này từ RFID/QR/trên tape-line tại vị trí waypoint.
    /// Phase B: thay thế hoàn toàn (x, y) trong MQTT payload cho MODE_LINE.
    /// Schema V4.1: bắt buộc phải có, mặc định rỗng cho row seed cũ (backfill bằng "NODE_{NodeId}").
    /// </summary>
    public string NodeCode { get; set; } = string.Empty;

    // Phase B: vẫn còn để FE render canvas layout — không dùng trong algorithm Dijkstra nữa.
    public double XCoord { get; set; }
    public double YCoord { get; set; }
    public string NodeType { get; set; } = "intersection";
    public bool IsBlocked { get; set; } = false;

    public virtual Map? Map { get; set; }
    public virtual ICollection<NavigationEdge> OutgoingEdges { get; set; } = new List<NavigationEdge>();
    public virtual ICollection<NavigationEdge> IncomingEdges { get; set; } = new List<NavigationEdge>();
    public virtual ICollection<AisleNode> AisleNodes { get; set; } = new List<AisleNode>();
    public virtual ICollection<RouteNodeMapping> RouteNodeMappings { get; set; } = new List<RouteNodeMapping>();
}

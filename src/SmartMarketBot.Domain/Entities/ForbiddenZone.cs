namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Vùng cấm robot đi vào theo toạ độ 2D trên bản đồ.
/// A* pathfinding sẽ đọc bảng này để tránh khu vực thi công, quầy thu ngân, lối thoát hiểm.
/// Buổi 10 — Thầy Đỗ Tấn Nhàn.
/// </summary>
public class ForbiddenZone
{
    public int ForbiddenZoneID { get; set; }
    public int MapID { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    /// <summary>Góc trái dưới (mét)</summary>
    public double XMin { get; set; }
    public double YMin { get; set; }

    /// <summary>Góc phải trên (mét)</summary>
    public double XMax { get; set; }
    public double YMax { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Lý do cấm (VD: 'Khu vực trẻ em', 'Thi công tạm thời')</summary>
    public string? Reason { get; set; }

    public virtual Map Map { get; set; } = null!;
}

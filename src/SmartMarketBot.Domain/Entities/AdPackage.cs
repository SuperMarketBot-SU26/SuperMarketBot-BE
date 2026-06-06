namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Gói quảng cáo được mua bởi Brand — xác định khung giờ, điểm AdScore và giá thầu.
/// </summary>
public class AdPackage
{
    public int PackageID { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>Điểm quảng cáo cơ sở dùng tính Priority Score</summary>
    public int AdScore { get; set; } = 0;

    /// <summary>Khung giờ bắt đầu hiển thị (null = cả ngày)</summary>
    public TimeOnly? TimeSlotStart { get; set; }

    /// <summary>Khung giờ kết thúc hiển thị</summary>
    public TimeOnly? TimeSlotEnd { get; set; }

    /// <summary>Chỉ hiển thị vào cuối tuần</summary>
    public bool IsWeekendOnly { get; set; } = false;

    public virtual ICollection<SponsoredProduct> SponsoredProducts { get; set; } = new List<SponsoredProduct>();
}

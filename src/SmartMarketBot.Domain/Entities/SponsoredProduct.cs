namespace SmartMarketBot.Domain.Entities;

public class SponsoredProduct
{
    public int SponsoredID { get; set; }
    public int ProductID { get; set; }
    public string SponsorBrand { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    /// <summary>Điểm quảng cáo cơ sở của nhãn hàng (Buổi 14)</summary>
    public int AdScore { get; set; } = 0;

    /// <summary>Khung giờ bắt đầu hiển thị (null = cả ngày) (Buổi 16)</summary>
    public TimeOnly? TimeSlotStart { get; set; }

    /// <summary>Khung giờ kết thúc (Buổi 16)</summary>
    public TimeOnly? TimeSlotEnd { get; set; }

    /// <summary>Chỉ hiển thị cuối tuần (Buổi 17)</summary>
    public bool IsWeekendOnly { get; set; } = false;

    /// <summary>Giá đấu thầu mỗi lượt hiển thị (VND) (Buổi 14)</summary>
    public decimal BidPrice { get; set; } = 0.00m;

    /// <summary>Hệ số nhân chi phí cuối tuần/ngày lễ (Buổi 17)</summary>
    public decimal WeekendMultiplier { get; set; } = 1.00m;

    public virtual Product Product { get; set; } = null!;
}

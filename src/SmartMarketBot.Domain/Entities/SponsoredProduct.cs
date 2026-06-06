namespace SmartMarketBot.Domain.Entities;

public class SponsoredProduct
{
    public int SponsoredID { get; set; }
    public int ProductID { get; set; }

    /// <summary>Nhãn hàng tài trợ (ref Brands)</summary>
    public int BrandID { get; set; }

    /// <summary>Gói quảng cáo được mua (ref AdPackages) — xác định AdScore, khung giờ, cuối tuần</summary>
    public int PackageID { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public virtual Product Product { get; set; } = null!;
    public virtual Brand Brand { get; set; } = null!;
    public virtual AdPackage AdPackage { get; set; } = null!;
}

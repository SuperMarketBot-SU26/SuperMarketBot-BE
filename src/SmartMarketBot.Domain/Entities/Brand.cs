namespace SmartMarketBot.Domain.Entities;

public sealed class Brand
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public decimal Wallet { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// True = brand của siêu thị tự chạy quảng cáo khuyến mãi.
    /// SystemBrand không bị trừ Package fee hay click charge khi Activate.
    /// </summary>
    public bool IsSystemBrand { get; set; }

    public ICollection<AdCampaign> AdCampaigns { get; set; } = new List<AdCampaign>();
}

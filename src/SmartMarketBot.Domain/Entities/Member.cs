namespace SmartMarketBot.Domain.Entities;

public class Member
{
    public int MemberId { get; set; }
    public int? AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? FacePath { get; set; }
    public string? FaceVector { get; set; }
    public decimal? SpendingLimit { get; set; }
    public decimal? WarningThreshold { get; set; }
    public int TotalPoints { get; set; } = 0;

    /// <summary>Ngân sách stop-loss (giữ để tương thích code cũ - alias của SpendingLimit).</summary>
    public decimal? ShoppingBudget { get; set; }

    /// <summary>Chế độ tìm kiếm: 'Normal' | 'Healthy' | 'Budget' (giữ cho tương thích code cũ).</summary>
    public string SearchMode { get; set; } = "Normal";

    /// <summary>Phân cấp thành viên (alias cho Membership.TierName).</summary>
    public string Tier { get; set; } = "Bronze";

    public virtual Account? Account { get; set; }
    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public virtual ICollection<MemberHealthPreference> MemberHealthPreferences { get; set; } = new List<MemberHealthPreference>();
    public virtual ICollection<InvoiceHistory> InvoiceHistories { get; set; } = new List<InvoiceHistory>();
}

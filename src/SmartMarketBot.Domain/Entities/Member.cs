namespace SmartMarketBot.Domain.Entities;

public class Member
{
    public int MemberId { get; set; }
    public int? AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? FacePath { get; set; }
    public string? FaceVector { get; set; }
    public decimal? SpendingLimit { get; set; }
    public int TotalPoints { get; set; } = 0;

    public virtual Account? Account { get; set; }
    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public virtual ICollection<MemberHealthPreference> MemberHealthPreferences { get; set; } = new List<MemberHealthPreference>();
    public virtual ICollection<InvoiceHistory> InvoiceHistories { get; set; } = new List<InvoiceHistory>();
}

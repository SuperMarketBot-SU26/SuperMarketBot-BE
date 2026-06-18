namespace SmartMarketBot.Domain.Entities;

public class Membership
{
    public int MembershipId { get; set; }
    public int MemberId { get; set; }
    public string TierName { get; set; } = "Bronze";

    /// <summary>Enum dạng string: 'Active' | 'Expired' | 'Cancelled'.</summary>
    public string Status { get; set; } = "Active";

    public virtual Member? Member { get; set; }
}

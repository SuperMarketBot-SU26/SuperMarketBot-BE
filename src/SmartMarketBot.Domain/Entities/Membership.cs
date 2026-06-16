namespace SmartMarketBot.Domain.Entities;

public class Membership
{
    public int MembershipId { get; set; }
    public int MemberId { get; set; }
    public string TierName { get; set; } = "Bronze";
    public int PointsThreshold { get; set; }

    public virtual Member? Member { get; set; }
}

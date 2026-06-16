namespace SmartMarketBot.Domain.Entities;

public class MemberHealthPreference
{
    public int MemberId { get; set; }
    public int HealthTagId { get; set; }
    public bool IsAllergy { get; set; } = false;

    public virtual Member? Member { get; set; }
    public virtual HealthTag? HealthTag { get; set; }
}

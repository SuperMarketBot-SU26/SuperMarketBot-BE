namespace SmartMarketBot.Domain.Entities;

public class MemberHealthPreference
{
    public int MemberID { get; set; }
    public int TagID { get; set; }
    public bool IsAllergy { get; set; } = false;

    public virtual Member Member { get; set; } = null!;
    public virtual HealthTag HealthTag { get; set; } = null!;
}

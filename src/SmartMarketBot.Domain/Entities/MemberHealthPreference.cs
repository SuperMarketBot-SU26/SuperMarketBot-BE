namespace SmartMarketBot.Domain.Entities;

public class MemberHealthPreference
{
    public int MemberId { get; set; }
    public int HealthTagId { get; set; }

    /// <summary>Enum dạng string: 'Allergy' | 'Avoid' | 'Preferred'.</summary>
    public string Status { get; set; } = "Avoid";

    public virtual Member? Member { get; set; }
    public virtual HealthTag? HealthTag { get; set; }
}

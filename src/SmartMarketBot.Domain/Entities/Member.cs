namespace SmartMarketBot.Domain.Entities;

public class Member
{
    public int MemberID { get; set; }
    public int? UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? FacePath { get; set; }
    public string? FaceVector { get; set; }

    // Computed column: AS (FullName) — read-only
    public string MemberName { get; } = string.Empty;

    public string Tier { get; set; } = "Bronze";
    public int TotalPoints { get; set; } = 0;
    public string? Avatar { get; set; }
    public DateTime? TierUpdatedAt { get; set; }

    /// <summary>Chế độ tìm kiếm: 'Normal' | 'Healthy' | 'Budget' (Buổi 16)</summary>
    public string SearchMode { get; set; } = "Normal";

    /// <summary>Ngân sách stop-loss (VND) — null = không giới hạn (Buổi 16)</summary>
    public decimal? ShoppingBudget { get; set; }

    public virtual User? User { get; set; }
    public virtual ICollection<MemberHealthPreference> MemberHealthPreferences { get; set; } = new List<MemberHealthPreference>();
    public virtual ICollection<ShoppingHistory> ShoppingHistories { get; set; } = new List<ShoppingHistory>();
    public virtual ICollection<MemberAlert> MemberAlerts { get; set; } = new List<MemberAlert>();
    public virtual ICollection<MemberEvent> MemberEvents { get; set; } = new List<MemberEvent>();
}

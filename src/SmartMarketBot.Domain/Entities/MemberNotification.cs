namespace SmartMarketBot.Domain.Entities;

public class MemberNotification
{
    public int NotificationId { get; set; }
    public int MemberId { get; set; }

    /// <summary>Allergy | BudgetExceeded | DuplicatePurchase | CartUpdate | PointsEarned | TestNotification</summary>
    public string NotifType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>JSON payload tùy loại (productId, points...). Null nếu không cần.</summary>
    public string? PayloadJson { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual Member? Member { get; set; }
}

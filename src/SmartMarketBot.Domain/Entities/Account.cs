namespace SmartMarketBot.Domain.Entities;

public class Account
{
    public int AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }

    /// <summary>Email đã xác minh qua OTP (giữ lại cho tương thích code cũ).</summary>
    public bool EmailConfirmed { get; set; } = false;

    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Phân quyền: 'Admin' | 'Staff' | 'Member' (string để tương thích ERD V4.0).</summary>
    public string Role { get; set; } = "Member";

    // Navigation
    public virtual Member? Member { get; set; }
    // Không khai báo UserTokens collection ở Account để tránh EF shadow FK 'AccountId1'
}

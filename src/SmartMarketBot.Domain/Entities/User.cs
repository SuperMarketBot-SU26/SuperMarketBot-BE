namespace SmartMarketBot.Domain.Entities;

public class User
{
    public int UserID { get; set; }

    /// <summary>Tên đăng nhập (unique). Có thể để trống khi đăng ký qua email.</summary>
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>Email đã xác minh qua OTP</summary>
    public bool EmailConfirmed { get; set; } = false;

    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual Member? Member { get; set; }
    public virtual Admin? Admin { get; set; }
    public virtual Staff? Staff { get; set; }
}

using SmartMarketBot.Domain.Common;

namespace SmartMarketBot.Domain.Entities;

public enum AccountRole
{
    Admin = 1,
    Staff = 2,
    Member = 3
}

public class Account
{
    public int AccountID { get; set; }

    /// <summary>Tên đăng nhập (unique). Dùng email làm username mặc định.</summary>
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>Email đã xác minh qua OTP</summary>
    public bool EmailConfirmed { get; set; } = false;

    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = VnDateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Phân quyền tĩnh: Admin=1, Staff=2, Member=3</summary>
    public AccountRole Role { get; set; } = AccountRole.Member;

    // Navigation
    public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();
    public virtual Member? Member { get; set; }
    public virtual Admin? Admin { get; set; }
    public virtual Staff? Staff { get; set; }
}

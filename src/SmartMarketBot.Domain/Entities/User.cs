namespace SmartMarketBot.Domain.Entities;

public class User
{
    public int UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual Member? Member { get; set; }
    public virtual Admin? Admin { get; set; }
    public virtual Staff? Staff { get; set; }
}

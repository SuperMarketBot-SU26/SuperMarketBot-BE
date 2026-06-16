namespace SmartMarketBot.Domain.Entities;

/// <summary>
/// Enum role truyền thống (giữ cho code cũ). ERD V4.0 dùng string Role trong ACCOUNT,
/// nhưng các service cũ convert giữa enum và string.
/// </summary>
public enum AccountRole
{
    Admin = 1,
    Staff = 2,
    Member = 3
}

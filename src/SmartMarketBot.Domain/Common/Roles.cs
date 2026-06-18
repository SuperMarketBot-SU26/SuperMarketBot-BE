namespace SmartMarketBot.Domain.Common;

/// <summary>
/// Role strings dùng trong <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute"/>.
/// Phải khớp với giá trị Role trong bảng ACCOUNT (ERD V4.0).
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Member = "Member";

    public const string AdminOrStaff = $"{Admin},{Staff}";
}
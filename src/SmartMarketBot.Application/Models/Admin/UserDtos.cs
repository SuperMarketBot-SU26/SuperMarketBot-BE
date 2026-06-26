using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Admin;

public sealed record UserDto(
    int AccountId,
    string Username,
    string Email,
    string? FullName,
    string? Phone,
    string Status,
    string Role,
    DateTime CreatedAt);

public sealed record CreateUserRequestDto
{
    [Required(ErrorMessage = "Username không được để trống.")]
    [MaxLength(100, ErrorMessage = "Username không được vượt quá 100 ký tự.")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public required string Email { get; init; }

    [Required(ErrorMessage = "Password không được để trống.")]
    [MinLength(8, ErrorMessage = "Password phải có ít nhất 8 ký tự.")]
    public required string Password { get; init; }

    [MaxLength(100, ErrorMessage = "FullName không được vượt quá 100 ký tự.")]
    public string? FullName { get; init; }

    [Phone(ErrorMessage = "Phone không hợp lệ.")]
    public string? Phone { get; init; }

    [Required(ErrorMessage = "Role là bắt buộc.")]
    [MaxLength(50, ErrorMessage = "Role không được vượt quá 50 ký tự.")]
    public string Role { get; init; } = "Member";

    [Required(ErrorMessage = "Status là bắt buộc.")]
    [MaxLength(50, ErrorMessage = "Status không được vượt quá 50 ký tự.")]
    public string Status { get; init; } = "Active";
}

public sealed record UpdateUserRequestDto
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public required string Email { get; init; }

    [MaxLength(100, ErrorMessage = "FullName không được vượt quá 100 ký tự.")]
    public string? FullName { get; init; }

    [Phone(ErrorMessage = "Phone không hợp lệ.")]
    public string? Phone { get; init; }

    [MaxLength(50, ErrorMessage = "Role không được vượt quá 50 ký tự.")]
    public string? Role { get; init; }

    [MaxLength(50, ErrorMessage = "Status không được vượt quá 50 ký tự.")]
    public string? Status { get; init; }
}

public sealed record UpdateUserRoleRequestDto
{
    [Required(ErrorMessage = "Role là bắt buộc.")]
    [MaxLength(50, ErrorMessage = "Role không được vượt quá 50 ký tự.")]
    public required string Role { get; init; }
}

public sealed record UpdateUserStatusRequestDto
{
    [Required(ErrorMessage = "Status là bắt buộc.")]
    [MaxLength(50, ErrorMessage = "Status không được vượt quá 50 ký tự.")]
    public required string Status { get; init; }
}

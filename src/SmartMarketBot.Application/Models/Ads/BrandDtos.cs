using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record BrandDto(
    int BrandId,
    string BrandName,
    decimal Wallet,
    string? Description,
    int ActiveCampaignCount);

public sealed record CreateBrandRequestDto
{
    [Required(ErrorMessage = "BrandName không được để trống.")]
    [MaxLength(100, ErrorMessage = "BrandName không được vượt quá 100 ký tự.")]
    public required string BrandName { get; init; }

    [MaxLength(500, ErrorMessage = "Description không được vượt quá 500 ký tự.")]
    public string? Description { get; init; }
}

public sealed record UpdateBrandRequestDto
{
    [Required(ErrorMessage = "BrandName không được để trống.")]
    [MaxLength(100, ErrorMessage = "BrandName không được vượt quá 100 ký tự.")]
    public required string BrandName { get; init; }

    [MaxLength(500, ErrorMessage = "Description không được vượt quá 500 ký tự.")]
    public string? Description { get; init; }
}

public sealed record TopUpWalletRequestDto
{
    [Required(ErrorMessage = "Amount là bắt buộc.")]
    [Range(0.01, 999999999, ErrorMessage = "Amount phải lớn hơn 0.")]
    public required decimal Amount { get; init; }
}

public sealed record TopUpWalletResponseDto(
    int BrandId,
    decimal PreviousBalance,
    decimal AmountAdded,
    decimal NewBalance);

public sealed record AdminDepositRequestDto
{
    [Required(ErrorMessage = "Amount là bắt buộc.")]
    [Range(0.01, double.PositiveInfinity, ErrorMessage = "Amount phải lớn hơn 0.")]
    public required decimal Amount { get; init; }

    [MaxLength(100, ErrorMessage = "ReferenceNo không được vượt quá 100 ký tự.")]
    public string? ReferenceNo { get; init; }
}

public sealed record AdminDepositResponseDto(
    int BrandId,
    string BrandName,
    decimal PreviousBalance,
    decimal AmountDeposited,
    decimal NewBalance,
    string? ReferenceNo,
    string Message);

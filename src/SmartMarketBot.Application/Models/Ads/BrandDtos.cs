using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record BrandDto(
    int BrandId,
    string BrandName,
    decimal Wallet,
    string? Description,
    int ActiveCampaignCount);

public sealed record CreateBrandRequestDto(
    [Required(ErrorMessage = "BrandName không được để trống.")]
    [MaxLength(100, ErrorMessage = "BrandName không được vượt quá 100 ký tự.")]
    string BrandName,
    
    [MaxLength(500, ErrorMessage = "Description không được vượt quá 500 ký tự.")]
    string? Description);

public sealed record UpdateBrandRequestDto(
    [Required(ErrorMessage = "BrandName không được để trống.")]
    [MaxLength(100, ErrorMessage = "BrandName không được vượt quá 100 ký tự.")]
    string BrandName,
    
    [MaxLength(500, ErrorMessage = "Description không được vượt quá 500 ký tự.")]
    string? Description);

public sealed record TopUpWalletRequestDto(
    [Required(ErrorMessage = "Amount là bắt buộc.")]
    [Range(0.01, 999999999, ErrorMessage = "Amount phải lớn hơn 0.")]
    decimal Amount);

public sealed record TopUpWalletResponseDto(
    int BrandId,
    decimal PreviousBalance,
    decimal AmountAdded,
    decimal NewBalance);

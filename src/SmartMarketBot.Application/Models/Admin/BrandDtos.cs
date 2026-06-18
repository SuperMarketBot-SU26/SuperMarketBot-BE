using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Admin;

public sealed record BrandDto(
    int BrandId,
    string BrandName,
    decimal Wallet,
    string? Description);

public sealed record CreateBrandRequestDto(
    [Required, MinLength(1), MaxLength(100)] string BrandName,
    [Range(0, 100_000_000)] decimal InitialWallet = 0m,
    [MaxLength(500)] string? Description = null);

public sealed record UpdateBrandRequestDto(
    [Required, MinLength(1), MaxLength(100)] string BrandName,
    [Range(0, 1_000_000_000)] decimal? Wallet = null,
    [MaxLength(500)] string? Description = null);
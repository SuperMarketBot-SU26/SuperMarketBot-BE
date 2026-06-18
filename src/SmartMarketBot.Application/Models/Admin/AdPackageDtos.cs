using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Admin;

public sealed record AdPackageDto(
    int PackageId,
    string PackageName,
    decimal PricePackage,
    decimal PriceRoute,
    decimal BasePriceClick,
    int AdScore,
    string Status);

public sealed record CreateAdPackageRequestDto(
    [Required, MinLength(1), MaxLength(100)] string PackageName,
    [Range(0, 1_000_000_000)] decimal PricePackage = 0m,
    [Range(0, 1_000_000_000)] decimal PriceRoute = 0m,
    [Range(0, 1_000_000_000)] decimal BasePriceClick = 0m,
    [Range(0, 1000)] int AdScore = 0,
    [MaxLength(50)] string Status = "Active");

public sealed record UpdateAdPackageRequestDto(
    [Required, MinLength(1), MaxLength(100)] string PackageName,
    [Range(0, 1_000_000_000)] decimal PricePackage,
    [Range(0, 1_000_000_000)] decimal PriceRoute,
    [Range(0, 1_000_000_000)] decimal BasePriceClick,
    [Range(0, 1000)] int AdScore,
    [MaxLength(50)] string Status);
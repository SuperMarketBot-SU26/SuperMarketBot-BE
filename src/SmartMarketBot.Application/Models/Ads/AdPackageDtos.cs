using System.ComponentModel.DataAnnotations;

namespace SmartMarketBot.Application.Models.Ads;

public sealed record AdPackageDto(
    int PackageId,
    string PackageName,
    decimal PricePackage,
    decimal PriceRoute,
    decimal BasePriceClick,
    int AdScore,
    string Status,
    int ActiveCampaignCount);

public sealed record CreateAdPackageRequestDto(
    [Required(ErrorMessage = "PackageName không được để trống.")]
    [MaxLength(100, ErrorMessage = "PackageName không được vượt quá 100 ký tự.")]
    string PackageName,
    
    [Required(ErrorMessage = "PricePackage là bắt buộc.")]
    [Range(0, 999999999, ErrorMessage = "PricePackage không được âm.")]
    decimal PricePackage,
    
    [Required(ErrorMessage = "PriceRoute là bắt buộc.")]
    [Range(0, 999999999, ErrorMessage = "PriceRoute không được âm.")]
    decimal PriceRoute,
    
    [Required(ErrorMessage = "BasePriceClick là bắt buộc.")]
    [Range(0, 999999999, ErrorMessage = "BasePriceClick không được âm.")]
    decimal BasePriceClick,
    
    [Required(ErrorMessage = "AdScore là bắt buộc.")]
    [Range(0, 1000, ErrorMessage = "AdScore phải từ 0 đến 1000.")]
    int AdScore);

public sealed record UpdateAdPackageRequestDto(
    [Required(ErrorMessage = "PackageName không được để trống.")]
    [MaxLength(100, ErrorMessage = "PackageName không được vượt quá 100 ký tự.")]
    string PackageName,
    
    [Required(ErrorMessage = "PricePackage là bắt buộc.")]
    [Range(0, 999999999, ErrorMessage = "PricePackage không được âm.")]
    decimal PricePackage,
    
    [Required(ErrorMessage = "PriceRoute là bắt buộc.")]
    [Range(0, 999999999, ErrorMessage = "PriceRoute không được âm.")]
    decimal PriceRoute,
    
    [Required(ErrorMessage = "BasePriceClick là bắt buộc.")]
    [Range(0, 999999999, ErrorMessage = "BasePriceClick không được âm.")]
    decimal BasePriceClick,
    
    [Required(ErrorMessage = "AdScore là bắt buộc.")]
    [Range(0, 1000, ErrorMessage = "AdScore phải từ 0 đến 1000.")]
    int AdScore,
    
    [Required(ErrorMessage = "Status là bắt buộc.")]
    [RegularExpression("^(Active|Inactive)$", ErrorMessage = "Status chỉ nhận giá trị 'Active' hoặc 'Inactive'.")]
    string Status);

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using SmartMarketBot.Application.Models.Members;

namespace SmartMarketBot.Application.Models.Cart;

public sealed record CartItemDto(
    int CartItemId,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    string? ImageUrl,
    string? AlertType,      // Allergy | Avoid | null
    string? AlertMessage,
    IReadOnlyList<AlternativeProductDto> AlternativeProducts
);

public sealed record CartDto(
    int CartId,
    int MemberId,
    decimal TotalPrice,
    IReadOnlyList<CartItemDto> Items,
    string? AlertType,      // Allergy | Avoid | BudgetExceeded | null
    string? AlertMessage,
    decimal? RemainingBudget
);

public sealed record AddToCartDto(
    [Required(ErrorMessage = "ProductId là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ.")]
    int ProductId,

    [Range(1, 999, ErrorMessage = "Số lượng phải từ 1 đến 999.")]
    int Quantity = 1
);

public sealed record UpdateCartItemDto(
    [Range(1, 999, ErrorMessage = "Số lượng phải từ 1 đến 999.")]
    int Quantity
);

public sealed record CheckoutResponseDto(
    int InvoiceId,
    decimal TotalPrice,
    int PointsEarned,
    string Message,
    object? RoutePlan // Optimized route output
);

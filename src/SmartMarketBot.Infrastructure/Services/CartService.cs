using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Cart;
using SmartMarketBot.Application.Models.Members;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Domain.Common;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;

namespace SmartMarketBot.Infrastructure.Services;

public sealed class CartService(
    AppDbContext db,
    INavigationService navigationService,
    IMemberRealtimeNotifier memberRealtimeNotifier,
    ILocalizationService localizer) : ICartService
{
    public async Task<CartDto> GetCartAsync(int accountId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);
        var cart = await GetOrCreateCartAsync(member.MemberId, ct);

        return await BuildCartDtoAsync(member, cart, ct);
    }

    public async Task<CartDto> AddItemToCartAsync(int accountId, AddToCartDto dto, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);
        var cart = await GetOrCreateCartAsync(member.MemberId, ct);

        var product = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId, ct)
            ?? throw new KeyNotFoundException(localizer.Get("ProductNotFound", dto.ProductId));

        var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
        }
        else
        {
            var newItem = new CartItem
            {
                CartId = cart.CartId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                AddedAt = DateTime.UtcNow
            };
            db.CartItems.Add(newItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var cartDto = await BuildCartDtoAsync(member, cart, ct);
        await NotifyMemberCartUpdatedAsync(member.MemberId, cartDto, ct);
        return cartDto;
    }

    public async Task<CartDto> UpdateCartItemAsync(int accountId, int productId, UpdateCartItemDto dto, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);
        var cart = await GetOrCreateCartAsync(member.MemberId, ct);

        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm #{productId} trong giỏ hàng.");

        item.Quantity = dto.Quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var cartDto = await BuildCartDtoAsync(member, cart, ct);
        await NotifyMemberCartUpdatedAsync(member.MemberId, cartDto, ct);
        return cartDto;
    }

    public async Task<CartDto> RemoveItemFromCartAsync(int accountId, int productId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);
        var cart = await GetOrCreateCartAsync(member.MemberId, ct);

        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm #{productId} trong giỏ hàng.");

        db.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var cartDto = await BuildCartDtoAsync(member, cart, ct);
        await NotifyMemberCartUpdatedAsync(member.MemberId, cartDto, ct);
        return cartDto;
    }

    public async Task<CartDto> ClearCartAsync(int accountId, CancellationToken ct = default)
    {
        var member = await GetMemberByAccountIdAsync(accountId, ct);
        var cart = await GetOrCreateCartAsync(member.MemberId, ct);

        if (cart.CartItems.Any())
        {
            db.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        var cartDto = await BuildCartDtoAsync(member, cart, ct);
        await NotifyMemberCartUpdatedAsync(member.MemberId, cartDto, ct);
        return cartDto;
    }

    public async Task<CheckoutResponseDto> CheckoutAndPlanRouteAsync(int accountId, CancellationToken ct = default)
    {
        // 1. Get member and cart
        var member = await db.Members
            .Include(m => m.Memberships)
            .FirstOrDefaultAsync(m => m.AccountId == accountId, ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ hội viên cho tài khoản #{accountId}.");

        var cart = await db.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.MemberId == member.MemberId, ct);

        if (cart == null || !cart.CartItems.Any())
            throw new InvalidOperationException("Giỏ hàng đang trống, không thể thanh toán.");

        // 2. Calculate details
        decimal totalPrice = 0;
        var invoiceItems = new List<InvoiceHistoryItem>();
        var productIds = new List<int>();

        foreach (var item in cart.CartItems)
        {
            if (item.Product == null) continue;
            var price = item.Product.PromotionPrice ?? item.Product.UnitPrice;
            totalPrice += price * item.Quantity;
            productIds.Add(item.ProductId);

            invoiceItems.Add(new InvoiceHistoryItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = price
            });
        }

        // 3. Auto-assign robot & start node for planning route
        var robot = await db.Robots
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Status == "Online" && r.Mode == "idle", ct)
            ?? await db.Robots.AsNoTracking().FirstOrDefaultAsync(ct);

        var robotId = robot?.RobotId ?? 1;

        var startNode = await db.NavigationNodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => !n.IsBlocked, ct)
            ?? await db.NavigationNodes.AsNoTracking().FirstOrDefaultAsync(ct);

        var startNodeId = startNode?.NodeId ?? 1;

        // 4. Plan shopping route
        OptimizeShoppingRouteResponseDto? routePlan = null;
        try
        {
            var routeRequest = new OptimizeShoppingRouteRequestDto(robotId, startNodeId, productIds);
            routePlan = await navigationService.OptimizeShoppingRouteAsync(routeRequest, ct);
        }
        catch (Exception ex)
        {
            // Fallback if routing fails - checkout still succeeds
            System.Diagnostics.Debug.WriteLine($"Route optimization failed: {ex.Message}");
        }

        // 5. Create invoice record
        var pointsEarned = (int)(totalPrice * 0.1m); // 10% points
        var invoice = new InvoiceHistory
        {
            MemberId = member.MemberId,
            PurchaseDate = VnDateTime.Now,
            TotalPrice = totalPrice,
            InvoiceHistoryItems = invoiceItems
        };

        // Update member points
        member.TotalPoints += pointsEarned;

        db.InvoiceHistories.Add(invoice);
        db.CartItems.RemoveRange(cart.CartItems);
        cart.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        // Push realtime notify for cart cleared on checkout
        var cartDto = await BuildCartDtoAsync(member, cart, ct);
        await NotifyMemberCartUpdatedAsync(member.MemberId, cartDto, ct);

        // Push thông báo tích điểm thưởng
        _ = Task.Run(async () =>
        {
            try
            {
                await memberRealtimeNotifier.PushToMemberAsync(member.MemberId,
                    new SmartMarketBot.Application.Models.Realtime.MemberRealtimeUpdateDto(
                        MemberId:   member.MemberId,
                        UpdateType: "PointsEarned",
                        Title:      "🎉 Tích điểm thành công",
                        Message:    $"Bạn vừa tích được {pointsEarned} điểm! Tổng điểm hiện tại: {member.TotalPoints} điểm.",
                        Payload:    new { PointsEarned = pointsEarned, TotalPoints = member.TotalPoints, InvoiceId = invoice.InvoiceHistoryId },
                        Timestamp:  VnDateTime.Now));
            }
            catch { /* fire-and-forget */ }
        });

        return new CheckoutResponseDto(
            InvoiceId: invoice.InvoiceHistoryId,
            TotalPrice: totalPrice,
            PointsEarned: pointsEarned,
            Message: "Chỉ đường thành công! Hóa đơn đã được lưu trữ và tích điểm hội viên.",
            RoutePlan: routePlan
        );
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private async Task<Member> GetMemberByAccountIdAsync(int accountId, CancellationToken ct)
    {
        return await db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.AccountId == accountId, ct)
            ?? throw new KeyNotFoundException($"Tài khoản #{accountId} chưa có hồ sơ Member.");
    }

    private async Task<Cart> GetOrCreateCartAsync(int memberId, CancellationToken ct)
    {
        var cart = await db.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.MemberId == memberId, ct);

        if (cart == null)
        {
            cart = new Cart
            {
                MemberId = memberId,
                CreatedAt = DateTime.UtcNow
            };
            db.Carts.Add(cart);
            await db.SaveChangesAsync(ct);
        }

        return cart;
    }

    private async Task<CartDto> BuildCartDtoAsync(Member member, Cart cart, CancellationToken ct)
    {
        var itemDtos = new List<CartItemDto>();
        decimal totalPrice = 0;

        // Load allergy/avoid preferences
        var preferences = await db.MemberHealthPreferences
            .AsNoTracking()
            .Where(mhp => mhp.MemberId == member.MemberId)
            .ToListAsync(ct);

        var allergyTagIds = preferences.Where(p => p.Status == "Allergy").Select(p => p.HealthTagId).ToList();
        var avoidTagIds = preferences.Where(p => p.Status == "Avoid").Select(p => p.HealthTagId).ToList();

        foreach (var item in cart.CartItems)
        {
            if (item.Product == null) continue;

            var product = item.Product;
            var price = product.PromotionPrice ?? product.UnitPrice;
            var itemTotal = price * item.Quantity;
            totalPrice += itemTotal;

            // Check health preferences for this product
            var productTagIds = await db.ProductHealthTags
                .AsNoTracking()
                .Where(pht => pht.ProductId == product.ProductId)
                .Select(pht => pht.HealthTagId)
                .ToListAsync(ct);

            string? alertType = null;
            string? alertMessage = null;

            if (allergyTagIds.Intersect(productTagIds).Any())
            {
                alertType = "Allergy";
                alertMessage = localizer.Get("AllergyAlert", product.ProductName);
            }
            else if (avoidTagIds.Intersect(productTagIds).Any())
            {
                alertType = "Avoid";
                alertMessage = $"Cảnh báo chế độ ăn: {product.ProductName} chứa thành phần cần tránh của bạn.";
            }

            var alternatives = new List<AlternativeProductDto>();
            if (alertType != null)
            {
                alternatives = await GetAlternativeProductsForCartItemAsync(product.ProductId, product.ProductTypeId, price, allergyTagIds, ct);
            }

            itemDtos.Add(new CartItemDto(
                CartItemId: item.CartItemId,
                ProductId: item.ProductId,
                ProductName: product.ProductName,
                UnitPrice: price,
                Quantity: item.Quantity,
                TotalPrice: itemTotal,
                ImageUrl: product.ImageUrl,
                AlertType: alertType,
                AlertMessage: alertMessage,
                AlternativeProducts: alternatives
            ));
        }

        // Budget check
        string? cartAlertType = null;
        string? cartAlertMessage = null;
        decimal? remainingBudget = null;

        if (member.SpendingLimit.HasValue)
        {
            remainingBudget = member.SpendingLimit.Value - totalPrice;
            if (totalPrice > member.SpendingLimit.Value)
            {
                cartAlertType = "BudgetExceeded";
                cartAlertMessage = $"Ngân sách vượt giới hạn! Tổng giỏ hàng ({totalPrice:N0} VNĐ) lớn hơn ngân sách ({member.SpendingLimit.Value:N0} VNĐ).";
            }
        }

        // Aggregate item allergy alerts to cart level if not budget exceeded
        if (cartAlertType == null)
        {
            var allergyItem = itemDtos.FirstOrDefault(i => i.AlertType == "Allergy");
            if (allergyItem != null)
            {
                cartAlertType = "Allergy";
                cartAlertMessage = allergyItem.AlertMessage;
            }
            else
            {
                var avoidItem = itemDtos.FirstOrDefault(i => i.AlertType == "Avoid");
                if (avoidItem != null)
                {
                    cartAlertType = "Avoid";
                    cartAlertMessage = avoidItem.AlertMessage;
                }
            }
        }

        return new CartDto(
            CartId: cart.CartId,
            MemberId: cart.MemberId,
            TotalPrice: totalPrice,
            Items: itemDtos,
            AlertType: cartAlertType,
            AlertMessage: cartAlertMessage,
            RemainingBudget: remainingBudget
        );
    }

    private async Task<List<AlternativeProductDto>> GetAlternativeProductsForCartItemAsync(
        int productId, int productTypeId, decimal unitPrice, List<int> allergyTagIds, CancellationToken ct)
    {
        // Get products containing member's allergy tags
        HashSet<int> allergenProductIds = [];
        if (allergyTagIds.Count > 0)
        {
            allergenProductIds = (await db.ProductHealthTags
                .AsNoTracking()
                .Where(pht => allergyTagIds.Contains(pht.HealthTagId))
                .Select(pht => pht.ProductId)
                .ToListAsync(ct))
                .ToHashSet();
        }

        // Price range ±50%
        var minPrice = unitPrice * 0.5m;
        var maxPrice = unitPrice * 1.5m;

        var altProducts = await db.Products
            .AsNoTracking()
            .Where(x => x.ProductTypeId == productTypeId
                && x.ProductId != productId
                && x.Status == "Available"
                && !allergenProductIds.Contains(x.ProductId)
                && x.UnitPrice >= minPrice
                && x.UnitPrice <= maxPrice)
            .OrderBy(x => x.UnitPrice)
            .Take(3)
            .ToListAsync(ct);

        return altProducts.Select(p => new AlternativeProductDto(
            p.ProductId,
            p.ProductName,
            p.UnitPrice,
            p.ImageUrl,
            "Sản phẩm thay thế an toàn cho bạn."
        )).ToList();
    }

    private async Task NotifyMemberCartUpdatedAsync(int memberId, CartDto cartDto, CancellationToken ct)
    {
        try
        {
            var updateDto = new SmartMarketBot.Application.Models.Realtime.MemberRealtimeUpdateDto(
                MemberId: memberId,
                UpdateType: "CartUpdate",
                Title: "Giỏ hàng thay đổi",
                Message: $"Giỏ hàng của bạn đã được cập nhật. Tổng tiền: {cartDto.TotalPrice:N0} VNĐ",
                Payload: cartDto,
                Timestamp: VnDateTime.Now
            );
            await memberRealtimeNotifier.PushToMemberAsync(memberId, updateDto, ct);
        }
        catch (Exception ex)
        {
            // Do not fail the transaction if notifier fails
            System.Diagnostics.Debug.WriteLine($"Failed to push realtime update: {ex.Message}");
        }
    }
}

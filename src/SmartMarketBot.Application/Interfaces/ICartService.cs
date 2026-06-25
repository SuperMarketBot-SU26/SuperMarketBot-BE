using System.Threading;
using System.Threading.Tasks;
using SmartMarketBot.Application.Models.Cart;

namespace SmartMarketBot.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int accountId, CancellationToken ct = default);
    Task<CartDto> AddItemToCartAsync(int accountId, AddToCartDto dto, CancellationToken ct = default);
    Task<CartDto> UpdateCartItemAsync(int accountId, int productId, UpdateCartItemDto dto, CancellationToken ct = default);
    Task<CartDto> RemoveItemFromCartAsync(int accountId, int productId, CancellationToken ct = default);
    Task<CartDto> ClearCartAsync(int accountId, CancellationToken ct = default);
    Task<CheckoutResponseDto> CheckoutAndPlanRouteAsync(int accountId, CancellationToken ct = default);
}

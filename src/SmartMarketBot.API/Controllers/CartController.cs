using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Cart;

namespace SmartMarketBot.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public sealed class CartController(ICartService cartService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken ct)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await cartService.GetCartAsync(accountId.Value, ct);
        return Ok(result);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromBody] AddToCartDto dto,
        CancellationToken ct)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await cartService.AddItemToCartAsync(accountId.Value, dto, ct);
        return Ok(result);
    }

    [HttpPut("items/{productId:int}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateItem(
        int productId,
        [FromBody] UpdateCartItemDto dto,
        CancellationToken ct)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await cartService.UpdateCartItemAsync(accountId.Value, productId, dto, ct);
        return Ok(result);
    }

    [HttpDelete("items/{productId:int}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveItem(
        int productId,
        CancellationToken ct)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await cartService.RemoveItemFromCartAsync(accountId.Value, productId, ct);
        return Ok(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> ClearCart(CancellationToken ct)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await cartService.ClearCartAsync(accountId.Value, ct);
        return Ok(result);
    }

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CheckoutResponseDto>> Checkout(CancellationToken ct)
    {
        var accountId = GetCurrentAccountId();
        if (accountId is null) return Unauthorized();

        var result = await cartService.CheckoutAndPlanRouteAsync(accountId.Value, ct);
        return Ok(result);
    }

    private int? GetCurrentAccountId()
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var id) ? id : null;
    }
}

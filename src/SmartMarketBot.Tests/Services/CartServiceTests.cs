using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Moq;

using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Cart;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;
using SmartMarketBot.Infrastructure.Services;

using Xunit;

namespace SmartMarketBot.Tests.Services;

public class CartServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly Mock<INavigationService> _mockNavService;
    private readonly Mock<IMemberRealtimeNotifier> _mockNotifier;
    private readonly Mock<ILocalizationService> _mockLocalizer;

    public CartServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Cart_{Guid.NewGuid()}")
            .Options;

        _mockNavService = new Mock<INavigationService>();
        _mockNavService.Setup(n => n.OptimizeShoppingRouteAsync(It.IsAny<OptimizeShoppingRouteRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OptimizeShoppingRouteResponseDto(10.0, 1, new List<ShoppingWaypointDto>(), new List<int>(), null));

        _mockNotifier = new Mock<IMemberRealtimeNotifier>();
        _mockLocalizer = new Mock<ILocalizationService>();
        _mockLocalizer.Setup(l => l.Get(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => key);
    }

    private AppDbContext CreateDbContext() => new(_dbOptions);

    [Fact]
    public async Task AddItemToCartAsync_NewItem_ShouldAddToCartAndReturnUpdatedCart()
    {
        // Arrange
        using var db = CreateDbContext();
        var account = new Account { AccountId = 10, Email = "cartuser@example.com", Username = "cartuser" };
        var member = new Member { MemberId = 5, AccountId = 10, Account = account, FullName = "Cart User" };
        var product = new Product { ProductId = 101, ProductName = "Fresh Milk", UnitPrice = 2.50m, Status = "Available" };

        db.Accounts.Add(account);
        db.Members.Add(member);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var cartService = new CartService(db, _mockNavService.Object, _mockNotifier.Object, _mockLocalizer.Object);

        // Act
        var result = await cartService.AddItemToCartAsync(10, new AddToCartDto(101, 2));

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().ProductId.Should().Be(101);
        result.Items.First().Quantity.Should().Be(2);
        result.TotalPrice.Should().Be(5.00m);
    }

    [Fact]
    public async Task AddItemToCartAsync_ExistingItem_ShouldIncrementQuantity()
    {
        // Arrange
        using var db = CreateDbContext();
        var account = new Account { AccountId = 12, Email = "cartuser2@example.com", Username = "cartuser2" };
        var member = new Member { MemberId = 6, AccountId = 12, Account = account, FullName = "Cart User 2" };
        var product = new Product { ProductId = 102, ProductName = "Apple", UnitPrice = 1.00m, Status = "Available" };
        var cart = new Cart { CartId = 20, MemberId = 6, CreatedAt = DateTime.UtcNow };
        var existingItem = new CartItem { CartId = 20, ProductId = 102, Quantity = 3, AddedAt = DateTime.UtcNow };

        db.Accounts.Add(account);
        db.Members.Add(member);
        db.Products.Add(product);
        db.Carts.Add(cart);
        db.CartItems.Add(existingItem);
        await db.SaveChangesAsync();

        var cartService = new CartService(db, _mockNavService.Object, _mockNotifier.Object, _mockLocalizer.Object);

        // Act
        var result = await cartService.AddItemToCartAsync(12, new AddToCartDto(102, 2));

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Quantity.Should().Be(5);
        result.TotalPrice.Should().Be(5.00m);
    }

    [Fact]
    public async Task AddItemToCartAsync_NonExistentProduct_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var account = new Account { AccountId = 15, Email = "cartuser3@example.com", Username = "cartuser3" };
        var member = new Member { MemberId = 7, AccountId = 15, Account = account, FullName = "Cart User 3" };

        db.Accounts.Add(account);
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var cartService = new CartService(db, _mockNavService.Object, _mockNotifier.Object, _mockLocalizer.Object);

        // Act
        Func<Task> act = async () => await cartService.AddItemToCartAsync(15, new AddToCartDto(9999, 1));

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

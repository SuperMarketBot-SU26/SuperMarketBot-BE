using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Members;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Options;
using SmartMarketBot.Infrastructure.Persistence;
using SmartMarketBot.Infrastructure.Services;

using Xunit;

namespace SmartMarketBot.Tests.Services;

public class MemberServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly Mock<ILocalizationService> _mockLocalizer;
    private readonly Mock<ICloudStorageService> _mockCloudStorage;
    private readonly Mock<IMemberRealtimeNotifier> _mockRealtimeNotifier;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<MemberService>> _mockLogger;
    private readonly IOptions<CloudinaryOptions> _cloudinaryOptions;

    public MemberServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Member_{Guid.NewGuid()}")
            .Options;

        _mockLocalizer = new Mock<ILocalizationService>();
        _mockLocalizer.Setup(l => l.Get(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => key);

        _mockCloudStorage = new Mock<ICloudStorageService>();
        _mockRealtimeNotifier = new Mock<IMemberRealtimeNotifier>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<MemberService>>();

        _cloudinaryOptions = Options.Create(new CloudinaryOptions
        {
            CloudName = "test_cloud",
            ApiKey = "test_key",
            ApiSecret = "test_secret"
        });
    }

    private AppDbContext CreateDbContext() => new(_dbOptions);

    [Fact]
    public async Task SetShoppingBudgetAsync_ValidMember_ShouldUpdateSpendingLimit()
    {
        // Arrange
        using var db = CreateDbContext();
        var account = new Account { AccountId = 1, Email = "budgetmember@example.com", Username = "budgetmember" };
        var member = new Member { MemberId = 10, AccountId = 1, Account = account, FullName = "Budget Member", SpendingLimit = 0 };

        db.Accounts.Add(account);
        db.Members.Add(member);
        await db.SaveChangesAsync();

        var memberService = new MemberService(
            db,
            _mockLocalizer.Object,
            _mockCloudStorage.Object,
            _cloudinaryOptions,
            _mockRealtimeNotifier.Object,
            _mockScopeFactory.Object,
            _mockLogger.Object);

        // Act
        var result = await memberService.SetShoppingBudgetAsync(10, new SetBudgetRequestDto(500.0m));

        // Assert
        result.Should().NotBeNull();
        result.MemberId.Should().Be(10);
        result.Budget.Should().Be(500.0m);

        var updatedMember = await db.Members.FindAsync(10);
        updatedMember!.SpendingLimit.Should().Be(500.0m);
    }

    [Fact]
    public async Task SetShoppingBudgetAsync_NonExistentMember_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var memberService = new MemberService(
            db,
            _mockLocalizer.Object,
            _mockCloudStorage.Object,
            _cloudinaryOptions,
            _mockRealtimeNotifier.Object,
            _mockScopeFactory.Object,
            _mockLogger.Object);

        // Act
        Func<Task> act = async () => await memberService.SetShoppingBudgetAsync(999, new SetBudgetRequestDto(100.0m));

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}

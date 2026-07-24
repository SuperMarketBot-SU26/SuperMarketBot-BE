using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartMarketBot.Application.Interfaces;
using SmartMarketBot.Application.Models.Navigation;
using SmartMarketBot.Application.Services;
using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Persistence;
using Xunit;

namespace SmartMarketBot.Tests.Services;

public class NavigationServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly Mock<ILocalizationService> _mockLocalizer;

    public NavigationServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_Nav_{Guid.NewGuid()}")
            .Options;

        _mockLocalizer = new Mock<ILocalizationService>();
        _mockLocalizer.Setup(l => l.Get(It.IsAny<string>()))
            .Returns((string key) => key);
        _mockLocalizer.Setup(l => l.Get(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] args) => key);
    }

    private AppDbContext CreateDbContext() => new(_dbOptions);

    [Fact]
    public async Task PlanRouteAsync_ValidNodes_ShouldReturnCorrectShortestPath()
    {
        // Arrange
        using var db = CreateDbContext();
        db.NavigationNodes.AddRange(
            new NavigationNode { NodeId = 1, NodeName = "Node 1", XCoord = 0, YCoord = 0, IsBlocked = false },
            new NavigationNode { NodeId = 2, NodeName = "Node 2", XCoord = 10, YCoord = 0, IsBlocked = false },
            new NavigationNode { NodeId = 3, NodeName = "Node 3", XCoord = 20, YCoord = 0, IsBlocked = false }
        );
        db.NavigationEdges.AddRange(
            new NavigationEdge { FromNodeId = 1, ToNodeId = 2, Distance = 10, IsBidirectional = true },
            new NavigationEdge { FromNodeId = 2, ToNodeId = 3, Distance = 10, IsBidirectional = true }
        );
        await db.SaveChangesAsync();

        var service = new NavigationService(db, _mockLocalizer.Object);

        // Act
        var result = await service.PlanRouteAsync(new RoutePlanRequestDto(1, 3));

        // Assert
        result.Should().NotBeNull();
        result.TotalDistance.Should().Be(20.0);
        result.Nodes.Should().HaveCount(3);
        result.Nodes.Select(n => n.NodeId).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task PlanRouteAsync_NonExistentNode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateDbContext();
        db.NavigationNodes.Add(new NavigationNode { NodeId = 1, NodeName = "Node 1", XCoord = 0, YCoord = 0 });
        await db.SaveChangesAsync();

        var service = new NavigationService(db, _mockLocalizer.Object);

        // Act
        Func<Task> act = async () => await service.PlanRouteAsync(new RoutePlanRequestDto(1, 999));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("StartEndNodeNotExist");
    }

    [Fact]
    public async Task PlanRouteAsync_BlockedStartNode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateDbContext();
        db.NavigationNodes.AddRange(
            new NavigationNode { NodeId = 1, NodeName = "Node 1", XCoord = 0, YCoord = 0, IsBlocked = true },
            new NavigationNode { NodeId = 2, NodeName = "Node 2", XCoord = 10, YCoord = 0, IsBlocked = false }
        );
        await db.SaveChangesAsync();

        var service = new NavigationService(db, _mockLocalizer.Object);

        // Act
        Func<Task> act = async () => await service.PlanRouteAsync(new RoutePlanRequestDto(1, 2));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("StartEndNodeBlocked");
    }

    [Fact]
    public async Task PlanRouteAsync_UnreachableDestination_ShouldReturnZeroDistanceAndEmptyNodes()
    {
        // Arrange
        using var db = CreateDbContext();
        db.NavigationNodes.AddRange(
            new NavigationNode { NodeId = 1, NodeName = "Node 1", XCoord = 0, YCoord = 0, IsBlocked = false },
            new NavigationNode { NodeId = 2, NodeName = "Node 2", XCoord = 10, YCoord = 0, IsBlocked = false }
        );
        // No edge between node 1 and node 2
        await db.SaveChangesAsync();

        var service = new NavigationService(db, _mockLocalizer.Object);

        // Act
        var result = await service.PlanRouteAsync(new RoutePlanRequestDto(1, 2));

        // Assert
        result.Should().NotBeNull();
        result.TotalDistance.Should().Be(0);
        result.Nodes.Should().BeEmpty();
    }
}

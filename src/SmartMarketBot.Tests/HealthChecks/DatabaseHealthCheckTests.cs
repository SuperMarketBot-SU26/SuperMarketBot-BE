using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartMarketBot.Infrastructure.HealthChecks;
using SmartMarketBot.Infrastructure.Persistence;
using Xunit;

namespace SmartMarketBot.Tests.HealthChecks;

public class DatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_InMemoryDatabase_ShouldReturnHealthyStatus()
    {
        // Arrange
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_HealthCheck_{Guid.NewGuid()}")
            .Options;

        using var dbContext = new AppDbContext(dbOptions);
        var healthCheck = new DatabaseHealthCheck(dbContext);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("healthy");
    }
}

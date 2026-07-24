using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

using FluentAssertions;

using Microsoft.Extensions.Options;

using SmartMarketBot.Domain.Entities;
using SmartMarketBot.Infrastructure.Options;
using SmartMarketBot.Infrastructure.Services;

using Xunit;

namespace SmartMarketBot.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public TokenServiceTests()
    {
        _jwtOptions = new JwtOptions
        {
            Issuer = "SmartMarketBotTest",
            Audience = "SmartMarketBotTestClient",
            SecretKey = "SuperSecretKeyForTestingPurposesOnlyMustBeAtLeast32BytesLong!",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };

        var optionsMock = Options.Create(_jwtOptions);
        _tokenService = new TokenService(optionsMock);
    }

    [Fact]
    public void CreateAccessToken_ValidAccount_ShouldReturnValidJwtTokenAndExpiration()
    {
        // Arrange
        var account = new Account
        {
            AccountId = 42,
            Email = "testuser@example.com",
            Username = "testuser",
            Status = "Active"
        };
        var roles = new List<string> { "Member", "Admin" };

        // Act
        var (token, expiresAt) = _tokenService.CreateAccessToken(account, roles);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be(_jwtOptions.Issuer);
        jwt.Audiences.Should().Contain(_jwtOptions.Audience);

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier);
        subClaim?.Value.Should().Be("42");

        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value);
        roleClaims.Should().Contain(new[] { "Member", "Admin" });
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueNonEmptyBase64String()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GetUserIdFromExpiredToken_ValidTokenFormat_ShouldExtractUserId()
    {
        // Arrange
        var account = new Account
        {
            AccountId = 100,
            Email = "member@example.com",
            Username = "member100"
        };
        var (token, _) = _tokenService.CreateAccessToken(account, new[] { "Member" });

        // Act
        var userId = _tokenService.GetUserIdFromExpiredToken(token);

        // Assert
        userId.Should().Be(100);
    }

    [Fact]
    public void GetUserIdFromExpiredToken_InvalidToken_ShouldReturnNull()
    {
        // Act
        var userId = _tokenService.GetUserIdFromExpiredToken("invalid.jwt.token");

        // Assert
        userId.Should().BeNull();
    }
}

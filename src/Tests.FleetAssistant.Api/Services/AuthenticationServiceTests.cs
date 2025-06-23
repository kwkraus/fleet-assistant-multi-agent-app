using FleetAssistant.Api.Services;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Tests.FleetAssistant.Api.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        _authService = new AuthenticationService(_mockLogger.Object);
    }

    [Fact]
    public async Task ValidateTokenAndGetUserContextAsync_WithoutAuthHeader_ReturnsNull()
    {
        // Act
        var result = await _authService.ValidateTokenAndGetUserContextAsync(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateTokenAndGetUserContextAsync_WithInvalidAuthHeader_ReturnsNull()
    {
        // Act
        var result = await _authService.ValidateTokenAndGetUserContextAsync("InvalidHeader");

        // Assert
        Assert.Null(result);
    }    [Fact]
    public async Task ValidateTokenAndGetUserContextAsync_WithValidJWT_ReturnsUserContext()
    {
        // Arrange
        var handler = new JwtSecurityTokenHandler();
        var claims = new[]
        {
            new System.Security.Claims.Claim("sub", "test-user-123"),
            new System.Security.Claims.Claim("email", "test@example.com"),
            new System.Security.Claims.Claim("tenant_id", "test-tenant"),
            new System.Security.Claims.Claim("roles", "user")
        };

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "test-issuer",
            Audience = "test-audience"
        };

        var token = handler.CreateEncodedJwt(tokenDescriptor);
        var authHeader = $"Bearer {token}";

        // Act
        var result = await _authService.ValidateTokenAndGetUserContextAsync(authHeader);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-user-123", result.UserId);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("test-tenant", result.TenantId);
        Assert.Contains("test-tenant", result.AuthorizedTenantIds);
        Assert.Contains("user", result.Roles);
    }

    [Fact]
    public async Task ValidateTokenAndGetUserContextAsync_WithMalformedJWT_ReturnsNull()
    {
        // Arrange
        var authHeader = "Bearer invalid.jwt.token";

        // Act
        var result = await _authService.ValidateTokenAndGetUserContextAsync(authHeader);

        // Assert
        Assert.Null(result);
    }
}

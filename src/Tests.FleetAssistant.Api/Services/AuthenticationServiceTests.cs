using FleetAssistant.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

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
    public async Task ValidateApiKeyAndGetUserContextAsync_WithoutAuthHeader_ReturnsNull()
    {
        // Act
        var result = await _authService.ValidateApiKeyAndGetUserContextAsync(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateApiKeyAndGetUserContextAsync_WithInvalidApiKey_ReturnsNull()
    {
        // Act
        var result = await _authService.ValidateApiKeyAndGetUserContextAsync("Bearer invalid_api_key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateApiKeyAsync_CreatesValidApiKey()
    {
        // Arrange
        var tenantId = "test-tenant";
        var name = "Test API Key";
        var scopes = new List<string> { "fleet:read", "fleet:query" };

        // Act
        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(tenantId, name, scopes);        // Assert
        Assert.NotNull(apiKey);
        Assert.StartsWith("fa_dev_", apiKey);
        Assert.Equal(31, apiKey.Length); // fa_dev_ (7) + 24 random chars
        Assert.Equal(tenantId, keyInfo.TenantId);
        Assert.Equal(name, keyInfo.Name);
        Assert.Equal(scopes, keyInfo.Scopes);
        Assert.True(keyInfo.IsActive);
    }

    [Fact]
    public async Task ValidateApiKeyAndGetUserContextAsync_WithValidApiKey_ReturnsUserContext()
    {
        // Arrange
        var tenantId = "test-tenant";
        var name = "Test API Key";
        var scopes = new List<string> { "fleet:read", "fleet:query" };

        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(tenantId, name, scopes);
        var authHeader = $"Bearer {apiKey}";

        // Act
        var result = await _authService.ValidateApiKeyAndGetUserContextAsync(authHeader);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(keyInfo.ApiKeyId, result.ApiKeyId);
        Assert.Equal(name, result.ApiKeyName);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("development", result.Environment);
        Assert.Equal(scopes, result.Scopes);
    }

    [Fact]
    public async Task ValidateApiKeyAndGetUserContextAsync_WithApiKeyHeader_ReturnsUserContext()
    {
        // Arrange
        var tenantId = "test-tenant";
        var name = "Test API Key";
        var scopes = new List<string> { "fleet:read" };

        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(tenantId, name, scopes);
        var authHeader = $"ApiKey {apiKey}";

        // Act
        var result = await _authService.ValidateApiKeyAndGetUserContextAsync(authHeader);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(keyInfo.ApiKeyId, result.ApiKeyId);
        Assert.Equal(tenantId, result.TenantId);
    }

    [Fact]
    public async Task ValidateApiKeyAndGetUserContextAsync_WithDirectApiKey_ReturnsUserContext()
    {
        // Arrange
        var tenantId = "test-tenant";
        var name = "Test API Key";
        var scopes = new List<string> { "fleet:read" };

        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(tenantId, name, scopes);

        // Act (direct API key without Bearer/ApiKey prefix)
        var result = await _authService.ValidateApiKeyAndGetUserContextAsync(apiKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(keyInfo.ApiKeyId, result.ApiKeyId);
        Assert.Equal(tenantId, result.TenantId);
    }
}

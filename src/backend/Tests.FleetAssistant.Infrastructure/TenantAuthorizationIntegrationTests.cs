using FleetAssistant.Api.Middleware;
using FleetAssistant.Api.Services;
using FleetAssistant.Infrastructure.Services;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Tests.FleetAssistant.Infrastructure;

[TestClass]
public class TenantAuthorizationIntegrationTests
{
    private Mock<ILogger<TenantAuthorizationMiddleware>> _mockLogger = null!;
    private Mock<ILogger<InMemoryTenantService>> _mockTenantLogger = null!;
    private Mock<ILogger<AuthenticationService>> _mockAuthLogger = null!;
    private InMemoryTenantService _tenantService = null!;
    private AuthenticationService _authService = null!;
    private TenantAuthorizationMiddleware _authMiddleware = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<TenantAuthorizationMiddleware>>();
        _mockTenantLogger = new Mock<ILogger<InMemoryTenantService>>();
        _mockAuthLogger = new Mock<ILogger<AuthenticationService>>();

        _tenantService = new InMemoryTenantService(_mockTenantLogger.Object);
        _authService = new AuthenticationService(_mockAuthLogger.Object);
        _authMiddleware = new TenantAuthorizationMiddleware(_mockLogger.Object, _tenantService);
    }

    [TestMethod]
    public async Task ValidatePermissionAsync_BasicTenantFleetQuery_Success()
    {
        // Arrange
        var tenantId = "test-tenant-1"; // Basic tier from test data
        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            tenantId,
            "Test Basic User",
            Roles.FleetUser,
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = tenantId,
            Scopes = keyInfo.Scopes
        };

        // Act
        var result = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQuery.Id);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Tenant);
        Assert.AreEqual(tenantId, result.Tenant.TenantId);
        Assert.IsNotNull(result.RateLimitInfo);
        Assert.IsTrue(result.RateLimitInfo.IsAllowed);
    }

    [TestMethod]
    public async Task ValidatePermissionAsync_BasicTenantAdvancedQuery_Denied()
    {
        // Arrange
        var tenantId = "test-tenant-1"; // Basic tier - no advanced features
        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            tenantId,
            "Test Basic User",
            Roles.FleetUser,
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = tenantId,
            Scopes = keyInfo.Scopes
        };

        // Act
        var result = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQueryAdvanced.Id);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("permission"));
    }

    [TestMethod]
    public async Task ValidatePermissionAsync_PremiumTenantAdvancedQuery_Success()
    {
        // Arrange
        var tenantId = "premium-tenant"; // Premium tier with advanced features
        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            tenantId,
            "Test Premium Analyst",
            Roles.FleetAnalyst,
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = tenantId,
            Scopes = keyInfo.Scopes
        };

        // Act
        var result = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQueryAdvanced.Id);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Tenant);
        Assert.AreEqual(TenantTier.Premium, result.Tenant.Tier);
    }

    [TestMethod]
    public async Task ValidatePermissionAsync_SuspendedTenant_Denied()
    {
        // Arrange
        var tenantConfig = new TenantConfiguration
        {
            Name = "Suspended Test Tenant",
            Tier = TenantTier.Basic,
            Status = TenantStatus.Active
        };
        var tenant = await _tenantService.CreateTenantAsync(tenantConfig);
        await _tenantService.SuspendTenantAsync(tenant.TenantId, "Payment overdue");

        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            tenant.TenantId,
            "Suspended User",
            Roles.FleetUser,
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = tenant.TenantId,
            Scopes = keyInfo.Scopes
        };

        // Act
        var result = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQuery.Id);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("suspended"));
    }

    [TestMethod]
    public async Task ValidatePermissionAsync_ApiKeyScopeRestriction_Denied()
    {
        // Arrange
        var tenantId = "premium-tenant";
        var limitedScopes = new List<string> { Permissions.FleetQuery.Id }; // Only basic query, no advanced

        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            tenantId,
            "Limited Scope Key",
            limitedScopes,
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = tenantId,
            Scopes = keyInfo.Scopes
        };

        // Act - Try to use advanced query with limited scope API key
        var result = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQueryAdvanced.Id);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("scope"));
    }

    [TestMethod]
    public async Task ValidateIntegrationAccessAsync_AllowedIntegration_Success()
    {
        // Arrange
        var tenantId = "test-tenant-1";
        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            tenantId,
            "Integration User",
            new List<string> { Permissions.IntegrationGeotab.Id },
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = tenantId,
            Scopes = keyInfo.Scopes
        };

        // Act
        var result = await _authMiddleware.ValidateIntegrationAccessAsync(userContext, "geotab");

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task ValidateIntegrationAccessAsync_DisallowedIntegration_Denied()
    {
        // Arrange
        var tenantId = "test-tenant-1";
        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            tenantId,
            "Limited Integration User",
            new List<string> { Permissions.FleetQuery.Id }, // No integration permissions
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = tenantId,
            Scopes = keyInfo.Scopes
        };

        // Act
        var result = await _authMiddleware.ValidateIntegrationAccessAsync(userContext, "geotab");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task RecordUsageAsync_MultipleRequests_UpdatesUsageCorrectly()
    {
        // Arrange
        var tenantId = "test-tenant-1";
        var initialUsage = await _tenantService.GetTenantUsageAsync(tenantId);
        var initialCount = initialUsage.ApiCallsToday;

        // Act
        await _authMiddleware.RecordUsageAsync(tenantId, 100.5, true);
        await _authMiddleware.RecordUsageAsync(tenantId, 200.5, true);
        await _authMiddleware.RecordUsageAsync(tenantId, 150.0, false); // Failed request

        // Assert
        var updatedUsage = await _tenantService.GetTenantUsageAsync(tenantId);
        Assert.AreEqual(initialCount + 3, updatedUsage.ApiCallsToday);
        Assert.IsTrue(updatedUsage.AverageResponseTimeMs > 0);
        Assert.IsTrue(updatedUsage.ErrorRatePercentage > 0); // Should have some error rate due to failed request
    }

    [TestMethod]
    public async Task ValidatePermissionAsync_RateLimitExceeded_RateLimited()
    {
        // Arrange - Create a tenant with very low limits
        var tenantConfig = new TenantConfiguration
        {
            Name = "Rate Limit Test Tenant",
            Tier = TenantTier.Free, // Free tier has low limits
            Status = TenantStatus.Active
        };
        var tenant = await _tenantService.CreateTenantAsync(tenantConfig);

        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            tenant.TenantId,
            "Rate Limited User",
            Roles.Viewer,
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = tenant.TenantId,
            Scopes = keyInfo.Scopes
        };

        // Exhaust the rate limit
        for (int i = 0; i < 150; i++) // Free tier has 100/day limit
        {
            await _tenantService.RecordUsageAsync(tenant.TenantId, 1, 100, true);
        }

        // Act
        var result = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQuery.Id);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.IsRateLimited);
        Assert.IsNotNull(result.RateLimitInfo);
        Assert.IsFalse(result.RateLimitInfo.IsAllowed);
        Assert.IsTrue(result.ErrorMessage?.Contains("rate limit") == true);
    }

    [TestMethod]
    public async Task GetRateLimitHeaders_RateLimitInfo_ReturnsCorrectHeaders()
    {
        // Arrange
        var tenantId = "test-tenant-1";
        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            tenantId,
            "Header Test User",
            Roles.FleetUser,
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = tenantId,
            Scopes = keyInfo.Scopes
        };

        // Act
        var result = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQuery.Id);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        var headers = result.GetRateLimitHeaders();
        Assert.IsTrue(headers.ContainsKey("X-RateLimit-Limit"));
        Assert.IsTrue(headers.ContainsKey("X-RateLimit-Remaining"));
        Assert.IsTrue(headers.ContainsKey("X-RateLimit-Reset"));

        // Verify header values are valid
        Assert.IsTrue(int.Parse(headers["X-RateLimit-Limit"]) > 0);
        Assert.IsTrue(int.Parse(headers["X-RateLimit-Remaining"]) >= 0);
        Assert.IsTrue(long.Parse(headers["X-RateLimit-Reset"]) > 0);
    }

    [TestMethod]
    public async Task ToErrorResponse_RateLimited_ReturnsDetailedError()
    {
        // Arrange - Create rate limited scenario
        var tenantConfig = new TenantConfiguration
        {
            Name = "Error Response Test",
            Tier = TenantTier.Free,
            Status = TenantStatus.Active
        };
        var tenant = await _tenantService.CreateTenantAsync(tenantConfig);

        // Exhaust rate limit
        for (int i = 0; i < 120; i++)
        {
            await _tenantService.RecordUsageAsync(tenant.TenantId, 1, 100, true);
        }

        var rateLimitResult = await _tenantService.CheckRateLimitAsync(tenant.TenantId);

        var authResult = TenantAuthorizationResult.RateLimited(rateLimitResult);

        // Act
        var errorResponse = authResult.ToErrorResponse();

        // Assert
        Assert.IsNotNull(errorResponse);

        // Use reflection or casting to access the anonymous object properties
        var responseType = errorResponse.GetType();
        var errorProperty = responseType.GetProperty("error");
        var rateLimitInfoProperty = responseType.GetProperty("rateLimitInfo");

        Assert.IsNotNull(errorProperty);
        Assert.IsNotNull(rateLimitInfoProperty);

        var errorMessage = errorProperty.GetValue(errorResponse)?.ToString();
        Assert.IsTrue(errorMessage?.Contains("rate limit") == true);

        var rateLimitInfo = rateLimitInfoProperty.GetValue(errorResponse);
        Assert.IsNotNull(rateLimitInfo);
    }

    [TestMethod]
    public async Task TenantLifecycle_CreateUpdateSuspendReactivate_WorksCorrectly()
    {
        // Arrange
        var tenantConfig = new TenantConfiguration
        {
            Name = "Lifecycle Test Tenant",
            Tier = TenantTier.Basic,
            Contact = new TenantContact
            {
                PrimaryContactName = "Test Contact",
                PrimaryContactEmail = "test@lifecycle.com"
            }
        };

        // Act & Assert - Create
        var createdTenant = await _tenantService.CreateTenantAsync(tenantConfig);
        Assert.AreEqual(TenantStatus.Pending, createdTenant.Status);

        // Update to Active
        createdTenant.Status = TenantStatus.Active;
        var updatedTenant = await _tenantService.UpdateTenantAsync(createdTenant);
        Assert.AreEqual(TenantStatus.Active, updatedTenant.Status);

        // Verify API access works
        var (apiKey, keyInfo) = await _authService.GenerateApiKeyAsync(
            updatedTenant.TenantId,
            "Lifecycle User",
            Roles.FleetUser,
            "test");

        var userContext = new UserContext
        {
            ApiKeyId = keyInfo.ApiKeyId,
            TenantId = updatedTenant.TenantId,
            Scopes = keyInfo.Scopes
        };

        var authResult = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQuery.Id);
        Assert.IsTrue(authResult.IsSuccess);

        // Suspend
        await _tenantService.SuspendTenantAsync(updatedTenant.TenantId, "Testing suspension");
        var suspendedAuthResult = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQuery.Id);
        Assert.IsFalse(suspendedAuthResult.IsSuccess);

        // Reactivate
        await _tenantService.ReactivateTenantAsync(updatedTenant.TenantId);
        var reactivatedAuthResult = await _authMiddleware.ValidatePermissionAsync(userContext, Permissions.FleetQuery.Id);
        Assert.IsTrue(reactivatedAuthResult.IsSuccess);
    }
}

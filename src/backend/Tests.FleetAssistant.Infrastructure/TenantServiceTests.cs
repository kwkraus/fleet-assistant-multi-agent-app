using FleetAssistant.Infrastructure.Services;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Tests.FleetAssistant.Infrastructure;

[TestClass]
public class TenantServiceTests
{
    private Mock<ILogger<InMemoryTenantService>> _mockLogger = null!;
    private InMemoryTenantService _tenantService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<InMemoryTenantService>>();
        _tenantService = new InMemoryTenantService(_mockLogger.Object);
    }

    [TestMethod]
    public async Task GetTenantConfigurationAsync_ExistingTenant_ReturnsConfiguration()
    {
        // Arrange
        var tenantId = "test-tenant-1"; // This should be pre-initialized

        // Act
        var result = await _tenantService.GetTenantConfigurationAsync(tenantId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(tenantId, result.TenantId);
        Assert.AreEqual(TenantStatus.Active, result.Status);
    }

    [TestMethod]
    public async Task GetTenantConfigurationAsync_NonExistentTenant_ReturnsNull()
    {
        // Arrange
        var tenantId = "non-existent-tenant";

        // Act
        var result = await _tenantService.GetTenantConfigurationAsync(tenantId);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task CreateTenantAsync_ValidConfiguration_CreatesSuccessfully()
    {
        // Arrange
        var tenantConfig = new TenantConfiguration
        {
            Name = "Test New Tenant",
            Tier = TenantTier.Basic,
            Contact = new TenantContact
            {
                PrimaryContactName = "Test User",
                PrimaryContactEmail = "test@example.com",
                OrganizationName = "Test Organization"
            }
        };

        // Act
        var result = await _tenantService.CreateTenantAsync(tenantConfig);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.TenantId));
        Assert.AreEqual("Test New Tenant", result.Name);
        Assert.AreEqual(TenantTier.Basic, result.Tier);
        Assert.AreEqual(TenantStatus.Pending, result.Status); // Default status
        Assert.IsTrue(result.CreatedAt <= DateTime.UtcNow);
        Assert.IsTrue(result.UpdatedAt <= DateTime.UtcNow);

        // Verify tier-based defaults are applied
        Assert.AreEqual(50, result.RateLimitPerMinute);
        Assert.AreEqual(1000, result.RateLimitPerDay);
        Assert.AreEqual(3, result.MaxConcurrentRequests);
        Assert.AreEqual(2, result.Integrations.MaxIntegrations);
    }

    [TestMethod]
    public async Task ValidateTenantAccessAsync_ActiveTenantWithPermission_ReturnsTrue()
    {
        // Arrange
        var tenantId = "test-tenant-1";
        var permission = Permissions.FleetQuery.Id;

        // Act
        var result = await _tenantService.ValidateTenantAccessAsync(tenantId, permission);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task ValidateTenantAccessAsync_InactiveTenant_ReturnsFalse()
    {
        // Arrange
        var tenantConfig = new TenantConfiguration
        {
            Name = "Suspended Tenant",
            Status = TenantStatus.Suspended,
            Tier = TenantTier.Basic
        };
        var tenant = await _tenantService.CreateTenantAsync(tenantConfig);
        await _tenantService.SuspendTenantAsync(tenant.TenantId, "Test suspension");

        // Act
        var result = await _tenantService.ValidateTenantAccessAsync(tenant.TenantId, Permissions.FleetQuery.Id);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ValidateIntegrationAccessAsync_AllowedIntegration_ReturnsTrue()
    {
        // Arrange
        var tenantId = "test-tenant-1";
        var integrationKey = "geotab"; // Should be in allowed list

        // Act
        var result = await _tenantService.ValidateIntegrationAccessAsync(tenantId, integrationKey);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CheckRateLimitAsync_WithinLimits_ReturnsAllowed()
    {
        // Arrange
        var tenantId = "test-tenant-1";

        // Act
        var result = await _tenantService.CheckRateLimitAsync(tenantId);

        // Assert
        Assert.IsTrue(result.IsAllowed);
        Assert.IsTrue(result.RequestsRemaining > 0);
        Assert.IsTrue(result.MaxRequestsAllowed > 0);
    }

    [TestMethod]
    public async Task RecordUsageAsync_ValidUsage_UpdatesCounters()
    {
        // Arrange
        var tenantId = "test-tenant-1";
        var initialUsage = await _tenantService.GetTenantUsageAsync(tenantId);
        var initialDailyCount = initialUsage.ApiCallsToday;
        var initialMonthlyCount = initialUsage.ApiCallsThisMonth;

        // Act
        await _tenantService.RecordUsageAsync(tenantId, 2, 150.5, true);

        // Assert
        var updatedUsage = await _tenantService.GetTenantUsageAsync(tenantId);
        Assert.AreEqual(initialDailyCount + 2, updatedUsage.ApiCallsToday);
        Assert.AreEqual(initialMonthlyCount + 2, updatedUsage.ApiCallsThisMonth);
        Assert.IsTrue(updatedUsage.AverageResponseTimeMs > 0);
    }

    [TestMethod]
    public async Task RecordUsageAsync_FailedRequest_UpdatesErrorRate()
    {
        // Arrange
        var tenantId = "test-tenant-1";

        // Act - Record some successful requests first
        await _tenantService.RecordUsageAsync(tenantId, 1, 100, true);
        await _tenantService.RecordUsageAsync(tenantId, 1, 100, true);
        await _tenantService.RecordUsageAsync(tenantId, 1, 100, true);

        // Then record a failed request
        await _tenantService.RecordUsageAsync(tenantId, 1, 100, false);

        // Assert
        var usage = await _tenantService.GetTenantUsageAsync(tenantId);
        Assert.IsTrue(usage.ErrorRatePercentage > 0);
        Assert.IsTrue(usage.ErrorRatePercentage <= 100);
    }

    [TestMethod]
    public async Task ValidateSubscriptionAsync_ActiveTenant_ReturnsValid()
    {
        // Arrange
        var tenantId = "test-tenant-1";

        // Act
        var result = await _tenantService.ValidateSubscriptionAsync(tenantId);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(TenantStatus.Active, result.Status);
    }

    [TestMethod]
    public async Task GetTenantPermissionsAsync_BasicTier_ReturnsCorrectPermissions()
    {
        // Arrange
        var tenantId = "test-tenant-1"; // Should be Basic tier

        // Act
        var permissions = await _tenantService.GetTenantPermissionsAsync(tenantId);

        // Assert
        Assert.IsTrue(permissions.Count > 0);
        Assert.IsTrue(permissions.Contains(Permissions.FleetQuery.Id));
        Assert.IsTrue(permissions.Contains(Permissions.AgentFuel.Id));
        Assert.IsTrue(permissions.Contains(Permissions.DataRealTime.Id));
    }

    [TestMethod]
    public async Task GetTenantPermissionsAsync_PremiumTier_HasAdvancedPermissions()
    {
        // Arrange
        var tenantId = "premium-tenant"; // Should be Premium tier

        // Act
        var permissions = await _tenantService.GetTenantPermissionsAsync(tenantId);

        // Assert
        Assert.IsTrue(permissions.Count > 0);
        Assert.IsTrue(permissions.Contains(Permissions.FleetQueryAdvanced.Id));
        Assert.IsTrue(permissions.Contains(Permissions.DataHistoricalExtended.Id));
        Assert.IsTrue(permissions.Contains(Permissions.DataExport.Id));
    }

    [TestMethod]
    public async Task SuspendTenantAsync_ActiveTenant_ChangeStatusToSuspended()
    {
        // Arrange
        var tenantConfig = new TenantConfiguration
        {
            Name = "Test Suspension",
            Status = TenantStatus.Active,
            Tier = TenantTier.Basic
        }; var tenant = await _tenantService.CreateTenantAsync(tenantConfig);
        tenant.Status = TenantStatus.Active;
        await _tenantService.UpdateTenantAsync(tenant);

        // Act
        await _tenantService.SuspendTenantAsync(tenant.TenantId, "Payment overdue");

        // Assert
        var updatedTenant = await _tenantService.GetTenantConfigurationAsync(tenant.TenantId);
        Assert.IsNotNull(updatedTenant);
        Assert.AreEqual(TenantStatus.Suspended, updatedTenant.Status);
        Assert.IsTrue(updatedTenant.Metadata.ContainsKey("suspensionReason"));
        Assert.AreEqual("Payment overdue", updatedTenant.Metadata["suspensionReason"]);
    }

    [TestMethod]
    public async Task ReactivateTenantAsync_SuspendedTenant_ChangeStatusToActive()
    {
        // Arrange
        var tenantConfig = new TenantConfiguration
        {
            Name = "Test Reactivation",
            Status = TenantStatus.Active,
            Tier = TenantTier.Basic
        };
        var tenant = await _tenantService.CreateTenantAsync(tenantConfig);
        await _tenantService.SuspendTenantAsync(tenant.TenantId, "Test suspension");

        // Act
        await _tenantService.ReactivateTenantAsync(tenant.TenantId);

        // Assert
        var updatedTenant = await _tenantService.GetTenantConfigurationAsync(tenant.TenantId);
        Assert.IsNotNull(updatedTenant);
        Assert.AreEqual(TenantStatus.Active, updatedTenant.Status);
        Assert.IsFalse(updatedTenant.Metadata.ContainsKey("suspensionReason"));
        Assert.IsTrue(updatedTenant.Metadata.ContainsKey("reactivatedAt"));
    }

    [TestMethod]
    public async Task ListTenantsAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var activeFilter = TenantStatus.Active;

        // Act
        var activeTenants = await _tenantService.ListTenantsAsync(activeFilter);

        // Assert
        Assert.IsTrue(activeTenants.Count > 0);
        Assert.IsTrue(activeTenants.All(t => t.Status == TenantStatus.Active));
    }

    [TestMethod]
    public async Task ListTenantsAsync_WithTierFilter_ReturnsFilteredResults()
    {
        // Arrange
        var tierFilter = TenantTier.Premium;

        // Act
        var premiumTenants = await _tenantService.ListTenantsAsync(tier: tierFilter);

        // Assert
        Assert.IsTrue(premiumTenants.Count > 0);
        Assert.IsTrue(premiumTenants.All(t => t.Tier == TenantTier.Premium));
    }

    [TestMethod]
    public void Permissions_AllPermissions_AreWellDefined()
    {
        // Act
        var allPermissions = Permissions.All;

        // Assert
        Assert.IsTrue(allPermissions.Count > 0);
        Assert.IsTrue(allPermissions.All(p => !string.IsNullOrEmpty(p.Id)));
        Assert.IsTrue(allPermissions.All(p => !string.IsNullOrEmpty(p.Name)));
        Assert.IsTrue(allPermissions.All(p => !string.IsNullOrEmpty(p.Category)));

        // Check that IDs are unique
        var ids = allPermissions.Select(p => p.Id).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
    }

    [TestMethod]
    public void Roles_AllRoles_HaveValidPermissions()
    {
        // Arrange
        var allPermissionIds = Permissions.All.Select(p => p.Id).ToHashSet();
        var roleNames = Roles.AllRoleNames;

        // Act & Assert
        foreach (var roleName in roleNames)
        {
            var rolePermissions = Roles.GetRolePermissions(roleName);
            Assert.IsTrue(rolePermissions.Count > 0, $"Role {roleName} has no permissions");

            foreach (var permission in rolePermissions)
            {
                Assert.IsTrue(allPermissionIds.Contains(permission),
                    $"Role {roleName} contains invalid permission: {permission}");
            }
        }
    }

    [TestMethod]
    public async Task CheckRateLimitAsync_ExceedsLimit_ReturnsDenied()
    {
        // Arrange
        var tenantConfig = new TenantConfiguration
        {
            Name = "Rate Limit Test",
            Tier = TenantTier.Free, // Has very low limits
            Status = TenantStatus.Active
        };
        var tenant = await _tenantService.CreateTenantAsync(tenantConfig);

        // Act - Exceed the daily limit
        for (int i = 0; i < 150; i++) // Free tier has 100/day limit
        {
            await _tenantService.RecordUsageAsync(tenant.TenantId, 1, 100, true);
        }

        var rateLimitResult = await _tenantService.CheckRateLimitAsync(tenant.TenantId);

        // Assert
        Assert.IsFalse(rateLimitResult.IsAllowed);
        Assert.IsNotNull(rateLimitResult.DenialReason);
        Assert.IsTrue(rateLimitResult.DenialReason.Contains("rate limit"));
    }
}

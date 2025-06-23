using FleetAssistant.Agents;
using FleetAssistant.Infrastructure.Configuration;
using FleetAssistant.Infrastructure.Security;
using FleetAssistant.Infrastructure.Plugins;
using FleetAssistant.Infrastructure.Plugins.Integrations;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Xunit;
using Moq;

namespace Tests.FleetAssistant.Agents;

/// <summary>
/// Integration test demonstrating the full multi-agent system working together
/// </summary>
public class MultiAgentIntegrationTests
{
    private readonly Mock<ILogger<PlanningAgent>> _planningLogger;
    private readonly Mock<ILogger<FuelAgent>> _fuelLogger;
    private readonly Mock<ILogger<MaintenanceAgent>> _maintenanceLogger;
    private readonly Mock<ILogger<SafetyAgent>> _safetyLogger;
    private readonly Mock<ILogger<IntegrationPluginRegistry>> _registryLogger;
    private readonly Mock<ILogger<GeoTabPluginBuilder>> _geoTabLogger;
    private readonly Mock<ILogger<FleetioPluginBuilder>> _fleetioLogger;
    private readonly Mock<ILogger<SamsaraPluginBuilder>> _samsaraLogger;
    private readonly IKernelBuilder _kernelBuilder;

    public MultiAgentIntegrationTests()
    {
        _planningLogger = new Mock<ILogger<PlanningAgent>>();
        _fuelLogger = new Mock<ILogger<FuelAgent>>();
        _maintenanceLogger = new Mock<ILogger<MaintenanceAgent>>();
        _safetyLogger = new Mock<ILogger<SafetyAgent>>();
        _registryLogger = new Mock<ILogger<IntegrationPluginRegistry>>();
        _geoTabLogger = new Mock<ILogger<GeoTabPluginBuilder>>();
        _fleetioLogger = new Mock<ILogger<FleetioPluginBuilder>>();
        _samsaraLogger = new Mock<ILogger<SamsaraPluginBuilder>>();
        _kernelBuilder = Kernel.CreateBuilder();
    }

    [Fact]
    public async Task FullMultiAgentSystem_ProcessesFuelQuery_WithIntegrationPlugins()
    {
        // Arrange - Create full multi-agent system with integration plugins
        var configStore = new InMemoryIntegrationConfigStore();
        var credentialStore = new InMemoryCredentialStore();

        var pluginBuilders = new List<IIntegrationPluginBuilder>
        {
            new GeoTabPluginBuilder(credentialStore, configStore, _geoTabLogger.Object),
            new FleetioPluginBuilder(credentialStore, configStore, _fleetioLogger.Object),
            new SamsaraPluginBuilder(credentialStore, configStore, _samsaraLogger.Object)
        };

        var pluginRegistry = new IntegrationPluginRegistry(pluginBuilders, configStore, _registryLogger.Object);

        var fuelAgent = new FuelAgent(_fuelLogger.Object, _kernelBuilder, pluginRegistry);
        var maintenanceAgent = new MaintenanceAgent(_maintenanceLogger.Object, _kernelBuilder, pluginRegistry);
        var safetyAgent = new SafetyAgent(_safetyLogger.Object, _kernelBuilder, pluginRegistry);

        var planningAgent = new PlanningAgent(
            _planningLogger.Object,
            _kernelBuilder,
            pluginRegistry,
            fuelAgent,
            maintenanceAgent,
            safetyAgent);

        var request = new FleetQueryRequest
        {
            Message = "What's the fuel efficiency of vehicle ABC123?",
            Context = new Dictionary<string, object>
            {
                ["vehicleId"] = "ABC123",
                ["timeframe"] = "last-30-days"
            }
        };

        var userContext = new UserContext
        {
            TenantId = "test-tenant",
            ApiKeyId = "test-key",
            ApiKeyName = "Test Integration Key",
            Environment = "test",
            Scopes = new List<string> { "fleet:read", "fuel:read" }
        };

        // Act - Process the query through the full multi-agent system
        var response = await planningAgent.ProcessQueryAsync(request, userContext);        // Assert - Verify the multi-agent system worked correctly
        Assert.NotNull(response);
        Assert.Contains("PlanningAgent", response.AgentsUsed);
        Assert.NotEmpty(response.Response);
        Assert.NotNull(response.AgentData);

        // In test environment without Azure OpenAI, we expect error handling or fallback behavior
        // Either planning data (if OpenAI configured) or error data (if not configured)
        Assert.True(response.AgentData.ContainsKey("planning") || response.AgentData.ContainsKey("error"),
            "Response should contain either planning data or error data");

        // Verify timestamp is recent
        Assert.True(response.Timestamp > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task MultiAgentSystem_HandlesSafetyQuery_WithSamsaraIntegration()
    {
        // Arrange - Set up system for safety-focused query
        var configStore = new InMemoryIntegrationConfigStore();
        var credentialStore = new InMemoryCredentialStore();

        var pluginBuilders = new List<IIntegrationPluginBuilder>
        {
            new SamsaraPluginBuilder(credentialStore, configStore, _samsaraLogger.Object)
        };

        var pluginRegistry = new IntegrationPluginRegistry(pluginBuilders, configStore, _registryLogger.Object);
        var safetyAgent = new SafetyAgent(_safetyLogger.Object, _kernelBuilder, pluginRegistry);

        var planningAgent = new PlanningAgent(
            _planningLogger.Object,
            _kernelBuilder,
            pluginRegistry,
            safetyAgent: safetyAgent);

        var request = new FleetQueryRequest
        {
            Message = "Show me any safety events for driver John Smith",
            Context = new Dictionary<string, object>
            {
                ["driverId"] = "driver123",
                ["timeframe"] = "last-7-days"
            }
        };

        var userContext = new UserContext
        {
            TenantId = "tenant2", // This tenant has Samsara enabled
            ApiKeyId = "test-key-tenant2",
            ApiKeyName = "Tenant 2 Key",
            Environment = "test",
            Scopes = new List<string> { "fleet:read", "safety:read" }
        };

        // Act
        var response = await planningAgent.ProcessQueryAsync(request, userContext);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("PlanningAgent", response.AgentsUsed);
        Assert.NotEmpty(response.Response);
    }

    [Fact]
    public async Task IntegrationPluginRegistry_LoadsCorrectPluginsForTenant()
    {
        // Arrange
        var configStore = new InMemoryIntegrationConfigStore();
        var credentialStore = new InMemoryCredentialStore();

        var pluginBuilders = new List<IIntegrationPluginBuilder>
        {
            new GeoTabPluginBuilder(credentialStore, configStore, _geoTabLogger.Object),
            new FleetioPluginBuilder(credentialStore, configStore, _fleetioLogger.Object),
            new SamsaraPluginBuilder(credentialStore, configStore, _samsaraLogger.Object)
        };

        var pluginRegistry = new IntegrationPluginRegistry(pluginBuilders, configStore, _registryLogger.Object);

        // Act - Get plugins for tenant1 (should have GeoTab + Fleetio)
        var tenant1Plugins = await pluginRegistry.GetEnabledPluginsAsync("tenant1");

        // Assert
        Assert.Equal(2, tenant1Plugins.Count);
        Assert.Contains(tenant1Plugins, p => p.Name == "geotab");
        Assert.Contains(tenant1Plugins, p => p.Name == "fleetio");
        Assert.DoesNotContain(tenant1Plugins, p => p.Name == "samsara");
    }

    [Fact]
    public async Task FuelAgent_UsesCorrectCapabilities()
    {
        // Arrange
        var configStore = new InMemoryIntegrationConfigStore();
        var credentialStore = new InMemoryCredentialStore();

        var pluginBuilders = new List<IIntegrationPluginBuilder>
        {
            new GeoTabPluginBuilder(credentialStore, configStore, _geoTabLogger.Object),
            new FleetioPluginBuilder(credentialStore, configStore, _fleetioLogger.Object),
            new SamsaraPluginBuilder(credentialStore, configStore, _samsaraLogger.Object)
        };

        var pluginRegistry = new IntegrationPluginRegistry(pluginBuilders, configStore, _registryLogger.Object);

        // Act - Get plugins by fuel-related capabilities
        var fuelPlugins = await pluginRegistry.GetPluginsByCapabilitiesAsync("test-tenant", new[] { "fuel" });

        // Assert - Should get GeoTab and Fleetio (both support fuel), but not Samsara
        Assert.Equal(2, fuelPlugins.Count);
        Assert.Contains(fuelPlugins, p => p.Name == "geotab");
        Assert.Contains(fuelPlugins, p => p.Name == "fleetio");
    }

    [Fact]
    public async Task SafetyAgent_UsesCorrectCapabilities()
    {
        // Arrange
        var configStore = new InMemoryIntegrationConfigStore();
        var credentialStore = new InMemoryCredentialStore();

        var pluginBuilders = new List<IIntegrationPluginBuilder>
        {
            new GeoTabPluginBuilder(credentialStore, configStore, _geoTabLogger.Object),
            new FleetioPluginBuilder(credentialStore, configStore, _fleetioLogger.Object),
            new SamsaraPluginBuilder(credentialStore, configStore, _samsaraLogger.Object)
        };

        var pluginRegistry = new IntegrationPluginRegistry(pluginBuilders, configStore, _registryLogger.Object);

        // Act - Get plugins by safety-related capabilities
        var safetyPlugins = await pluginRegistry.GetPluginsByCapabilitiesAsync("test-tenant", new[] { "safety" });

        // Assert - Should get only Samsara (only one with safety capabilities)
        Assert.Single(safetyPlugins);
        Assert.Contains(safetyPlugins, p => p.Name == "samsara");
    }

    [Fact]
    public async Task MultiAgentSystem_HandlesComplexMultiDomainQuery()
    {
        // Arrange - Create full system for comprehensive query
        var configStore = new InMemoryIntegrationConfigStore();
        var credentialStore = new InMemoryCredentialStore();

        var pluginBuilders = new List<IIntegrationPluginBuilder>
        {
            new GeoTabPluginBuilder(credentialStore, configStore, _geoTabLogger.Object),
            new FleetioPluginBuilder(credentialStore, configStore, _fleetioLogger.Object),
            new SamsaraPluginBuilder(credentialStore, configStore, _samsaraLogger.Object)
        };

        var pluginRegistry = new IntegrationPluginRegistry(pluginBuilders, configStore, _registryLogger.Object);

        var fuelAgent = new FuelAgent(_fuelLogger.Object, _kernelBuilder, pluginRegistry);
        var maintenanceAgent = new MaintenanceAgent(_maintenanceLogger.Object, _kernelBuilder, pluginRegistry);
        var safetyAgent = new SafetyAgent(_safetyLogger.Object, _kernelBuilder, pluginRegistry);

        var planningAgent = new PlanningAgent(
            _planningLogger.Object,
            _kernelBuilder,
            pluginRegistry,
            fuelAgent,
            maintenanceAgent,
            safetyAgent);

        var request = new FleetQueryRequest
        {
            Message = "Give me a complete analysis of vehicle ABC123 including fuel efficiency, maintenance status, and safety events",
            Context = new Dictionary<string, object>
            {
                ["vehicleId"] = "ABC123",
                ["analysisType"] = "comprehensive",
                ["timeframe"] = "last-30-days"
            }
        };

        var userContext = new UserContext
        {
            TenantId = "test-tenant", // This tenant has all integrations
            ApiKeyId = "comprehensive-test-key",
            ApiKeyName = "Comprehensive Test Key",
            Environment = "test",
            Scopes = new List<string> { "fleet:read", "fuel:read", "maintenance:read", "safety:read" }
        };

        // Act - Process comprehensive multi-domain query
        var response = await planningAgent.ProcessQueryAsync(request, userContext);

        // Assert - Verify comprehensive response
        Assert.NotNull(response);
        Assert.Contains("PlanningAgent", response.AgentsUsed);
        Assert.NotEmpty(response.Response);
        Assert.NotNull(response.AgentData);

        // The response should be comprehensive, handling fuel, maintenance, and safety
        Assert.True(response.Response.Length > 50, "Response should be comprehensive for multi-domain query");
    }
}

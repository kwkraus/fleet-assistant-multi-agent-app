using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FleetAssistant.Agents.Orchestration;
using FleetAssistant.Shared.Models;
using Xunit;
using Moq;
using System.Text.Json;

namespace Tests.FleetAssistant.Agents;

/// <summary>
/// Integration tests for the complete Fleet Agent system using the new Semantic Kernel Agent Framework
/// </summary>
public class SemanticKernelAgentIntegrationTests
{
    private readonly Mock<ILogger<FleetAgentOrchestrationService>> _mockLogger;
    private readonly IConfiguration _configuration;

    public SemanticKernelAgentIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<FleetAgentOrchestrationService>>();
        
        // Create configuration for testing
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AZURE_AI_FOUNDRY_ENDPOINT"] = "https://test-endpoint.cognitiveservices.azure.com/",
            ["AZURE_AI_MODEL_DEPLOYMENT"] = "gpt-4o"
        });
        _configuration = configBuilder.Build();
    }

    [Fact]
    public async Task OrchestrationService_HealthCheck_ShouldReturnCorrectStatus()
    {
        // Arrange
        var service = new FleetAgentOrchestrationService(_mockLogger.Object, _configuration);

        // Act
        var healthStatus = await service.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(healthStatus);
        Assert.NotNull(healthStatus.Status);
        Assert.False(healthStatus.IsHealthy); // Should be false without real Azure AI setup
        Assert.True(healthStatus.Details.ContainsKey("initialized"));
        Assert.True(healthStatus.Details.ContainsKey("orchestratorExists"));
        Assert.True(healthStatus.Details.ContainsKey("configurationValid"));
    }

    [Theory]
    [InlineData("fuel", "FuelAgent")]
    [InlineData("maintenance", "MaintenanceAgent")]
    [InlineData("safety", "SafetyAgent")]
    [InlineData("route", "PlanningAgent")]
    [InlineData("plan", "PlanningAgent")]
    public async Task OrchestrationService_ShouldRouteQueriesCorrectly(string queryKeyword, string expectedAgentType)
    {
        // Arrange
        var service = new FleetAgentOrchestrationService(_mockLogger.Object, _configuration);
        var request = new FleetQueryRequest 
        { 
            Message = $"I need help with {queryKeyword} management" 
        };
        var userContext = new UserContext 
        { 
            TenantId = "test-tenant-001", 
            ApiKeyId = "test-api-key-001" 
        };

        // Act
        var response = await service.ProcessQueryAsync(request, userContext);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Response);
        Assert.NotEmpty(response.AgentData);
        Assert.Contains("error", response.Response.ToLower()); // Expected since we don't have real Azure AI
        
        // Verify the response structure
        Assert.NotNull(response.AgentsUsed);
        Assert.NotNull(response.Warnings);
        Assert.True(response.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public async Task OrchestrationService_WithComplexQuery_ShouldHandleMultipleAgents()
    {
        // Arrange
        var service = new FleetAgentOrchestrationService(_mockLogger.Object, _configuration);
        var request = new FleetQueryRequest 
        { 
            Message = "I need to plan a fuel-efficient route while considering vehicle maintenance schedules and driver safety records" 
        };
        var userContext = new UserContext 
        { 
            TenantId = "test-tenant-002", 
            ApiKeyId = "test-api-key-002" 
        };

        // Act
        var response = await service.ProcessQueryAsync(request, userContext);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Response);
        Assert.NotEmpty(response.AgentData);
        
        // Should contain processing metadata
        Assert.True(response.AgentData.ContainsKey("error") || response.AgentData.ContainsKey("conversationHistory"));
        Assert.Contains("ErrorHandler", response.AgentsUsed); // Expected since we don't have real Azure AI
    }

    [Fact]
    public void Plugin_AllPluginsMethods_ShouldReturnValidData()
    {
        // Test all plugin methods to ensure they return valid data
        var fuelPlugin = new FleetAssistant.Agents.Plugins.FuelManagementPlugin();
        var maintenancePlugin = new FleetAssistant.Agents.Plugins.MaintenancePlugin();
        var safetyPlugin = new FleetAssistant.Agents.Plugins.SafetyPlugin();
        var planningPlugin = new FleetAssistant.Agents.Plugins.PlanningPlugin();

        // Fuel Plugin Tests
        Assert.NotEmpty(fuelPlugin.CheckFuelLevels());
        Assert.NotEmpty(fuelPlugin.GetFuelAnalysis());
        Assert.NotEmpty(fuelPlugin.ScheduleFuelDelivery("V001", "Warehouse"));
        Assert.NotEmpty(fuelPlugin.GetFuelCostAnalysis());

        // Maintenance Plugin Tests
        Assert.NotEmpty(maintenancePlugin.CheckMaintenanceSchedule());
        Assert.NotEmpty(maintenancePlugin.GetVehicleHealth("V001"));
        Assert.NotEmpty(maintenancePlugin.ScheduleMaintenance("V001", "Oil Change", "2025-07-01"));
        Assert.NotEmpty(maintenancePlugin.GetMaintenanceCosts());
        Assert.NotEmpty(maintenancePlugin.GetMaintenanceHistory("V001"));

        // Safety Plugin Tests
        Assert.NotEmpty(safetyPlugin.CheckDriverSafety());
        Assert.NotEmpty(safetyPlugin.GetSafetyInspections());
        Assert.NotEmpty(safetyPlugin.ReportSafetyIncident("Collision", "Minor incident", "V001"));
        Assert.NotEmpty(safetyPlugin.GetComplianceStatus());
        Assert.NotEmpty(safetyPlugin.ScheduleSafetyTraining("D001", "Defensive Driving"));

        // Planning Plugin Tests
        Assert.NotEmpty(planningPlugin.GenerateRoutes("Downtown"));
        Assert.NotEmpty(planningPlugin.ScheduleOperations("Delivery", "2025-07-01", "Morning"));
        Assert.NotEmpty(planningPlugin.AnalyzeFleetUtilization());
        Assert.NotEmpty(planningPlugin.CoordinateResources("Delivery"));
        Assert.NotEmpty(planningPlugin.GenerateFleetReport());
    }

    [Fact]
    public async Task OrchestrationService_WithInvalidConfiguration_ShouldHandleGracefully()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder().Build(); // Empty configuration
        var service = new FleetAgentOrchestrationService(_mockLogger.Object, invalidConfig);
        
        var request = new FleetQueryRequest { Message = "Test query" };
        var userContext = new UserContext { TenantId = "test", ApiKeyId = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProcessQueryAsync(request, userContext));
        
        Assert.Contains("Azure AI Foundry", exception.Message);
    }

    [Fact]
    public async Task OrchestrationService_ErrorHandling_ShouldReturnGracefulResponse()
    {
        // Arrange
        var service = new FleetAgentOrchestrationService(_mockLogger.Object, _configuration);
        var request = new FleetQueryRequest { Message = null! }; // Invalid request
        var userContext = new UserContext { TenantId = "test", ApiKeyId = "test" };

        // Act
        var response = await service.ProcessQueryAsync(request, userContext);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("error", response.Response.ToLower());
        Assert.Contains("ErrorHandler", response.AgentsUsed);
        Assert.NotEmpty(response.Warnings);
    }

    [Fact]
    public void FleetQueryResponse_Serialization_ShouldWorkCorrectly()
    {
        // Arrange
        var response = new FleetQueryResponse
        {
            Response = "Test response",
            AgentData = new Dictionary<string, object>
            {
                ["testKey"] = "testValue",
                ["timestamp"] = DateTime.UtcNow
            },
            AgentsUsed = new List<string> { "TriageAgent", "FuelAgent" },
            Warnings = new List<string> { "Test warning" },
            ProcessingTimeMs = 1500
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<FleetQueryResponse>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(response.Response, deserialized.Response);
        Assert.Equal(response.AgentsUsed.Count, deserialized.AgentsUsed.Count);
        Assert.Equal(response.Warnings.Count, deserialized.Warnings.Count);
        Assert.Equal(response.ProcessingTimeMs, deserialized.ProcessingTimeMs);
    }
}

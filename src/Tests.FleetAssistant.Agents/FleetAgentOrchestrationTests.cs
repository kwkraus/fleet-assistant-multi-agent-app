using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FleetAssistant.Agents.Orchestration;
using FleetAssistant.Shared.Models;
using Xunit;
using Moq;

namespace Tests.FleetAssistant.Agents;

/// <summary>
/// Tests for the Fleet Agent Orchestration Service using Semantic Kernel Agent Framework
/// </summary>
public class FleetAgentOrchestrationTests
{
    private readonly Mock<ILogger<FleetAgentOrchestrationService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public FleetAgentOrchestrationTests()
    {
        _mockLogger = new Mock<ILogger<FleetAgentOrchestrationService>>();
        _mockConfiguration = new Mock<IConfiguration>();
    }

    [Fact]
    public void FleetAgentOrchestrationService_Constructor_ShouldInitialize()
    {
        // Arrange & Act
        var service = new FleetAgentOrchestrationService(_mockLogger.Object, _mockConfiguration.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WhenNotInitialized_ShouldReturnUnhealthy()
    {
        // Arrange
        var service = new FleetAgentOrchestrationService(_mockLogger.Object, _mockConfiguration.Object);

        // Act
        var healthStatus = await service.GetHealthStatusAsync();

        // Assert
        Assert.False(healthStatus.IsHealthy);
        Assert.Contains("not initialized", healthStatus.Status);
    }

    [Fact]
    public async Task ProcessQueryAsync_WithValidRequest_ShouldInitializeAndProcess()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["AZURE_AI_FOUNDRY_ENDPOINT"])
            .Returns("https://test-endpoint.cognitiveservices.azure.com/");
        _mockConfiguration.Setup(c => c["AZURE_AI_MODEL_DEPLOYMENT"])
            .Returns("gpt-4o");

        var service = new FleetAgentOrchestrationService(_mockLogger.Object, _mockConfiguration.Object);
        
        var request = new FleetQueryRequest { Message = "What are the fuel levels?" };
        var userContext = new UserContext 
        { 
            TenantId = "test-tenant", 
            ApiKeyId = "test-key" 
        };

        // Act & Assert
        // Note: This will fail without actual Azure AI setup, but tests the initialization logic
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProcessQueryAsync(request, userContext));
        
        // Should attempt to initialize (and fail due to missing real credentials)
        Assert.Contains("Azure AI Foundry", exception.Message);
    }

    [Theory]
    [InlineData("What are the current fuel levels?")]
    [InlineData("Schedule maintenance for vehicle V001")]
    [InlineData("Check driver safety scores")]
    [InlineData("Plan optimal routes for delivery")]
    public async Task ProcessQueryAsync_WithDifferentQueryTypes_ShouldHandleGracefully(string message)
    {
        // Arrange
        var service = new FleetAgentOrchestrationService(_mockLogger.Object, _mockConfiguration.Object);
        
        var request = new FleetQueryRequest { Message = message };
        var userContext = new UserContext 
        { 
            TenantId = "test-tenant", 
            ApiKeyId = "test-key" 
        };

        // Act
        var response = await service.ProcessQueryAsync(request, userContext);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Response);
        Assert.Contains("AgentsUsed", response.AgentData.Keys);
    }
}

/// <summary>
/// Tests for individual agent plugins
/// </summary>
public class AgentPluginTests
{
    [Fact]
    public void FuelManagementPlugin_CheckFuelLevels_ShouldReturnData()
    {
        // Arrange
        var plugin = new FleetAssistant.Agents.Plugins.FuelManagementPlugin();

        // Act
        var result = plugin.CheckFuelLevels();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("fuel levels", result.ToLower());
    }

    [Fact]
    public void MaintenancePlugin_CheckMaintenanceSchedule_ShouldReturnData()
    {
        // Arrange
        var plugin = new FleetAssistant.Agents.Plugins.MaintenancePlugin();

        // Act
        var result = plugin.CheckMaintenanceSchedule();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("maintenance", result.ToLower());
    }

    [Fact]
    public void SafetyPlugin_CheckDriverSafety_ShouldReturnData()
    {
        // Arrange
        var plugin = new FleetAssistant.Agents.Plugins.SafetyPlugin();

        // Act
        var result = plugin.CheckDriverSafety();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("safety", result.ToLower());
    }

    [Fact]
    public void PlanningPlugin_GenerateRoutes_ShouldReturnData()
    {
        // Arrange
        var plugin = new FleetAssistant.Agents.Plugins.PlanningPlugin();

        // Act
        var result = plugin.GenerateRoutes("Downtown");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("route", result.ToLower());
    }

    [Fact]
    public void FuelManagementPlugin_GetFuelAnalysis_ShouldReturnAnalysis()
    {
        // Arrange
        var plugin = new FleetAssistant.Agents.Plugins.FuelManagementPlugin();

        // Act
        var result = plugin.GetFuelAnalysis("week");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("analysis", result.ToLower());
        Assert.Contains("week", result.ToLower());
    }

    [Fact]
    public void MaintenancePlugin_ScheduleMaintenance_ShouldReturnScheduleInfo()
    {
        // Arrange
        var plugin = new FleetAssistant.Agents.Plugins.MaintenancePlugin();

        // Act
        var result = plugin.ScheduleMaintenance("V001", "Oil Change", "2025-07-01");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("V001", result);
        Assert.Contains("Oil Change", result);
    }

    [Fact]
    public void SafetyPlugin_ReportSafetyIncident_ShouldReturnIncidentId()
    {
        // Arrange
        var plugin = new FleetAssistant.Agents.Plugins.SafetyPlugin();

        // Act
        var result = plugin.ReportSafetyIncident("Collision", "Minor fender bender", "V001");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("incident", result.ToLower());
        Assert.Contains("V001", result);
    }

    [Fact]
    public void PlanningPlugin_CoordinateResources_ShouldReturnCoordination()
    {
        // Arrange
        var plugin = new FleetAssistant.Agents.Plugins.PlanningPlugin();

        // Act
        var result = plugin.CoordinateResources("Delivery", "high");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("coordination", result.ToLower());
        Assert.Contains("high", result.ToLower());
    }
}

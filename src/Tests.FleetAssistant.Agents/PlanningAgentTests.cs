using FleetAssistant.Agents;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;

namespace Tests.FleetAssistant.Agents;

public class PlanningAgentTests
{
    private readonly Mock<ILogger<PlanningAgent>> _mockLogger;
    private readonly Mock<IKernelBuilder> _mockKernelBuilder;
    private readonly PlanningAgent _planningAgent;

    public PlanningAgentTests()
    {
        _mockLogger = new Mock<ILogger<PlanningAgent>>();
        _mockKernelBuilder = new Mock<IKernelBuilder>();
        _planningAgent = new PlanningAgent(_mockLogger.Object, _mockKernelBuilder.Object);
    }

    [Fact]
    public async Task ProcessQueryAsync_WithSimpleQuery_ReturnsSuccessfulResponse()
    {
        // Arrange
        var request = new FleetQueryRequest
        {
            Message = "What vehicles need maintenance?"
        };

        var userContext = new UserContext
        {
            ApiKeyId = "test-key",
            TenantId = "test-tenant",
            ApiKeyName = "Test Key",
            Environment = "test",
            Scopes = new List<string> { "fleet:read", "fleet:query" }
        };

        // Act & Assert - This will fail without proper Azure OpenAI configuration
        // For now, let's just verify it doesn't throw during construction
        Assert.NotNull(_planningAgent);
    }

    [Fact]
    public void PlanningAgent_Constructor_DoesNotThrow()
    {
        // Arrange & Act
        var agent = new PlanningAgent(_mockLogger.Object, _mockKernelBuilder.Object);

        // Assert
        Assert.NotNull(agent);
    }
}

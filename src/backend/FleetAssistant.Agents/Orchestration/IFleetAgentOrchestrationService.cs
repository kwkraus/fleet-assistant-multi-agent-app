using FleetAssistant.Shared.Models;

namespace FleetAssistant.Agents.Orchestration;

/// <summary>
/// Interface for the fleet agent orchestration service
/// </summary>
public interface IFleetAgentOrchestrationService
{
    /// <summary>
    /// Processes a fleet query using the agent orchestration pattern
    /// </summary>
    /// <param name="request">The fleet query request</param>
    /// <param name="userContext">User context with tenant information</param>
    /// <returns>Response from the orchestrated agents</returns>
    Task<FleetQueryResponse> ProcessQueryAsync(FleetQueryRequest request, UserContext userContext);

    /// <summary>
    /// Initializes the orchestration service
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Gets the health status of the orchestration service
    /// </summary>
    Task<ServiceHealthStatus> GetHealthStatusAsync();
}

/// <summary>
/// Health status for the orchestration service
/// </summary>
public class ServiceHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

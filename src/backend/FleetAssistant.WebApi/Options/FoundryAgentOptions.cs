using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.WebApi.Options;

/// <summary>
/// Configuration options for Azure AI Foundry Agent Service
/// </summary>
public class FoundryAgentOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "FoundryAgentService";

    /// <summary>
    /// Azure AI Foundry agent ID
    /// </summary>
    [Required]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AI Foundry agent endpoint URL
    /// </summary>
    [Required]
    public string AgentEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Delay in milliseconds between polling attempts when checking run status
    /// </summary>
    public int RunPollingDelayMs { get; set; } = 1000; // Default 1 second

    /// <summary>
    /// Delay in milliseconds between streaming words in responses
    /// </summary>
    public int StreamingDelayMs { get; set; } = 50; // Default 50ms for smooth streaming

}

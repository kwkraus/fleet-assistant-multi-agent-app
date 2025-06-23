namespace FleetAssistant.Shared.Models;

/// <summary>
/// Response model for fleet query endpoint
/// </summary>
public class FleetQueryResponse
{
    /// <summary>
    /// The AI assistant's response to the user's query
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Additional data from various agents (fuel, maintenance, etc.)
    /// </summary>
    public Dictionary<string, object> AgentData { get; set; } = new();

    /// <summary>
    /// Optional warnings from failed integrations or partial data
    /// </summary>
    public List<string>? Warnings { get; set; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// List of agents that were called to generate this response
    /// </summary>
    public List<string> AgentsUsed { get; set; } = new();

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

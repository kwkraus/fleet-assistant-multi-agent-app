using System.ComponentModel.DataAnnotations;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Request model for fleet query endpoint
/// </summary>
public class FleetQueryRequest
{
    /// <summary>
    /// The user's message/query
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional conversation history for context
    /// </summary>
    public List<ConversationMessage>? ConversationHistory { get; set; }

    /// <summary>
    /// Optional additional context (vehicle IDs, timeframes, etc.)
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// Represents a message in the conversation history
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// Role of the message sender (user, assistant, system)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Content of the message
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the message
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

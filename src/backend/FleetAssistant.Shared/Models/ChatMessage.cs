using System.Text.Json.Serialization;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Chat message format compatible with Vercel AI SDK
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Role of the message sender (user, assistant, system)
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Content of the message
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When the message was created (optional for AI SDK compatibility)
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedAt { get; set; }
}

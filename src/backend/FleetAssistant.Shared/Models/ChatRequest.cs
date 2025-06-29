using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Chat request format compatible with Vercel AI SDK
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// Array of chat messages
    /// </summary>
    [Required]
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Optional conversation ID for maintaining context
    /// </summary>
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    /// <summary>
    /// Optional additional options
    /// </summary>
    [JsonPropertyName("options")]
    public Dictionary<string, object>? Options { get; set; }
}

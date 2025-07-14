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
    /// Optional base64 encoded files (maximum 2 files)
    /// </summary>
    [MaxLength(2)]
    [JsonPropertyName("files")]
    public List<Base64File>? Files { get; set; }

    /// <summary>
    /// Optional additional options
    /// </summary>
    [JsonPropertyName("options")]
    public Dictionary<string, object>? Options { get; set; }
}

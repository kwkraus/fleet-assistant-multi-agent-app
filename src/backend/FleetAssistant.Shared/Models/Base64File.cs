using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FleetAssistant.Shared.Models;

/// <summary>
/// Represents a base64 encoded file for chat requests
/// </summary>
public class Base64File
{
    /// <summary>
    /// Original file name
    /// </summary>
    [Required, MaxLength(255)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// MIME type of the file
    /// </summary>
    [Required, MaxLength(100)]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Range(1, 3_145_728)] // Default 3MB limit
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Base64 encoded file content
    /// </summary>
    [Required]
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

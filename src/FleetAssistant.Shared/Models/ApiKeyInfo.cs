namespace FleetAssistant.Shared.Models;

/// <summary>
/// API Key configuration for a tenant
/// </summary>
public class ApiKeyInfo
{
    /// <summary>
    /// Unique identifier for the API key
    /// </summary>
    public string ApiKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Hashed version of the API key (for storage)
    /// </summary>
    public string HashedApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the API key
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID this API key belongs to
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Environment this API key is valid for
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Scopes/permissions for this API key
    /// </summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// When the API key was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the API key was last used
    /// </summary>
    public DateTime LastUsedAt { get; set; }

    /// <summary>
    /// Whether the API key is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional expiration date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

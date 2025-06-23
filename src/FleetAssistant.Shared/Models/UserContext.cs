namespace FleetAssistant.Shared.Models;

/// <summary>
/// User context information extracted from API Key
/// </summary>
public class UserContext
{
    /// <summary>
    /// API Key identifier
    /// </summary>
    public string ApiKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Name/description of the API key (for logging)
    /// </summary>
    public string ApiKeyName { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID associated with this API key
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Environment (dev, staging, prod) for this API key
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Permissions/scopes for this API key
    /// </summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>
    /// When this API key was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this API key last accessed the API
    /// </summary>
    public DateTime LastUsedAt { get; set; }

    /// <summary>
    /// Additional metadata for the API key
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

namespace FleetAssistant.Shared.Models;

/// <summary>
/// User context information extracted from JWT claims
/// </summary>
public class UserContext
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// List of tenant IDs the user has access to
    /// </summary>
    public List<string> AuthorizedTenantIds { get; set; } = new();

    /// <summary>
    /// User's roles within the application
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Primary tenant ID for this request (from context or default)
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Additional claims from the JWT token
    /// </summary>
    public Dictionary<string, string> Claims { get; set; } = new();
}

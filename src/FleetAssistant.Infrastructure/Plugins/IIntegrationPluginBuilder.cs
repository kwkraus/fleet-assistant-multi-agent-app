using Microsoft.SemanticKernel;

namespace FleetAssistant.Infrastructure.Plugins;

/// <summary>
/// Interface for building integration plugins for specific fleet management systems
/// </summary>
public interface IIntegrationPluginBuilder
{
    /// <summary>
    /// Unique key identifying this integration (e.g., "geotab", "fleetio", "samsara")
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Human-readable name of the integration
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// List of capabilities this integration provides
    /// </summary>
    IReadOnlyList<string> Capabilities { get; }

    /// <summary>
    /// Builds a Semantic Kernel plugin for the specified tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID to build the plugin for</param>
    /// <returns>Configured plugin or null if not available for this tenant</returns>
    Task<KernelPlugin?> BuildPluginAsync(string tenantId);

    /// <summary>
    /// Checks if this integration is available for the specified tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID to check</param>
    /// <returns>True if available, false otherwise</returns>
    Task<bool> IsAvailableForTenantAsync(string tenantId);
}

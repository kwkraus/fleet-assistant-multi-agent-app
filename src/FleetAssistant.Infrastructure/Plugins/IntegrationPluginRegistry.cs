using FleetAssistant.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace FleetAssistant.Infrastructure.Plugins;

/// <summary>
/// Registry for managing integration plugin builders
/// </summary>
public interface IIntegrationPluginRegistry
{
    /// <summary>
    /// Gets all available plugin builders
    /// </summary>
    IReadOnlyList<IIntegrationPluginBuilder> GetAllBuilders();

    /// <summary>
    /// Gets a plugin builder by its key
    /// </summary>
    /// <param name="key">Integration key</param>
    /// <returns>Plugin builder or null if not found</returns>
    IIntegrationPluginBuilder? GetBuilder(string key);

    /// <summary>
    /// Gets all enabled plugins for a specific tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>List of available plugins for the tenant</returns>
    Task<IReadOnlyList<KernelPlugin>> GetEnabledPluginsAsync(string tenantId);

    /// <summary>
    /// Gets plugins filtered by capabilities for a specific tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="requiredCapabilities">Required capabilities</param>
    /// <returns>List of plugins that support the required capabilities</returns>
    Task<IReadOnlyList<KernelPlugin>> GetPluginsByCapabilitiesAsync(string tenantId, IEnumerable<string> requiredCapabilities);
}

public class IntegrationPluginRegistry : IIntegrationPluginRegistry
{
    private readonly IReadOnlyList<IIntegrationPluginBuilder> _pluginBuilders;
    private readonly IIntegrationConfigStore _configStore;
    private readonly ILogger<IntegrationPluginRegistry> _logger;

    public IntegrationPluginRegistry(
        IEnumerable<IIntegrationPluginBuilder> pluginBuilders,
        IIntegrationConfigStore configStore,
        ILogger<IntegrationPluginRegistry> logger)
    {
        _pluginBuilders = pluginBuilders.ToList().AsReadOnly();
        _configStore = configStore;
        _logger = logger;

        _logger.LogInformation("Integration plugin registry initialized with {Count} builders: {Builders}",
            _pluginBuilders.Count, string.Join(", ", _pluginBuilders.Select(b => b.Key)));
    }

    public IReadOnlyList<IIntegrationPluginBuilder> GetAllBuilders()
    {
        return _pluginBuilders;
    }

    public IIntegrationPluginBuilder? GetBuilder(string key)
    {
        return _pluginBuilders.FirstOrDefault(b =>
            string.Equals(b.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<KernelPlugin>> GetEnabledPluginsAsync(string tenantId)
    {
        try
        {
            var enabledIntegrations = await _configStore.GetEnabledIntegrationsAsync(tenantId);
            var plugins = new List<KernelPlugin>();

            foreach (var integrationKey in enabledIntegrations)
            {
                var builder = GetBuilder(integrationKey);
                if (builder == null)
                {
                    _logger.LogWarning("No plugin builder found for integration {IntegrationKey} enabled for tenant {TenantId}",
                        integrationKey, tenantId);
                    continue;
                }

                try
                {
                    var plugin = await builder.BuildPluginAsync(tenantId);
                    if (plugin != null)
                    {
                        plugins.Add(plugin);
                        _logger.LogInformation("Successfully loaded plugin {PluginKey} for tenant {TenantId}",
                            integrationKey, tenantId);
                    }
                    else
                    {
                        _logger.LogWarning("Plugin builder {PluginKey} returned null for tenant {TenantId}",
                            integrationKey, tenantId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to build plugin {PluginKey} for tenant {TenantId}",
                        integrationKey, tenantId);
                }
            }

            return plugins.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enabled plugins for tenant {TenantId}", tenantId);
            return new List<KernelPlugin>().AsReadOnly();
        }
    }

    public async Task<IReadOnlyList<KernelPlugin>> GetPluginsByCapabilitiesAsync(string tenantId, IEnumerable<string> requiredCapabilities)
    {
        try
        {
            var allPlugins = await GetEnabledPluginsAsync(tenantId);
            var capabilityList = requiredCapabilities.ToList();

            if (!capabilityList.Any())
            {
                return allPlugins;
            }

            var filteredPlugins = new List<KernelPlugin>();

            foreach (var plugin in allPlugins)
            {
                // Find the corresponding builder to check capabilities
                var builder = _pluginBuilders.FirstOrDefault(b =>
                    string.Equals(b.Key, plugin.Name, StringComparison.OrdinalIgnoreCase));

                if (builder != null)
                {
                    var hasRequiredCapabilities = capabilityList.Any(capability =>
                        builder.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase));

                    if (hasRequiredCapabilities)
                    {
                        filteredPlugins.Add(plugin);
                    }
                }
            }

            _logger.LogInformation("Found {Count} plugins with required capabilities {Capabilities} for tenant {TenantId}",
                filteredPlugins.Count, string.Join(", ", capabilityList), tenantId);

            return filteredPlugins.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get plugins by capabilities for tenant {TenantId}", tenantId);
            return new List<KernelPlugin>().AsReadOnly();
        }
    }
}

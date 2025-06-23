namespace FleetAssistant.Infrastructure.Configuration;

/// <summary>
/// Interface for managing tenant integration configurations
/// </summary>
public interface IIntegrationConfigStore
{
    /// <summary>
    /// Gets the list of enabled integrations for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>List of enabled integration keys</returns>
    Task<IReadOnlyList<string>> GetEnabledIntegrationsAsync(string tenantId);

    /// <summary>
    /// Gets the configuration for a specific integration and tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="integrationKey">Integration key (e.g., "geotab")</param>
    /// <returns>Configuration dictionary or null if not configured</returns>
    Task<IReadOnlyDictionary<string, string>?> GetIntegrationConfigAsync(string tenantId, string integrationKey);

    /// <summary>
    /// Checks if a specific integration is enabled for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="integrationKey">Integration key</param>
    /// <returns>True if enabled, false otherwise</returns>
    Task<bool> IsIntegrationEnabledAsync(string tenantId, string integrationKey);
}

/// <summary>
/// In-memory implementation of integration configuration store for MVP
/// </summary>
public class InMemoryIntegrationConfigStore : IIntegrationConfigStore
{
    private readonly Dictionary<string, List<string>> _tenantIntegrations;
    private readonly Dictionary<(string, string), Dictionary<string, string>> _integrationConfigs;

    public InMemoryIntegrationConfigStore()
    {
        _tenantIntegrations = [];
        _integrationConfigs = [];

        // Initialize some test data
        InitializeTestData();
    }

    public Task<IReadOnlyList<string>> GetEnabledIntegrationsAsync(string tenantId)
    {
        if (_tenantIntegrations.TryGetValue(tenantId, out var integrations))
        {
            return Task.FromResult<IReadOnlyList<string>>(integrations.AsReadOnly());
        }

        return Task.FromResult<IReadOnlyList<string>>(new List<string>().AsReadOnly());
    }

    public Task<IReadOnlyDictionary<string, string>?> GetIntegrationConfigAsync(string tenantId, string integrationKey)
    {
        if (_integrationConfigs.TryGetValue((tenantId, integrationKey), out var config))
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>?>(config.AsReadOnly());
        }

        return Task.FromResult<IReadOnlyDictionary<string, string>?>(null);
    }

    public Task<bool> IsIntegrationEnabledAsync(string tenantId, string integrationKey)
    {
        var enabled = _tenantIntegrations.TryGetValue(tenantId, out var integrations) &&
                     integrations.Contains(integrationKey, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(enabled);
    }

    private void InitializeTestData()
    {
        // Test tenant configurations
        _tenantIntegrations["tenant1"] = ["geotab", "fleetio"];
        _tenantIntegrations["tenant2"] = ["samsara", "geotab"];
        _tenantIntegrations["test-tenant"] = ["geotab", "fleetio", "samsara"];

        // Integration configurations
        _integrationConfigs[("tenant1", "geotab")] = new Dictionary<string, string>
        {
            ["server"] = "my.geotab.com",
            ["database"] = "tenant1_db",
            ["apiVersion"] = "v1"
        };

        _integrationConfigs[("tenant1", "fleetio")] = new Dictionary<string, string>
        {
            ["baseUrl"] = "https://secure.fleetio.com/api/v1",
            ["accountId"] = "12345"
        };

        _integrationConfigs[("test-tenant", "geotab")] = new Dictionary<string, string>
        {
            ["server"] = "my.geotab.com",
            ["database"] = "test_db",
            ["apiVersion"] = "v1"
        };

        _integrationConfigs[("test-tenant", "fleetio")] = new Dictionary<string, string>
        {
            ["baseUrl"] = "https://secure.fleetio.com/api/v1",
            ["accountId"] = "54321"
        };

        _integrationConfigs[("test-tenant", "samsara")] = new Dictionary<string, string>
        {
            ["baseUrl"] = "https://api.samsara.com",
            ["apiVersion"] = "v1"
        };
    }
}

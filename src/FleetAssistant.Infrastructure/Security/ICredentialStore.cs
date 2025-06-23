namespace FleetAssistant.Infrastructure.Security;

/// <summary>
/// Interface for securely storing and retrieving tenant credentials
/// </summary>
public interface ICredentialStore
{
    /// <summary>
    /// Gets credentials for a specific tenant and integration
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="integrationKey">Integration key (e.g., "geotab")</param>
    /// <returns>Credentials dictionary or null if not found</returns>
    Task<IReadOnlyDictionary<string, string>?> GetCredentialsAsync(string tenantId, string integrationKey);

    /// <summary>
    /// Stores credentials for a specific tenant and integration
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="integrationKey">Integration key</param>
    /// <param name="credentials">Credentials to store</param>
    Task SetCredentialsAsync(string tenantId, string integrationKey, IReadOnlyDictionary<string, string> credentials);

    /// <summary>
    /// Removes credentials for a specific tenant and integration
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="integrationKey">Integration key</param>
    Task RemoveCredentialsAsync(string tenantId, string integrationKey);
}

/// <summary>
/// In-memory implementation of credential store for MVP
/// WARNING: This is not secure and should only be used for development/testing
/// </summary>
public class InMemoryCredentialStore : ICredentialStore
{
    private readonly Dictionary<(string, string), Dictionary<string, string>> _credentials;

    public InMemoryCredentialStore()
    {
        _credentials = new Dictionary<(string, string), Dictionary<string, string>>();
        
        // Initialize test credentials
        InitializeTestCredentials();
    }

    public Task<IReadOnlyDictionary<string, string>?> GetCredentialsAsync(string tenantId, string integrationKey)
    {
        if (_credentials.TryGetValue((tenantId, integrationKey), out var credentials))
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>?>(credentials.AsReadOnly());
        }

        return Task.FromResult<IReadOnlyDictionary<string, string>?>(null);
    }

    public Task SetCredentialsAsync(string tenantId, string integrationKey, IReadOnlyDictionary<string, string> credentials)
    {
        _credentials[(tenantId, integrationKey)] = new Dictionary<string, string>(credentials);
        return Task.CompletedTask;
    }

    public Task RemoveCredentialsAsync(string tenantId, string integrationKey)
    {
        _credentials.Remove((tenantId, integrationKey));
        return Task.CompletedTask;
    }

    private void InitializeTestCredentials()
    {
        // GeoTab test credentials
        _credentials[("tenant1", "geotab")] = new Dictionary<string, string>
        {
            ["username"] = "test_user@tenant1.com",
            ["password"] = "test_password_123",
            ["database"] = "tenant1_db"
        };

        _credentials[("test-tenant", "geotab")] = new Dictionary<string, string>
        {
            ["username"] = "test_user@test.com",
            ["password"] = "test_password_456", 
            ["database"] = "test_db"
        };

        // Fleetio test credentials
        _credentials[("tenant1", "fleetio")] = new Dictionary<string, string>
        {
            ["apiToken"] = "fleetio_test_token_tenant1",
            ["accountId"] = "12345"
        };

        // Samsara test credentials
        _credentials[("tenant2", "samsara")] = new Dictionary<string, string>
        {
            ["apiToken"] = "samsara_test_token_tenant2",
            ["orgId"] = "org_67890"
        };
    }
}

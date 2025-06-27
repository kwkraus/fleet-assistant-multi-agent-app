using FleetAssistant.Shared.Models;

namespace FleetAssistant.Infrastructure.Services;

/// <summary>
/// Service for managing tenant configurations, permissions, and access control
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets tenant configuration by tenant ID
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Tenant configuration or null if not found</returns>
    Task<TenantConfiguration?> GetTenantConfigurationAsync(string tenantId);

    /// <summary>
    /// Creates a new tenant with the specified configuration
    /// </summary>
    /// <param name="configuration">Tenant configuration</param>
    /// <returns>Created tenant configuration</returns>
    Task<TenantConfiguration> CreateTenantAsync(TenantConfiguration configuration);

    /// <summary>
    /// Updates an existing tenant configuration
    /// </summary>
    /// <param name="configuration">Updated tenant configuration</param>
    /// <returns>Updated tenant configuration</returns>
    Task<TenantConfiguration> UpdateTenantAsync(TenantConfiguration configuration);

    /// <summary>
    /// Validates if a tenant can access a specific resource or feature
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="permission">Required permission</param>
    /// <returns>True if access is allowed, false otherwise</returns>
    Task<bool> ValidateTenantAccessAsync(string tenantId, string permission);

    /// <summary>
    /// Validates if a tenant can access a specific integration
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="integrationKey">Integration key (e.g., "geotab")</param>
    /// <returns>True if access is allowed, false otherwise</returns>
    Task<bool> ValidateIntegrationAccessAsync(string tenantId, string integrationKey);

    /// <summary>
    /// Checks if a tenant has reached their rate limits
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Rate limit check result</returns>
    Task<RateLimitCheckResult> CheckRateLimitAsync(string tenantId);

    /// <summary>
    /// Records API usage for rate limiting and billing
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="apiCalls">Number of API calls to record</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <param name="success">Whether the request was successful</param>
    Task RecordUsageAsync(string tenantId, int apiCalls = 1, double responseTimeMs = 0, bool success = true);

    /// <summary>
    /// Gets tenant usage statistics
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Current usage information</returns>
    Task<TenantUsageInfo> GetTenantUsageAsync(string tenantId);

    /// <summary>
    /// Validates if a tenant's subscription is active and not expired
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Subscription validation result</returns>
    Task<SubscriptionValidationResult> ValidateSubscriptionAsync(string tenantId);

    /// <summary>
    /// Gets all permissions for a tenant based on their tier and configuration
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>List of available permissions</returns>
    Task<IReadOnlyList<string>> GetTenantPermissionsAsync(string tenantId);

    /// <summary>
    /// Suspends a tenant (disables API access)
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="reason">Reason for suspension</param>
    Task SuspendTenantAsync(string tenantId, string reason);

    /// <summary>
    /// Reactivates a suspended tenant
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    Task ReactivateTenantAsync(string tenantId);

    /// <summary>
    /// Lists all tenants (for admin purposes)
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="tier">Optional tier filter</param>
    /// <returns>List of tenant configurations</returns>
    Task<IReadOnlyList<TenantConfiguration>> ListTenantsAsync(TenantStatus? status = null, TenantTier? tier = null);
}

/// <summary>
/// Result of rate limit validation
/// </summary>
public class RateLimitCheckResult
{
    /// <summary>
    /// Whether the request is allowed (within rate limits)
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Reason for denial if not allowed
    /// </summary>
    public string? DenialReason { get; set; }

    /// <summary>
    /// Number of requests remaining in the current period
    /// </summary>
    public int RequestsRemaining { get; set; }

    /// <summary>
    /// When the rate limit resets
    /// </summary>
    public DateTime ResetsAt { get; set; }

    /// <summary>
    /// Current request count in the period
    /// </summary>
    public int CurrentRequestCount { get; set; }

    /// <summary>
    /// Maximum requests allowed in the period
    /// </summary>
    public int MaxRequestsAllowed { get; set; }
}

/// <summary>
/// Result of subscription validation
/// </summary>
public class SubscriptionValidationResult
{
    /// <summary>
    /// Whether the subscription is valid and active
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Reason for invalidity if not valid
    /// </summary>
    public string? InvalidReason { get; set; }

    /// <summary>
    /// Days until subscription expires (null if lifetime)
    /// </summary>
    public int? DaysUntilExpiry { get; set; }

    /// <summary>
    /// Whether the subscription is in a grace period
    /// </summary>
    public bool IsInGracePeriod { get; set; }

    /// <summary>
    /// Tenant status
    /// </summary>
    public TenantStatus Status { get; set; }
}

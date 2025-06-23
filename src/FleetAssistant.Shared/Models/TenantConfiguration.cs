namespace FleetAssistant.Shared.Models;

/// <summary>
/// Comprehensive tenant configuration and settings
/// </summary>
public class TenantConfiguration
{
    /// <summary>
    /// Unique tenant identifier
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the tenant organization
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tenant status (Active, Suspended, Pending, Disabled)
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Pending;

    /// <summary>
    /// Subscription tier (Free, Basic, Premium, Enterprise)
    /// </summary>
    public TenantTier Tier { get; set; } = TenantTier.Free;

    /// <summary>
    /// When the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the tenant configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Subscription expiration date (null for lifetime subscriptions)
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }

    /// <summary>
    /// Maximum number of API calls per minute
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 100;

    /// <summary>
    /// Maximum number of API calls per day
    /// </summary>
    public int RateLimitPerDay { get; set; } = 10000;

    /// <summary>
    /// Maximum number of concurrent agent requests
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 5;

    /// <summary>
    /// Maximum number of active API keys
    /// </summary>
    public int MaxApiKeys { get; set; } = 10;

    /// <summary>
    /// Enabled features for this tenant
    /// </summary>
    public TenantFeatures Features { get; set; } = new();

    /// <summary>
    /// Contact information for the tenant
    /// </summary>
    public TenantContact Contact { get; set; } = new();

    /// <summary>
    /// Integration permissions and limits
    /// </summary>
    public TenantIntegrationSettings Integrations { get; set; } = new();

    /// <summary>
    /// Custom metadata for the tenant
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Billing and usage tracking information
    /// </summary>
    public TenantUsageInfo Usage { get; set; } = new();
}

/// <summary>
/// Tenant status enumeration
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Tenant is pending activation
    /// </summary>
    Pending,

    /// <summary>
    /// Tenant is active and fully operational
    /// </summary>
    Active,

    /// <summary>
    /// Tenant is temporarily suspended (payment issues, policy violations)
    /// </summary>
    Suspended,

    /// <summary>
    /// Tenant is disabled (permanent deactivation)
    /// </summary>
    Disabled
}

/// <summary>
/// Tenant subscription tier
/// </summary>
public enum TenantTier
{
    /// <summary>
    /// Free tier with basic features
    /// </summary>
    Free,

    /// <summary>
    /// Basic paid tier
    /// </summary>
    Basic,

    /// <summary>
    /// Premium tier with advanced features
    /// </summary>
    Premium,

    /// <summary>
    /// Enterprise tier with full features and support
    /// </summary>
    Enterprise
}

/// <summary>
/// Feature flags and capabilities for a tenant
/// </summary>
public class TenantFeatures
{
    /// <summary>
    /// Can use multi-agent orchestration
    /// </summary>
    public bool MultiAgentOrchestration { get; set; } = true;

    /// <summary>
    /// Can use advanced AI models (GPT-4, etc.)
    /// </summary>
    public bool AdvancedAiModels { get; set; } = false;

    /// <summary>
    /// Can access real-time fleet data
    /// </summary>
    public bool RealTimeData { get; set; } = true;

    /// <summary>
    /// Can use predictive analytics
    /// </summary>
    public bool PredictiveAnalytics { get; set; } = false;

    /// <summary>
    /// Can access historical data beyond 30 days
    /// </summary>
    public bool ExtendedHistoricalData { get; set; } = false;

    /// <summary>
    /// Can use custom integrations
    /// </summary>
    public bool CustomIntegrations { get; set; } = false;

    /// <summary>
    /// Can access priority support
    /// </summary>
    public bool PrioritySupport { get; set; } = false;

    /// <summary>
    /// Can use white-label branding
    /// </summary>
    public bool WhiteLabel { get; set; } = false;

    /// <summary>
    /// Can export data in various formats
    /// </summary>
    public bool DataExport { get; set; } = true;

    /// <summary>
    /// Can use webhooks for notifications
    /// </summary>
    public bool Webhooks { get; set; } = false;
}

/// <summary>
/// Contact information for a tenant
/// </summary>
public class TenantContact
{
    /// <summary>
    /// Primary contact name
    /// </summary>
    public string PrimaryContactName { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact email
    /// </summary>
    public string PrimaryContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Organization name
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Address information
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Timezone for the tenant (for scheduling and reporting)
    /// </summary>
    public string TimeZone { get; set; } = "UTC";
}

/// <summary>
/// Integration settings and permissions for a tenant
/// </summary>
public class TenantIntegrationSettings
{
    /// <summary>
    /// Maximum number of integration connections
    /// </summary>
    public int MaxIntegrations { get; set; } = 3;

    /// <summary>
    /// Allowed integration types
    /// </summary>
    public List<string> AllowedIntegrations { get; set; } = new() { "geotab", "fleetio", "samsara" };

    /// <summary>
    /// Rate limits per integration (calls per hour)
    /// </summary>
    public Dictionary<string, int> IntegrationRateLimits { get; set; } = new();

    /// <summary>
    /// Whether tenant can configure custom webhooks
    /// </summary>
    public bool CanConfigureWebhooks { get; set; } = false;

    /// <summary>
    /// Maximum webhook endpoints
    /// </summary>
    public int MaxWebhookEndpoints { get; set; } = 5;
}

/// <summary>
/// Usage tracking and billing information
/// </summary>
public class TenantUsageInfo
{
    /// <summary>
    /// Total API calls this month
    /// </summary>
    public long ApiCallsThisMonth { get; set; } = 0;

    /// <summary>
    /// Total API calls today
    /// </summary>
    public long ApiCallsToday { get; set; } = 0;

    /// <summary>
    /// Last reset date for monthly counters
    /// </summary>
    public DateTime MonthlyResetDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Last reset date for daily counters
    /// </summary>
    public DateTime DailyResetDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>
    /// Storage used in bytes
    /// </summary>
    public long StorageUsedBytes { get; set; } = 0;

    /// <summary>
    /// Number of active integrations
    /// </summary>
    public int ActiveIntegrations { get; set; } = 0;

    /// <summary>
    /// Number of active API keys
    /// </summary>
    public int ActiveApiKeys { get; set; } = 0;

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; } = 0;

    /// <summary>
    /// Error rate percentage (0-100)
    /// </summary>
    public double ErrorRatePercentage { get; set; } = 0;
}

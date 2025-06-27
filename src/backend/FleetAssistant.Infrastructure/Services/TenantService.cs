using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FleetAssistant.Infrastructure.Services;

/// <summary>
/// In-memory implementation of tenant service for MVP
/// In production, this would use a database for persistence
/// </summary>
public class InMemoryTenantService : ITenantService
{
    private readonly ILogger<InMemoryTenantService> _logger;
    private readonly Dictionary<string, TenantConfiguration> _tenants;
    private readonly Dictionary<string, List<(DateTime timestamp, int calls, double responseTime, bool success)>> _usageHistory;

    public InMemoryTenantService(ILogger<InMemoryTenantService> logger)
    {
        _logger = logger;
        _tenants = new Dictionary<string, TenantConfiguration>();
        _usageHistory = new Dictionary<string, List<(DateTime, int, double, bool)>>();

        // Initialize with some test tenants
        InitializeTestTenants();
    }

    public async Task<TenantConfiguration?> GetTenantConfigurationAsync(string tenantId)
    {
        _tenants.TryGetValue(tenantId, out var tenant);
        return await Task.FromResult(tenant);
    }

    public async Task<TenantConfiguration> CreateTenantAsync(TenantConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration.TenantId))
        {
            configuration.TenantId = Guid.NewGuid().ToString();
        }

        configuration.CreatedAt = DateTime.UtcNow;
        configuration.UpdatedAt = DateTime.UtcNow;

        // Set default tier-based limits
        ApplyTierDefaults(configuration);

        _tenants[configuration.TenantId] = configuration;
        _usageHistory[configuration.TenantId] = new List<(DateTime, int, double, bool)>();

        _logger.LogInformation("Created new tenant {TenantId} with tier {Tier}",
            configuration.TenantId, configuration.Tier);

        return await Task.FromResult(configuration);
    }

    public async Task<TenantConfiguration> UpdateTenantAsync(TenantConfiguration configuration)
    {
        if (!_tenants.ContainsKey(configuration.TenantId))
        {
            throw new ArgumentException($"Tenant {configuration.TenantId} not found");
        }

        configuration.UpdatedAt = DateTime.UtcNow;
        _tenants[configuration.TenantId] = configuration;

        _logger.LogInformation("Updated tenant {TenantId}", configuration.TenantId);

        return await Task.FromResult(configuration);
    }

    public async Task<bool> ValidateTenantAccessAsync(string tenantId, string permission)
    {
        var tenant = await GetTenantConfigurationAsync(tenantId);
        if (tenant == null || tenant.Status != TenantStatus.Active)
        {
            return false;
        }

        // Check subscription
        var subscriptionResult = await ValidateSubscriptionAsync(tenantId);
        if (!subscriptionResult.IsValid)
        {
            return false;
        }

        // Get tenant permissions based on tier and features
        var tenantPermissions = await GetTenantPermissionsAsync(tenantId);
        return tenantPermissions.Contains(permission);
    }

    public async Task<bool> ValidateIntegrationAccessAsync(string tenantId, string integrationKey)
    {
        var tenant = await GetTenantConfigurationAsync(tenantId);
        if (tenant == null || tenant.Status != TenantStatus.Active)
        {
            return false;
        }

        // Check if integration is allowed
        if (!tenant.Integrations.AllowedIntegrations.Contains(integrationKey))
        {
            return false;
        }

        // Check if tenant has reached max integrations
        var currentIntegrations = tenant.Usage.ActiveIntegrations;
        if (currentIntegrations >= tenant.Integrations.MaxIntegrations)
        {
            return false;
        }

        return true;
    }

    public async Task<RateLimitCheckResult> CheckRateLimitAsync(string tenantId)
    {
        var tenant = await GetTenantConfigurationAsync(tenantId);
        if (tenant == null)
        {
            return new RateLimitCheckResult
            {
                IsAllowed = false,
                DenialReason = "Tenant not found"
            };
        }

        var now = DateTime.UtcNow;
        var usage = tenant.Usage;

        // Reset daily counter if needed
        if (usage.DailyResetDate.Date < now.Date)
        {
            usage.ApiCallsToday = 0;
            usage.DailyResetDate = now.Date;
        }

        // Reset monthly counter if needed
        if (usage.MonthlyResetDate.Month != now.Month || usage.MonthlyResetDate.Year != now.Year)
        {
            usage.ApiCallsThisMonth = 0;
            usage.MonthlyResetDate = new DateTime(now.Year, now.Month, 1);
        }

        // Check daily limit
        if (usage.ApiCallsToday >= tenant.RateLimitPerDay)
        {
            return new RateLimitCheckResult
            {
                IsAllowed = false,
                DenialReason = "Daily rate limit exceeded",
                RequestsRemaining = 0,
                ResetsAt = now.Date.AddDays(1),
                CurrentRequestCount = (int)usage.ApiCallsToday,
                MaxRequestsAllowed = tenant.RateLimitPerDay
            };
        }

        // Check minute-based rate limiting (simplified - in production use sliding window)
        var minuteAgo = now.AddMinutes(-1);
        if (_usageHistory.TryGetValue(tenantId, out var history))
        {
            var recentCalls = history.Where(h => h.timestamp > minuteAgo).Sum(h => h.calls);
            if (recentCalls >= tenant.RateLimitPerMinute)
            {
                return new RateLimitCheckResult
                {
                    IsAllowed = false,
                    DenialReason = "Per-minute rate limit exceeded",
                    RequestsRemaining = 0,
                    ResetsAt = minuteAgo.AddMinutes(1),
                    CurrentRequestCount = recentCalls,
                    MaxRequestsAllowed = tenant.RateLimitPerMinute
                };
            }
        }

        return new RateLimitCheckResult
        {
            IsAllowed = true,
            RequestsRemaining = tenant.RateLimitPerDay - (int)usage.ApiCallsToday,
            ResetsAt = now.Date.AddDays(1),
            CurrentRequestCount = (int)usage.ApiCallsToday,
            MaxRequestsAllowed = tenant.RateLimitPerDay
        };
    }

    public async Task RecordUsageAsync(string tenantId, int apiCalls = 1, double responseTimeMs = 0, bool success = true)
    {
        var tenant = await GetTenantConfigurationAsync(tenantId);
        if (tenant == null) return;

        var now = DateTime.UtcNow;

        // Update usage counters
        tenant.Usage.ApiCallsToday += apiCalls;
        tenant.Usage.ApiCallsThisMonth += apiCalls;

        // Update average response time (simple moving average)
        if (responseTimeMs > 0)
        {
            var currentAvg = tenant.Usage.AverageResponseTimeMs;
            var totalCalls = tenant.Usage.ApiCallsThisMonth;
            tenant.Usage.AverageResponseTimeMs = ((currentAvg * (totalCalls - apiCalls)) + responseTimeMs) / totalCalls;
        }

        // Update error rate
        if (!success)
        {
            var totalCalls = tenant.Usage.ApiCallsThisMonth;
            var currentErrors = (tenant.Usage.ErrorRatePercentage / 100.0) * (totalCalls - apiCalls);
            tenant.Usage.ErrorRatePercentage = ((currentErrors + 1) / totalCalls) * 100;
        }

        // Record in usage history for rate limiting
        if (!_usageHistory.ContainsKey(tenantId))
        {
            _usageHistory[tenantId] = new List<(DateTime, int, double, bool)>();
        }

        _usageHistory[tenantId].Add((now, apiCalls, responseTimeMs, success));

        // Clean up old history (keep last 24 hours)
        var cutoff = now.AddHours(-24);
        _usageHistory[tenantId].RemoveAll(h => h.timestamp < cutoff);

        _logger.LogDebug("Recorded usage for tenant {TenantId}: {ApiCalls} calls, {ResponseTime}ms",
            tenantId, apiCalls, responseTimeMs);
    }

    public async Task<TenantUsageInfo> GetTenantUsageAsync(string tenantId)
    {
        var tenant = await GetTenantConfigurationAsync(tenantId);
        return tenant?.Usage ?? new TenantUsageInfo();
    }

    public async Task<SubscriptionValidationResult> ValidateSubscriptionAsync(string tenantId)
    {
        var tenant = await GetTenantConfigurationAsync(tenantId);
        if (tenant == null)
        {
            return new SubscriptionValidationResult
            {
                IsValid = false,
                InvalidReason = "Tenant not found",
                Status = TenantStatus.Disabled
            };
        }

        if (tenant.Status != TenantStatus.Active)
        {
            return new SubscriptionValidationResult
            {
                IsValid = false,
                InvalidReason = $"Tenant status is {tenant.Status}",
                Status = tenant.Status
            };
        }

        // Check expiration
        if (tenant.SubscriptionExpiresAt.HasValue)
        {
            var now = DateTime.UtcNow;
            var daysUntilExpiry = (int)(tenant.SubscriptionExpiresAt.Value - now).TotalDays;

            if (daysUntilExpiry < 0)
            {
                return new SubscriptionValidationResult
                {
                    IsValid = false,
                    InvalidReason = "Subscription expired",
                    DaysUntilExpiry = daysUntilExpiry,
                    Status = tenant.Status
                };
            }

            return new SubscriptionValidationResult
            {
                IsValid = true,
                DaysUntilExpiry = daysUntilExpiry,
                IsInGracePeriod = daysUntilExpiry <= 7, // Grace period for last 7 days
                Status = tenant.Status
            };
        }

        // Lifetime subscription
        return new SubscriptionValidationResult
        {
            IsValid = true,
            Status = tenant.Status
        };
    }

    public async Task<IReadOnlyList<string>> GetTenantPermissionsAsync(string tenantId)
    {
        var tenant = await GetTenantConfigurationAsync(tenantId);
        if (tenant == null) return new List<string>();

        var permissions = new List<string>();

        // Base permissions for all active tenants
        if (tenant.Status == TenantStatus.Active)
        {
            permissions.Add(Permissions.FleetQuery.Id);
            permissions.Add(Permissions.DataRealTime.Id);
        }

        // Tier-based permissions
        switch (tenant.Tier)
        {
            case TenantTier.Free:
                permissions.AddRange(Roles.Viewer);
                break;
            case TenantTier.Basic:
                permissions.AddRange(Roles.FleetUser);
                break;
            case TenantTier.Premium:
                permissions.AddRange(Roles.FleetAnalyst);
                break;
            case TenantTier.Enterprise:
                permissions.AddRange(Roles.TenantAdmin);
                break;
        }

        // Feature-based permissions
        if (tenant.Features.AdvancedAiModels)
        {
            permissions.Add(Permissions.FleetQueryAdvanced.Id);
        }

        if (tenant.Features.ExtendedHistoricalData)
        {
            permissions.Add(Permissions.DataHistoricalExtended.Id);
        }

        if (tenant.Features.DataExport)
        {
            permissions.Add(Permissions.DataExport.Id);
        }

        // Integration permissions
        foreach (var integration in tenant.Integrations.AllowedIntegrations)
        {
            permissions.Add($"integration:{integration}");
        }

        return permissions.Distinct().ToList();
    }

    public async Task SuspendTenantAsync(string tenantId, string reason)
    {
        var tenant = await GetTenantConfigurationAsync(tenantId);
        if (tenant == null) return;

        tenant.Status = TenantStatus.Suspended;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.Metadata["suspensionReason"] = reason;
        tenant.Metadata["suspendedAt"] = DateTime.UtcNow.ToString("O");

        _logger.LogWarning("Suspended tenant {TenantId}: {Reason}", tenantId, reason);
    }

    public async Task ReactivateTenantAsync(string tenantId)
    {
        var tenant = await GetTenantConfigurationAsync(tenantId);
        if (tenant == null) return;

        tenant.Status = TenantStatus.Active;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.Metadata.Remove("suspensionReason");
        tenant.Metadata["reactivatedAt"] = DateTime.UtcNow.ToString("O");

        _logger.LogInformation("Reactivated tenant {TenantId}", tenantId);
    }

    public async Task<IReadOnlyList<TenantConfiguration>> ListTenantsAsync(TenantStatus? status = null, TenantTier? tier = null)
    {
        var tenants = _tenants.Values.AsEnumerable();

        if (status.HasValue)
        {
            tenants = tenants.Where(t => t.Status == status.Value);
        }

        if (tier.HasValue)
        {
            tenants = tenants.Where(t => t.Tier == tier.Value);
        }

        return await Task.FromResult(tenants.ToList());
    }

    private void InitializeTestTenants()
    {
        // Test tenant 1: Basic tier
        var tenant1 = new TenantConfiguration
        {
            TenantId = "test-tenant-1",
            Name = "Test Fleet Company",
            Status = TenantStatus.Active,
            Tier = TenantTier.Basic,
            Contact = new TenantContact
            {
                PrimaryContactName = "John Doe",
                PrimaryContactEmail = "john@testfleet.com",
                OrganizationName = "Test Fleet Company"
            }
        };
        ApplyTierDefaults(tenant1);
        _tenants[tenant1.TenantId] = tenant1;
        _usageHistory[tenant1.TenantId] = new List<(DateTime, int, double, bool)>();

        // Test tenant 2: Premium tier
        var tenant2 = new TenantConfiguration
        {
            TenantId = "premium-tenant",
            Name = "Premium Fleet Corp",
            Status = TenantStatus.Active,
            Tier = TenantTier.Premium,
            Contact = new TenantContact
            {
                PrimaryContactName = "Jane Smith",
                PrimaryContactEmail = "jane@premiumfleet.com",
                OrganizationName = "Premium Fleet Corp"
            }
        };
        ApplyTierDefaults(tenant2);
        _tenants[tenant2.TenantId] = tenant2;
        _usageHistory[tenant2.TenantId] = new List<(DateTime, int, double, bool)>();

        _logger.LogInformation("Initialized {Count} test tenants", _tenants.Count);
    }

    private static void ApplyTierDefaults(TenantConfiguration tenant)
    {
        switch (tenant.Tier)
        {
            case TenantTier.Free:
                tenant.RateLimitPerMinute = 10;
                tenant.RateLimitPerDay = 100;
                tenant.MaxConcurrentRequests = 1;
                tenant.MaxApiKeys = 2;
                tenant.Integrations.MaxIntegrations = 1;
                tenant.Features.AdvancedAiModels = false;
                tenant.Features.ExtendedHistoricalData = false;
                tenant.Features.PredictiveAnalytics = false;
                break;

            case TenantTier.Basic:
                tenant.RateLimitPerMinute = 50;
                tenant.RateLimitPerDay = 1000;
                tenant.MaxConcurrentRequests = 3;
                tenant.MaxApiKeys = 5;
                tenant.Integrations.MaxIntegrations = 2;
                tenant.Features.AdvancedAiModels = false;
                tenant.Features.ExtendedHistoricalData = false;
                tenant.Features.PredictiveAnalytics = false;
                break;

            case TenantTier.Premium:
                tenant.RateLimitPerMinute = 200;
                tenant.RateLimitPerDay = 10000;
                tenant.MaxConcurrentRequests = 10;
                tenant.MaxApiKeys = 15;
                tenant.Integrations.MaxIntegrations = 5;
                tenant.Features.AdvancedAiModels = true;
                tenant.Features.ExtendedHistoricalData = true;
                tenant.Features.PredictiveAnalytics = true;
                tenant.Features.DataExport = true;
                break;

            case TenantTier.Enterprise:
                tenant.RateLimitPerMinute = 1000;
                tenant.RateLimitPerDay = 100000;
                tenant.MaxConcurrentRequests = 25;
                tenant.MaxApiKeys = 50;
                tenant.Integrations.MaxIntegrations = 10;
                tenant.Features.AdvancedAiModels = true;
                tenant.Features.ExtendedHistoricalData = true;
                tenant.Features.PredictiveAnalytics = true;
                tenant.Features.CustomIntegrations = true;
                tenant.Features.PrioritySupport = true;
                tenant.Features.WhiteLabel = true;
                tenant.Features.DataExport = true;
                tenant.Features.Webhooks = true;
                break;
        }
    }
}

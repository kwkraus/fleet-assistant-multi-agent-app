using FleetAssistant.Infrastructure.Services;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FleetAssistant.Api.Middleware;

/// <summary>
/// Middleware for validating tenant authorization and permissions
/// </summary>
public class TenantAuthorizationMiddleware
{
    private readonly ILogger<TenantAuthorizationMiddleware> _logger;
    private readonly ITenantService _tenantService;

    public TenantAuthorizationMiddleware(
        ILogger<TenantAuthorizationMiddleware> logger,
        ITenantService tenantService)
    {
        _logger = logger;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Validates tenant permissions for the current request
    /// </summary>
    /// <param name="userContext">User context from authentication</param>
    /// <param name="requiredPermission">Required permission for the operation</param>
    /// <returns>Authorization result</returns>
    public async Task<TenantAuthorizationResult> ValidatePermissionAsync(
        UserContext userContext,
        string requiredPermission)
    {
        try
        {
            // Check if tenant exists and is active
            var tenant = await _tenantService.GetTenantConfigurationAsync(userContext.TenantId);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} not found", userContext.TenantId);
                return TenantAuthorizationResult.Failure("Tenant not found");
            }

            if (tenant.Status != TenantStatus.Active)
            {
                _logger.LogWarning("Tenant {TenantId} is not active (status: {Status})",
                    userContext.TenantId, tenant.Status);
                return TenantAuthorizationResult.Failure($"Tenant is {tenant.Status.ToString().ToLower()}");
            }

            // Validate subscription
            var subscriptionResult = await _tenantService.ValidateSubscriptionAsync(userContext.TenantId);
            if (!subscriptionResult.IsValid)
            {
                _logger.LogWarning("Tenant {TenantId} has invalid subscription: {Reason}",
                    userContext.TenantId, subscriptionResult.InvalidReason);
                return TenantAuthorizationResult.Failure($"Subscription issue: {subscriptionResult.InvalidReason}");
            }

            // Check rate limits
            var rateLimitResult = await _tenantService.CheckRateLimitAsync(userContext.TenantId);
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning("Tenant {TenantId} exceeded rate limit: {Reason}",
                    userContext.TenantId, rateLimitResult.DenialReason);
                return TenantAuthorizationResult.RateLimited(rateLimitResult);
            }

            // Check specific permission
            var hasPermission = await _tenantService.ValidateTenantAccessAsync(userContext.TenantId, requiredPermission);
            if (!hasPermission)
            {
                _logger.LogWarning("Tenant {TenantId} lacks permission {Permission}",
                    userContext.TenantId, requiredPermission);
                return TenantAuthorizationResult.Failure($"Missing permission: {requiredPermission}");
            }

            // Validate API key scopes if specified
            if (userContext.Scopes.Any() && !userContext.Scopes.Contains(requiredPermission))
            {
                _logger.LogWarning("API key {ApiKeyId} lacks required scope {Permission}",
                    userContext.ApiKeyId, requiredPermission);
                return TenantAuthorizationResult.Failure($"API key missing scope: {requiredPermission}");
            }

            _logger.LogDebug("Tenant {TenantId} authorized for permission {Permission}",
                userContext.TenantId, requiredPermission);

            return TenantAuthorizationResult.Success(tenant, rateLimitResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating tenant authorization for {TenantId}", userContext.TenantId);
            return TenantAuthorizationResult.Failure("Authorization validation failed");
        }
    }

    /// <summary>
    /// Validates integration access for a tenant
    /// </summary>
    /// <param name="userContext">User context from authentication</param>
    /// <param name="integrationKey">Integration key (e.g., "geotab")</param>
    /// <returns>Authorization result</returns>
    public async Task<TenantAuthorizationResult> ValidateIntegrationAccessAsync(
        UserContext userContext,
        string integrationKey)
    {
        try
        {
            // First validate basic permissions
            var basicResult = await ValidatePermissionAsync(userContext, $"integration:{integrationKey}");
            if (!basicResult.IsSuccess)
            {
                return basicResult;
            }

            // Check integration-specific access
            var hasIntegrationAccess = await _tenantService.ValidateIntegrationAccessAsync(
                userContext.TenantId, integrationKey);

            if (!hasIntegrationAccess)
            {
                _logger.LogWarning("Tenant {TenantId} lacks access to integration {Integration}",
                    userContext.TenantId, integrationKey);
                return TenantAuthorizationResult.Failure($"Integration not available: {integrationKey}");
            }

            return basicResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating integration access for {TenantId} and {Integration}",
                userContext.TenantId, integrationKey);
            return TenantAuthorizationResult.Failure("Integration authorization validation failed");
        }
    }

    /// <summary>
    /// Records usage after a successful request
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <param name="success">Whether the request was successful</param>
    public async Task RecordUsageAsync(string tenantId, double responseTimeMs, bool success = true)
    {
        try
        {
            await _tenantService.RecordUsageAsync(tenantId, 1, responseTimeMs, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording usage for tenant {TenantId}", tenantId);
            // Don't fail the request due to usage recording errors
        }
    }
}

/// <summary>
/// Result of tenant authorization validation
/// </summary>
public class TenantAuthorizationResult
{
    /// <summary>
    /// Whether authorization was successful
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Error message if authorization failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Whether the failure was due to rate limiting
    /// </summary>
    public bool IsRateLimited { get; private set; }

    /// <summary>
    /// Tenant configuration if authorization was successful
    /// </summary>
    public TenantConfiguration? Tenant { get; private set; }

    /// <summary>
    /// Rate limit information
    /// </summary>
    public RateLimitCheckResult? RateLimitInfo { get; private set; }

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object> Context { get; private set; } = new();

    private TenantAuthorizationResult() { }

    /// <summary>
    /// Creates a successful authorization result
    /// </summary>
    public static TenantAuthorizationResult Success(TenantConfiguration tenant, RateLimitCheckResult rateLimitInfo)
    {
        return new TenantAuthorizationResult
        {
            IsSuccess = true,
            Tenant = tenant,
            RateLimitInfo = rateLimitInfo
        };
    }

    /// <summary>
    /// Creates a failed authorization result
    /// </summary>
    public static TenantAuthorizationResult Failure(string errorMessage)
    {
        return new TenantAuthorizationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a rate-limited authorization result
    /// </summary>
    public static TenantAuthorizationResult RateLimited(RateLimitCheckResult rateLimitInfo)
    {
        return new TenantAuthorizationResult
        {
            IsSuccess = false,
            IsRateLimited = true,
            ErrorMessage = rateLimitInfo.DenialReason,
            RateLimitInfo = rateLimitInfo
        };
    }

    /// <summary>
    /// Gets rate limit headers for HTTP responses
    /// </summary>
    public Dictionary<string, string> GetRateLimitHeaders()
    {
        var headers = new Dictionary<string, string>();

        if (RateLimitInfo != null)
        {
            headers["X-RateLimit-Limit"] = RateLimitInfo.MaxRequestsAllowed.ToString();
            headers["X-RateLimit-Remaining"] = Math.Max(0, RateLimitInfo.RequestsRemaining).ToString();
            headers["X-RateLimit-Reset"] = ((DateTimeOffset)RateLimitInfo.ResetsAt).ToUnixTimeSeconds().ToString();

            if (IsRateLimited)
            {
                headers["Retry-After"] = Math.Max(1, (int)(RateLimitInfo.ResetsAt - DateTime.UtcNow).TotalSeconds).ToString();
            }
        }

        return headers;
    }

    /// <summary>
    /// Converts to HTTP error response
    /// </summary>
    public object ToErrorResponse()
    {
        var response = new
        {
            error = ErrorMessage ?? "Authorization failed",
            success = false,
            timestamp = DateTime.UtcNow.ToString("O")
        };

        if (IsRateLimited && RateLimitInfo != null)
        {
            return new
            {
                error = ErrorMessage ?? "Rate limit exceeded",
                success = false,
                timestamp = DateTime.UtcNow.ToString("O"),
                rateLimitInfo = new
                {
                    limit = RateLimitInfo.MaxRequestsAllowed,
                    remaining = Math.Max(0, RateLimitInfo.RequestsRemaining),
                    resetTime = RateLimitInfo.ResetsAt.ToString("O"),
                    retryAfterSeconds = Math.Max(1, (int)(RateLimitInfo.ResetsAt - DateTime.UtcNow).TotalSeconds)
                }
            };
        }

        return response;
    }
}

using FleetAssistant.Api.Middleware;
using FleetAssistant.Api.Services;
using FleetAssistant.Infrastructure.Services;
using FleetAssistant.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FleetAssistant.Api.Functions;

/// <summary>
/// Admin endpoints for tenant management
/// </summary>
public class TenantManagementFunction
{
    private readonly ILogger<TenantManagementFunction> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly TenantAuthorizationMiddleware _tenantAuth;
    private readonly ITenantService _tenantService;

    public TenantManagementFunction(
        ILogger<TenantManagementFunction> logger,
        IAuthenticationService authenticationService,
        TenantAuthorizationMiddleware tenantAuth,
        ITenantService tenantService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _tenantAuth = tenantAuth;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Gets tenant configuration and status
    /// </summary>
    [Function("GetTenant")]
    public async Task<IActionResult> GetTenantAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/tenant/{tenantId}")] HttpRequest req,
        string tenantId)
    {
        try
        {
            var userContext = await AuthenticateAsync(req);
            if (userContext == null)
            {
                return new UnauthorizedObjectResult(new { error = "Invalid or missing API key" });
            }

            // Validate admin permissions
            var authResult = await _tenantAuth.ValidatePermissionAsync(userContext, Permissions.AdminTenantRead.Id);
            if (!authResult.IsSuccess)
            {
                return CreateAuthFailureResponse(authResult, req);
            }

            // Only allow tenant admins to view their own tenant, or system admins to view any tenant
            if (userContext.TenantId != tenantId && !userContext.Scopes.Contains(Permissions.AdminTenantWrite.Id))
            {
                return new ObjectResult(new { error = "Access denied to tenant information" })
                {
                    StatusCode = 403
                };
            }

            var tenant = await _tenantService.GetTenantConfigurationAsync(tenantId);
            if (tenant == null)
            {
                return new NotFoundObjectResult(new { error = "Tenant not found" });
            }

            // Get usage information
            var usage = await _tenantService.GetTenantUsageAsync(tenantId);
            var subscriptionStatus = await _tenantService.ValidateSubscriptionAsync(tenantId);

            var response = new
            {
                tenant = tenant,
                usage = usage,
                subscription = subscriptionStatus,
                permissions = await _tenantService.GetTenantPermissionsAsync(tenantId)
            };

            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant {TenantId}", tenantId);
            return new ObjectResult(new { error = "Internal server error" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Lists all tenants (system admin only)
    /// </summary>
    [Function("ListTenants")]
    public async Task<IActionResult> ListTenantsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/tenants")] HttpRequest req)
    {
        try
        {
            var userContext = await AuthenticateAsync(req);
            if (userContext == null)
            {
                return new UnauthorizedObjectResult(new { error = "Invalid or missing API key" });
            }

            // Validate system admin permissions
            var authResult = await _tenantAuth.ValidatePermissionAsync(userContext, Permissions.AdminTenantWrite.Id);
            if (!authResult.IsSuccess)
            {
                return CreateAuthFailureResponse(authResult, req);
            }

            // Parse query parameters
            req.Query.TryGetValue("status", out var statusQuery);
            req.Query.TryGetValue("tier", out var tierQuery);

            TenantStatus? status = null;
            if (Enum.TryParse<TenantStatus>(statusQuery, true, out var parsedStatus))
            {
                status = parsedStatus;
            }

            TenantTier? tier = null;
            if (Enum.TryParse<TenantTier>(tierQuery, true, out var parsedTier))
            {
                tier = parsedTier;
            }

            var tenants = await _tenantService.ListTenantsAsync(status, tier);

            return new OkObjectResult(new
            {
                tenants = tenants,
                total = tenants.Count,
                filters = new
                {
                    status = status?.ToString(),
                    tier = tier?.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tenants");
            return new ObjectResult(new { error = "Internal server error" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    [Function("CreateTenant")]
    public async Task<IActionResult> CreateTenantAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/tenant")] HttpRequest req)
    {
        try
        {
            var userContext = await AuthenticateAsync(req);
            if (userContext == null)
            {
                return new UnauthorizedObjectResult(new { error = "Invalid or missing API key" });
            }

            // Validate system admin permissions
            var authResult = await _tenantAuth.ValidatePermissionAsync(userContext, Permissions.AdminTenantWrite.Id);
            if (!authResult.IsSuccess)
            {
                return CreateAuthFailureResponse(authResult, req);
            }

            // Parse request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            TenantConfiguration? tenantConfig;
            try
            {
                tenantConfig = JsonSerializer.Deserialize<TenantConfiguration>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult(new { error = "Invalid JSON in request body" });
            }

            if (tenantConfig == null || string.IsNullOrEmpty(tenantConfig.Name))
            {
                return new BadRequestObjectResult(new { error = "Tenant name is required" });
            }

            var createdTenant = await _tenantService.CreateTenantAsync(tenantConfig);

            _logger.LogInformation("Created new tenant {TenantId} by admin {AdminTenantId}",
                createdTenant.TenantId, userContext.TenantId);

            return new OkObjectResult(new
            {
                tenant = createdTenant,
                message = "Tenant created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return new ObjectResult(new { error = "Internal server error" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Updates tenant configuration
    /// </summary>
    [Function("UpdateTenant")]
    public async Task<IActionResult> UpdateTenantAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "admin/tenant/{tenantId}")] HttpRequest req,
        string tenantId)
    {
        try
        {
            var userContext = await AuthenticateAsync(req);
            if (userContext == null)
            {
                return new UnauthorizedObjectResult(new { error = "Invalid or missing API key" });
            }

            // Validate admin permissions
            var authResult = await _tenantAuth.ValidatePermissionAsync(userContext, Permissions.AdminTenantWrite.Id);
            if (!authResult.IsSuccess)
            {
                return CreateAuthFailureResponse(authResult, req);
            }

            // Parse request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            TenantConfiguration? tenantConfig;
            try
            {
                tenantConfig = JsonSerializer.Deserialize<TenantConfiguration>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult(new { error = "Invalid JSON in request body" });
            }

            if (tenantConfig == null)
            {
                return new BadRequestObjectResult(new { error = "Invalid tenant configuration" });
            }

            // Ensure tenant ID matches route parameter
            tenantConfig.TenantId = tenantId;

            var updatedTenant = await _tenantService.UpdateTenantAsync(tenantConfig);

            _logger.LogInformation("Updated tenant {TenantId} by admin {AdminTenantId}",
                tenantId, userContext.TenantId);

            return new OkObjectResult(new
            {
                tenant = updatedTenant,
                message = "Tenant updated successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return new NotFoundObjectResult(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
            return new ObjectResult(new { error = "Internal server error" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Suspends a tenant
    /// </summary>
    [Function("SuspendTenant")]
    public async Task<IActionResult> SuspendTenantAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/tenant/{tenantId}/suspend")] HttpRequest req,
        string tenantId)
    {
        try
        {
            var userContext = await AuthenticateAsync(req);
            if (userContext == null)
            {
                return new UnauthorizedObjectResult(new { error = "Invalid or missing API key" });
            }

            // Validate system admin permissions
            var authResult = await _tenantAuth.ValidatePermissionAsync(userContext, Permissions.AdminTenantWrite.Id);
            if (!authResult.IsSuccess)
            {
                return CreateAuthFailureResponse(authResult, req);
            }

            // Parse request body for suspension reason
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var suspensionData = new { reason = "Administrative action" };

            if (!string.IsNullOrEmpty(requestBody))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed?.TryGetValue("reason", out var reason) == true)
                    {
                        suspensionData = new { reason };
                    }
                }
                catch (JsonException)
                {
                    // Use default reason if parsing fails
                }
            }

            await _tenantService.SuspendTenantAsync(tenantId, suspensionData.reason);

            _logger.LogWarning("Suspended tenant {TenantId} by admin {AdminTenantId}: {Reason}",
                tenantId, userContext.TenantId, suspensionData.reason);

            return new OkObjectResult(new
            {
                message = "Tenant suspended successfully",
                reason = suspensionData.reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant {TenantId}", tenantId);
            return new ObjectResult(new { error = "Internal server error" }) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Reactivates a suspended tenant
    /// </summary>
    [Function("ReactivateTenant")]
    public async Task<IActionResult> ReactivateTenantAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/tenant/{tenantId}/reactivate")] HttpRequest req,
        string tenantId)
    {
        try
        {
            var userContext = await AuthenticateAsync(req);
            if (userContext == null)
            {
                return new UnauthorizedObjectResult(new { error = "Invalid or missing API key" });
            }

            // Validate system admin permissions
            var authResult = await _tenantAuth.ValidatePermissionAsync(userContext, Permissions.AdminTenantWrite.Id);
            if (!authResult.IsSuccess)
            {
                return CreateAuthFailureResponse(authResult, req);
            }

            await _tenantService.ReactivateTenantAsync(tenantId);

            _logger.LogInformation("Reactivated tenant {TenantId} by admin {AdminTenantId}",
                tenantId, userContext.TenantId);

            return new OkObjectResult(new
            {
                message = "Tenant reactivated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating tenant {TenantId}", tenantId);
            return new ObjectResult(new { error = "Internal server error" }) { StatusCode = 500 };
        }
    }

    private async Task<UserContext?> AuthenticateAsync(HttpRequest req)
    {
        var authHeader = req.Headers.Authorization.FirstOrDefault() ??
                        req.Headers["X-API-Key"].FirstOrDefault();

        return await _authenticationService.ValidateApiKeyAndGetUserContextAsync(authHeader);
    }

    private IActionResult CreateAuthFailureResponse(TenantAuthorizationResult authResult, HttpRequest req)
    {
        var statusCode = authResult.IsRateLimited ? 429 : 403;
        var headers = authResult.GetRateLimitHeaders();

        var result = new ObjectResult(authResult.ToErrorResponse())
        {
            StatusCode = statusCode
        };

        foreach (var header in headers)
        {
            req.HttpContext.Response.Headers[header.Key] = header.Value;
        }

        return result;
    }
}

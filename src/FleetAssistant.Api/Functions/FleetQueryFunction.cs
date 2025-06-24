using FleetAssistant.Agents;
using FleetAssistant.Api.Middleware;
using FleetAssistant.Api.Services;
using FleetAssistant.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace FleetAssistant.Api.Functions;

/// <summary>
/// Main HTTP endpoint for fleet queries
/// </summary>
public class FleetQueryFunction(
    ILogger<FleetQueryFunction> logger,
    IAuthenticationService authenticationService,
    TenantAuthorizationMiddleware tenantAuth,
    PlanningAgent planningAgent)
{
    private readonly ILogger<FleetQueryFunction> _logger = logger;
    private readonly IAuthenticationService _authenticationService = authenticationService;
    private readonly TenantAuthorizationMiddleware _tenantAuth = tenantAuth;
    private readonly PlanningAgent _planningAgent = planningAgent;

    [Function("FleetQuery")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fleet/query")] HttpRequest req)
    {
        var stopwatch = Stopwatch.StartNew();
        UserContext? userContext = null;
         
        try
        {
            _logger.LogInformation("Fleet query request received");

            // Authenticate using API Key (support both Authorization header and X-API-Key header)
            var authHeader = req.Headers.Authorization.FirstOrDefault() ??
                           req.Headers["X-API-Key"].FirstOrDefault();

            userContext = await _authenticationService.ValidateApiKeyAndGetUserContextAsync(authHeader);
            
            if (userContext == null)
            {
                _logger.LogWarning("Authentication failed for fleet query request");
                return new UnauthorizedObjectResult(new
                {
                    error = "Invalid or missing API key",
                    message = "Provide a valid API key in the Authorization header (Bearer <key>) or X-API-Key header"
                });
            }

            // Validate tenant authorization and permissions
            TenantAuthorizationResult? authResult = await _tenantAuth.ValidatePermissionAsync(userContext, Permissions.FleetQuery.Id);

            if (!authResult.IsSuccess)
            {
                _logger.LogWarning("Authorization failed for tenant {TenantId}: {Error}",
                    userContext.TenantId, authResult.ErrorMessage);

                // Return appropriate status code based on failure type
                var statusCode = authResult.IsRateLimited ? 429 : 403;
                var headers = authResult.GetRateLimitHeaders();

                var result = new ObjectResult(authResult.ToErrorResponse())
                {
                    StatusCode = statusCode
                };

                // Add rate limit headers if available
                foreach (var header in headers)
                {
                    req.HttpContext.Response.Headers[header.Key] = header.Value;
                }

                return result;
            }

            // Parse request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is required" });
            }

            FleetQueryRequest? queryRequest;

            try
            {
                queryRequest = JsonSerializer.Deserialize<FleetQueryRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse request body");
                return new BadRequestObjectResult(new { error = "Invalid JSON in request body" });
            }

            if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Message))
            {
                return new BadRequestObjectResult(new { error = "Message is required" });
            }

            _logger.LogInformation("Processing query from API key {ApiKeyId} for tenant {TenantId}: {Message}",
                userContext.ApiKeyId, userContext.TenantId, queryRequest.Message);

            // Delegate to the Planning Agent for processing
            var response = await _planningAgent.ProcessQueryAsync(queryRequest, userContext); stopwatch.Stop();
            response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            // Record usage for billing and rate limiting
            await _tenantAuth.RecordUsageAsync(userContext.TenantId, stopwatch.ElapsedMilliseconds, true);

            // Add rate limit headers to response
            var rateLimitHeaders = authResult.GetRateLimitHeaders();

            foreach (var header in rateLimitHeaders)
            {
                req.HttpContext.Response.Headers[header.Key] = header.Value;
            }

            _logger.LogInformation("Fleet query completed in {ElapsedMs}ms for API key {ApiKeyId}",
                stopwatch.ElapsedMilliseconds, userContext.ApiKeyId);

            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing fleet query request");

            // Record failed usage if we have user context
            if (userContext != null)
            {
                await _tenantAuth.RecordUsageAsync(userContext.TenantId, stopwatch.ElapsedMilliseconds, false);
            }

            return new ObjectResult(new
            {
                error = "An error occurred while processing your request",
                traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString()
            })
            {
                StatusCode = 500
            };
        }
    }
}

using FleetAssistant.Api.Services;
using FleetAssistant.Agents;
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
public class FleetQueryFunction
{
    private readonly ILogger<FleetQueryFunction> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly PlanningAgent _planningAgent;

    public FleetQueryFunction(
        ILogger<FleetQueryFunction> logger,
        IAuthenticationService authenticationService,
        PlanningAgent planningAgent)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _planningAgent = planningAgent;
    }

    [Function("FleetQuery")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fleet/query")] HttpRequest req)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Fleet query request received");

            // Authenticate using API Key (support both Authorization header and X-API-Key header)
            var authHeader = req.Headers.Authorization.FirstOrDefault() ??
                           req.Headers["X-API-Key"].FirstOrDefault();

            var userContext = await _authenticationService.ValidateApiKeyAndGetUserContextAsync(authHeader);

            if (userContext == null)
            {
                _logger.LogWarning("Authentication failed for fleet query request");
                return new UnauthorizedObjectResult(new
                {
                    error = "Invalid or missing API key",
                    message = "Provide a valid API key in the Authorization header (Bearer <key>) or X-API-Key header"
                });
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
            }            _logger.LogInformation("Processing query from API key {ApiKeyId} for tenant {TenantId}: {Message}",
                userContext.ApiKeyId, userContext.TenantId, queryRequest.Message);

            // Delegate to the Planning Agent for processing
            var response = await _planningAgent.ProcessQueryAsync(queryRequest, userContext);

            stopwatch.Stop();
            response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Fleet query completed in {ElapsedMs}ms for API key {ApiKeyId}",
                stopwatch.ElapsedMilliseconds, userContext.ApiKeyId);

            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing fleet query request");

            return new ObjectResult(new
            {
                error = "An error occurred while processing your request",
                traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString()
            })
            {
                StatusCode = 500            };
        }
    }
}

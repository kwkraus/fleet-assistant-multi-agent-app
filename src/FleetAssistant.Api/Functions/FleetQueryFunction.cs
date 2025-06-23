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
public class FleetQueryFunction
{
    private readonly ILogger<FleetQueryFunction> _logger;
    private readonly IAuthenticationService _authenticationService;

    public FleetQueryFunction(
        ILogger<FleetQueryFunction> logger,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
    }

    [Function("FleetQuery")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fleet/query")] HttpRequest req)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Fleet query request received");

            // Authenticate user
            var authHeader = req.Headers.Authorization.FirstOrDefault();
            var userContext = await _authenticationService.ValidateTokenAndGetUserContextAsync(authHeader);
            
            if (userContext == null)
            {
                _logger.LogWarning("Authentication failed for fleet query request");
                return new UnauthorizedObjectResult(new { error = "Invalid or missing authentication token" });
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

            _logger.LogInformation("Processing query from user {UserId} for tenant {TenantId}: {Message}", 
                userContext.UserId, userContext.TenantId, queryRequest.Message);

            // For now, return a simple response while we build out the agent system
            var response = await ProcessQueryAsync(queryRequest, userContext);
            
            stopwatch.Stop();
            response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Fleet query completed in {ElapsedMs}ms for user {UserId}", 
                stopwatch.ElapsedMilliseconds, userContext.UserId);

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
                StatusCode = 500
            };
        }
    }

    private async Task<FleetQueryResponse> ProcessQueryAsync(FleetQueryRequest request, UserContext userContext)
    {
        // This is a placeholder implementation while we build out the planning agent
        // In the next steps, this will delegate to the PlanningAgent
        
        var response = new FleetQueryResponse
        {
            Response = $"Hello {userContext.Email}! I received your query about: '{request.Message}'. " +
                      "I'm a fleet management AI assistant, and I'll help you with vehicle data, maintenance, fuel efficiency, and more. " +
                      "The planning agent and specialized agents are being implemented next.",
            AgentData = new Dictionary<string, object>
            {
                ["userContext"] = new 
                {
                    userId = userContext.UserId,
                    tenantId = userContext.TenantId,
                    roles = userContext.Roles
                },
                ["queryContext"] = new
                {
                    message = request.Message,
                    hasConversationHistory = request.ConversationHistory?.Count > 0,
                    hasAdditionalContext = request.Context?.Count > 0
                }
            },
            AgentsUsed = new List<string> { "PlaceholderAgent" },
            Timestamp = DateTime.UtcNow
        };

        // Simulate some processing time
        await Task.Delay(100);

        return response;
    }
}

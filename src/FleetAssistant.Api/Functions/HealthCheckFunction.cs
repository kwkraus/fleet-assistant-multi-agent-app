using FleetAssistant.Agents.Orchestration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FleetAssistant.Api.Functions;

/// <summary>
/// Health check endpoint for the Fleet Agent Orchestration Service
/// </summary>
public class HealthCheckFunction(
    ILogger<HealthCheckFunction> logger,
    IFleetAgentOrchestrationService orchestrationService)
{
    private readonly ILogger<HealthCheckFunction> _logger = logger;
    private readonly IFleetAgentOrchestrationService _orchestrationService = orchestrationService;

    [Function("HealthCheck")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("Health check requested");

            var healthStatus = await _orchestrationService.GetHealthStatusAsync();

            var response = new
            {
                status = healthStatus.Status,
                healthy = healthStatus.IsHealthy,
                timestamp = healthStatus.LastChecked,
                service = "Fleet Agent Orchestration",
                version = "1.0.0",
                details = healthStatus.Details
            };

            if (healthStatus.IsHealthy)
            {
                return new OkObjectResult(response);
            }
            else
            {
                return new ObjectResult(response) { StatusCode = 503 }; // Service Unavailable
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            
            return new ObjectResult(new
            {
                status = "Error during health check",
                healthy = false,
                timestamp = DateTime.UtcNow,
                service = "Fleet Agent Orchestration",
                version = "1.0.0",
                error = ex.Message
            })
            { StatusCode = 500 };
        }
    }
}

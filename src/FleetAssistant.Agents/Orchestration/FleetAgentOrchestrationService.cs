using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FleetAssistant.Shared.Models;

namespace FleetAssistant.Agents.Orchestration;

/// <summary>
/// Service implementation for fleet agent orchestration
/// </summary>
public class FleetAgentOrchestrationService : IFleetAgentOrchestrationService, IDisposable
{
    private readonly ILogger<FleetAgentOrchestrationService> _logger;
    private readonly IConfiguration _configuration;
    private FleetAgentOrchestrator? _orchestrator;
    private bool _initialized = false;
    private readonly object _initLock = new object();

    public FleetAgentOrchestrationService(
        ILogger<FleetAgentOrchestrationService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Initializes the orchestration service
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            try
            {
                _logger.LogInformation("Initializing Fleet Agent Orchestration Service");

                // Get configuration
                var azureAIEndpoint = _configuration["AZURE_AI_FOUNDRY_ENDPOINT"] 
                    ?? _configuration["AzureAI:FoundryEndpoint"]
                    ?? throw new InvalidOperationException("Azure AI Foundry endpoint not configured");

                var modelDeployment = _configuration["AZURE_AI_MODEL_DEPLOYMENT"] 
                    ?? _configuration["AzureAI:ModelDeployment"] 
                    ?? "gpt-4o";

                // Create orchestrator
                _orchestrator = new FleetAgentOrchestrator(
                    _logger.CreateLogger<FleetAgentOrchestrator>(), 
                    azureAIEndpoint, 
                    modelDeployment);

                _logger.LogInformation("Fleet Agent Orchestration Service initialized with endpoint: {Endpoint}", 
                    azureAIEndpoint);
                    
                _initialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Fleet Agent Orchestration Service");
                throw;
            }
        }

        // Initialize the orchestrator outside the lock (async operation)
        if (_orchestrator != null)
        {
            await _orchestrator.InitializeAsync();
        }
    }

    /// <summary>
    /// Processes a fleet query using the agent orchestration pattern
    /// </summary>
    public async Task<FleetQueryResponse> ProcessQueryAsync(FleetQueryRequest request, UserContext userContext)
    {
        if (!_initialized || _orchestrator == null)
        {
            await InitializeAsync();
        }

        if (_orchestrator == null)
        {
            throw new InvalidOperationException("Orchestrator not properly initialized");
        }

        try
        {
            _logger.LogInformation("Processing query for tenant {TenantId}: {Message}", 
                userContext.TenantId, request.Message);

            var response = await _orchestrator.ProcessQueryAsync(request, userContext);

            _logger.LogInformation("Successfully processed query for tenant {TenantId}", userContext.TenantId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query for tenant {TenantId}", userContext.TenantId);
            
            return new FleetQueryResponse
            {
                Response = "I apologize, but I'm currently unable to process your request. Please try again later.",
                AgentData = new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                },
                AgentsUsed = new List<string> { "ErrorHandler" },
                Warnings = new List<string> { "Service temporarily unavailable" },
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Gets the health status of the orchestration service
    /// </summary>
    public async Task<ServiceHealthStatus> GetHealthStatusAsync()
    {
        try
        {
            var status = new ServiceHealthStatus
            {
                LastChecked = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["initialized"] = _initialized,
                    ["orchestratorExists"] = _orchestrator != null,
                    ["configurationValid"] = !string.IsNullOrEmpty(_configuration["AZURE_AI_FOUNDRY_ENDPOINT"])
                }
            };

            if (_initialized && _orchestrator != null)
            {
                status.IsHealthy = true;
                status.Status = "Healthy - All agents operational";
                status.Details["agentCount"] = 5; // Triage + 4 specialists
            }
            else
            {
                status.IsHealthy = false;
                status.Status = "Unhealthy - Service not initialized";
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health status");
            
            return new ServiceHealthStatus
            {
                IsHealthy = false,
                Status = $"Error: {ex.Message}",
                LastChecked = DateTime.UtcNow,
                Details = new Dictionary<string, object> { ["error"] = ex.Message }
            };
        }
    }

    public void Dispose()
    {
        try
        {
            _orchestrator?.Dispose();
            _logger.LogInformation("Fleet Agent Orchestration Service disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Fleet Agent Orchestration Service");
        }
    }
}

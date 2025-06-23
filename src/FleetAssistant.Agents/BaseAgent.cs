using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using FleetAssistant.Shared.Models;
using FleetAssistant.Infrastructure.Plugins;

namespace FleetAssistant.Agents;

/// <summary>
/// Base class for all agents in the fleet management system
/// </summary>
public abstract class BaseAgent
{
    protected readonly ILogger _logger;
    protected readonly IKernelBuilder _kernelBuilder;
    protected readonly IIntegrationPluginRegistry? _pluginRegistry;

    protected BaseAgent(ILogger logger, IKernelBuilder kernelBuilder)
    {
        _logger = logger;
        _kernelBuilder = kernelBuilder;
        _pluginRegistry = null; // Will be injected in derived classes if needed
    }

    protected BaseAgent(ILogger logger, IKernelBuilder kernelBuilder, IIntegrationPluginRegistry pluginRegistry)
    {
        _logger = logger;
        _kernelBuilder = kernelBuilder;
        _pluginRegistry = pluginRegistry;
    }

    /// <summary>
    /// Creates a kernel instance with Azure AI Foundry integration for this agent
    /// </summary>
    /// <param name="userContext">User context for tenant-specific configuration</param>
    /// <returns>Configured kernel instance</returns>
    protected virtual async Task<Kernel> CreateKernelAsync(UserContext userContext)
    {
        try
        {
            // Create a new kernel builder for this agent instance
            var kernelBuilder = Kernel.CreateBuilder();

            // Add Azure OpenAI chat completion service
            // Note: In production, these would come from Azure AI Foundry configuration
            // For now, we'll use environment variables or configuration
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? 
                          "https://your-foundry-endpoint.openai.azure.com/";
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? 
                        "your-api-key-here";
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? 
                                "gpt-4o";

            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: deploymentName,
                endpoint: endpoint,
                apiKey: apiKey);

            // Register integration plugins for this agent and tenant
            var kernel = kernelBuilder.Build();
            await RegisterIntegrationPluginsAsync(kernel, userContext.TenantId);

            _logger.LogInformation("Created kernel for agent {AgentType} and tenant {TenantId}", 
                GetType().Name, userContext.TenantId);

            return kernel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create kernel for agent {AgentType} and tenant {TenantId}", 
                GetType().Name, userContext.TenantId);
            throw;
        }
    }    /// <summary>
    /// Registers integration plugins specific to this agent and tenant
    /// </summary>
    /// <param name="kernel">The kernel to register plugins with</param>
    /// <param name="tenantId">Tenant ID for filtering relevant plugins</param>
    protected virtual async Task RegisterIntegrationPluginsAsync(Kernel kernel, string tenantId)
    {
        try
        {
            _logger.LogInformation("Registering integration plugins for agent {AgentType} and tenant {TenantId}", 
                GetType().Name, tenantId);

            if (_pluginRegistry == null)
            {
                _logger.LogInformation("No plugin registry available for agent {AgentType}", GetType().Name);
                return;
            }

            // Get capabilities this agent needs
            var requiredCapabilities = GetRequiredCapabilities();
            
            // Load plugins that match the agent's capabilities
            var plugins = await _pluginRegistry.GetPluginsByCapabilitiesAsync(tenantId, requiredCapabilities);

            foreach (var plugin in plugins)
            {
                kernel.Plugins.Add(plugin);
                _logger.LogInformation("Added plugin {PluginName} to kernel for agent {AgentType}", 
                    plugin.Name, GetType().Name);
            }

            _logger.LogInformation("Successfully registered {Count} plugins for agent {AgentType} and tenant {TenantId}",
                plugins.Count, GetType().Name, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register integration plugins for agent {AgentType} and tenant {TenantId}", 
                GetType().Name, tenantId);
            // Don't throw - allow agent to continue without plugins
        }
    }

    /// <summary>
    /// Gets the list of capabilities this agent requires from integration plugins
    /// Override in derived classes to specify agent-specific capabilities
    /// </summary>
    /// <returns>List of required capabilities</returns>
    protected virtual IEnumerable<string> GetRequiredCapabilities()
    {
        // Default: return empty list (agent doesn't need specific integrations)
        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// Handles errors gracefully and returns partial results when possible
    /// </summary>
    /// <param name="ex">The exception that occurred</param>
    /// <param name="operation">Description of the operation that failed</param>
    /// <returns>A graceful error response</returns>
    protected virtual AgentErrorResponse HandleError(Exception ex, string operation)
    {
        _logger.LogError(ex, "Error in {AgentType} during {Operation}", GetType().Name, operation);

        return new AgentErrorResponse
        {
            Success = false,
            ErrorMessage = $"Error in {GetType().Name}: {ex.Message}",
            PartialData = null,
            Warnings = new List<string> { $"Failed to complete {operation}" }
        };
    }
}

/// <summary>
/// Standard response structure for agent operations
/// </summary>
public class AgentResponse
{
    public bool Success { get; set; } = true;
    public string Response { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Error response structure for failed agent operations
/// </summary>
public class AgentErrorResponse : AgentResponse
{
    public string ErrorMessage { get; set; } = string.Empty;
    public object? PartialData { get; set; }
}

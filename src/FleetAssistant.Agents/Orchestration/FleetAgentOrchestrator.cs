using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Contents;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using FleetAssistant.Shared.Models;
using FleetAssistant.Agents.Plugins;
using Azure.AI.Projects;

namespace FleetAssistant.Agents.Orchestration;

/// <summary>
/// Manages the handoff orchestration between specialized fleet management agents
/// </summary>
public class FleetAgentOrchestrator : IDisposable
{
    private readonly ILogger<FleetAgentOrchestrator> _logger;
    private readonly string _azureAIProjectEndpoint;
    private readonly string _modelDeploymentName;
    
    private AzureAIAgent? _triageAgent;
    private AzureAIAgent? _fuelAgent;
    private AzureAIAgent? _maintenanceAgent;
    private AzureAIAgent? _safetyAgent;
    private AzureAIAgent? _planningAgent;
    
    private HandoffOrchestration? _orchestration;
    private InProcessRuntime? _runtime;
    private PersistentAgentsClient? _agentsClient;
    
    private bool _disposed = false;

    public FleetAgentOrchestrator(
        ILogger<FleetAgentOrchestrator> logger,
        string azureAIProjectEndpoint,
        string modelDeploymentName = "gpt-4o")
    {
        _logger = logger;
        _azureAIProjectEndpoint = azureAIProjectEndpoint;
        _modelDeploymentName = modelDeploymentName;
    }

    /// <summary>
    /// Initializes all agents and sets up the handoff orchestration
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing Fleet Agent Orchestrator");

            // Create Azure AI Agents client
            _agentsClient = AzureAIAgent.CreateAgentsClient(_azureAIProjectEndpoint, new DefaultAzureCredential());

            // Create all specialized agents
            await CreateAgentsAsync();

            // Set up handoff relationships
            SetupHandoffRelationships();

            // Initialize runtime
            _runtime = new InProcessRuntime();
            await _runtime.StartAsync();

            _logger.LogInformation("Fleet Agent Orchestrator initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Fleet Agent Orchestrator");
            throw;
        }
    }

    /// <summary>
    /// Processes a fleet query using the handoff orchestration pattern
    /// </summary>
    public async Task<FleetQueryResponse> ProcessQueryAsync(FleetQueryRequest request, UserContext userContext)
    {
        if (_orchestration == null || _runtime == null)
        {
            throw new InvalidOperationException("Orchestrator not initialized. Call InitializeAsync first.");
        }

        var chatHistory = new List<ChatMessageContent>();
        
        try
        {
            _logger.LogInformation("Processing fleet query for tenant {TenantId}: {Message}", 
                userContext.TenantId, request.Message);

            // Configure response callback to capture agent interactions
            ValueTask ResponseCallback(ChatMessageContent response)
            {
                chatHistory.Add(response);
                _logger.LogDebug("Agent {AgentName}: {Content}", response.AuthorName, response.Content);
                return ValueTask.CompletedTask;
            }

            // Configure interactive callback for human-in-the-loop scenarios
            ValueTask<ChatMessageContent> InteractiveCallback()
            {
                // In a real scenario, this would prompt for user input
                // For now, we'll return the original request
                var userMessage = new ChatMessageContent(AuthorRole.User, request.Message);
                return ValueTask.FromResult(userMessage);
            }

            // Update orchestration callbacks
            _orchestration.ResponseCallback = ResponseCallback;
            _orchestration.InteractiveCallback = InteractiveCallback;

            // Create the initial task with tenant context
            var taskMessage = $"[Tenant: {userContext.TenantId}] {request.Message}";
            
            // Invoke the orchestration
            var result = await _orchestration.InvokeAsync(taskMessage, _runtime);
            var finalOutput = await result.GetValueAsync(TimeSpan.FromSeconds(300));

            // Extract agent information from chat history
            var agentsUsed = chatHistory
                .Where(m => !string.IsNullOrEmpty(m.AuthorName))
                .Select(m => m.AuthorName!)
                .Distinct()
                .ToList();

            // Create response
            var response = new FleetQueryResponse
            {
                Response = finalOutput ?? "No response generated",
                AgentData = new Dictionary<string, object>
                {
                    ["conversationHistory"] = chatHistory.Select(m => new
                    {
                        Agent = m.AuthorName,
                        Content = m.Content,
                        Timestamp = DateTime.UtcNow
                    }).ToList(),
                    ["totalInteractions"] = chatHistory.Count,
                    ["processingTime"] = DateTime.UtcNow
                },
                AgentsUsed = agentsUsed,
                Warnings = new List<string>(),
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully processed query for tenant {TenantId} using agents: {Agents}",
                userContext.TenantId, string.Join(", ", agentsUsed));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing fleet query for tenant {TenantId}", userContext.TenantId);
            
            return new FleetQueryResponse
            {
                Response = $"I apologize, but I encountered an error processing your request: {ex.Message}",
                AgentData = new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["conversationHistory"] = chatHistory.Select(m => new
                    {
                        Agent = m.AuthorName,
                        Content = m.Content,
                        Timestamp = DateTime.UtcNow
                    }).ToList()
                },
                AgentsUsed = new List<string> { "ErrorHandler" },
                Warnings = new List<string> { "Query processing failed" },
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Creates all specialized agents with their respective plugins
    /// </summary>
    private async Task CreateAgentsAsync()
    {
        if (_agentsClient == null)
        {
            throw new InvalidOperationException("Agents client not initialized");
        }

        // Create Triage Agent (main coordinator)
        var triageDefinition = await _agentsClient.Administration.CreateAgentAsync(
            _modelDeploymentName,
            name: "TriageAgent",
            description: "Fleet management triage agent that routes queries to appropriate specialists",
            instructions: @"You are the main triage agent for a fleet management system. 
                Analyze user queries and determine which specialized agent should handle the request:
                - Fuel-related queries: route to FuelAgent
                - Maintenance-related queries: route to MaintenanceAgent  
                - Safety-related queries: route to SafetyAgent
                - Planning, routing, or coordination queries: route to PlanningAgent
                
                Always be helpful and professional. If a query spans multiple domains, 
                start with the most relevant agent and coordinate as needed.");

        _triageAgent = new AzureAIAgent(triageDefinition, _agentsClient);

        // Create Fuel Agent
        var fuelDefinition = await _agentsClient.Administration.CreateAgentAsync(
            _modelDeploymentName,
            name: "FuelAgent",
            description: "Specialist agent for fuel management and optimization",
            instructions: @"You are a fuel management specialist for fleet operations. 
                Help with fuel monitoring, cost analysis, delivery scheduling, and optimization.
                Use your tools to provide accurate, data-driven responses about fuel-related queries.
                If the query is not fuel-related, transfer back to the TriageAgent.");

        _fuelAgent = new AzureAIAgent(fuelDefinition, _agentsClient, 
            plugins: [KernelPluginFactory.CreateFromObject(new FuelManagementPlugin())]);

        // Create Maintenance Agent
        var maintenanceDefinition = await _agentsClient.Administration.CreateAgentAsync(
            _modelDeploymentName,
            name: "MaintenanceAgent",
            description: "Specialist agent for vehicle maintenance and health monitoring",
            instructions: @"You are a maintenance specialist for fleet vehicles. 
                Help with maintenance scheduling, vehicle health monitoring, cost tracking, and service coordination.
                Use your tools to provide comprehensive maintenance information and recommendations.
                If the query is not maintenance-related, transfer back to the TriageAgent.");

        _maintenanceAgent = new AzureAIAgent(maintenanceDefinition, _agentsClient,
            plugins: [KernelPluginFactory.CreateFromObject(new MaintenancePlugin())]);

        // Create Safety Agent
        var safetyDefinition = await _agentsClient.Administration.CreateAgentAsync(
            _modelDeploymentName,
            name: "SafetyAgent",
            description: "Specialist agent for safety and compliance management",
            instructions: @"You are a safety and compliance specialist for fleet operations. 
                Help with driver safety monitoring, compliance tracking, incident reporting, and training coordination.
                Use your tools to ensure fleet safety standards and regulatory compliance.
                If the query is not safety-related, transfer back to the TriageAgent.");

        _safetyAgent = new AzureAIAgent(safetyDefinition, _agentsClient,
            plugins: [KernelPluginFactory.CreateFromObject(new SafetyPlugin())]);

        // Create Planning Agent
        var planningDefinition = await _agentsClient.Administration.CreateAgentAsync(
            _modelDeploymentName,
            name: "PlanningAgent", 
            description: "Specialist agent for fleet planning, routing, and coordination",
            instructions: @"You are a fleet planning and coordination specialist. 
                Help with route optimization, operation scheduling, resource allocation, and performance analysis.
                Use your tools to provide strategic insights and coordinate complex fleet operations.
                If the query is not planning-related, transfer back to the TriageAgent.");

        _planningAgent = new AzureAIAgent(planningDefinition, _agentsClient,
            plugins: [KernelPluginFactory.CreateFromObject(new PlanningPlugin())]);

        _logger.LogInformation("Created all specialized agents successfully");
    }

    /// <summary>
    /// Sets up the handoff relationships between agents
    /// </summary>
    private void SetupHandoffRelationships()
    {
        if (_triageAgent == null || _fuelAgent == null || _maintenanceAgent == null || 
            _safetyAgent == null || _planningAgent == null)
        {
            throw new InvalidOperationException("All agents must be created before setting up handoffs");
        }

        var handoffs = OrchestrationHandoffs
            .StartWith(_triageAgent)
            // Triage agent can hand off to any specialist
            .Add(_triageAgent, _fuelAgent, "Transfer to this agent for fuel-related queries")
            .Add(_triageAgent, _maintenanceAgent, "Transfer to this agent for maintenance-related queries") 
            .Add(_triageAgent, _safetyAgent, "Transfer to this agent for safety and compliance queries")
            .Add(_triageAgent, _planningAgent, "Transfer to this agent for planning, routing, and coordination queries")
            // Specialists can hand back to triage or to each other for complex queries
            .Add(_fuelAgent, _triageAgent, "Transfer back for non-fuel queries or general coordination")
            .Add(_fuelAgent, _planningAgent, "Transfer for route optimization considering fuel efficiency")
            .Add(_maintenanceAgent, _triageAgent, "Transfer back for non-maintenance queries or general coordination")
            .Add(_maintenanceAgent, _safetyAgent, "Transfer for safety-related maintenance issues")
            .Add(_safetyAgent, _triageAgent, "Transfer back for non-safety queries or general coordination")
            .Add(_safetyAgent, _maintenanceAgent, "Transfer for maintenance issues affecting safety")
            .Add(_planningAgent, _triageAgent, "Transfer back for non-planning queries or general coordination")
            .Add(_planningAgent, _fuelAgent, "Transfer for fuel considerations in route planning")
            .Add(_planningAgent, _maintenanceAgent, "Transfer for maintenance scheduling coordination");

        _orchestration = new HandoffOrchestration(
            handoffs,
            _triageAgent,
            _fuelAgent,
            _maintenanceAgent,
            _safetyAgent,
            _planningAgent);

        _logger.LogInformation("Set up handoff relationships between all agents");
    }

    /// <summary>
    /// Cleanup resources
    /// </summary>
    public async Task CleanupAsync()
    {
        if (_disposed) return;

        try
        {
            _logger.LogInformation("Cleaning up Fleet Agent Orchestrator");

            if (_runtime != null)
            {
                await _runtime.RunUntilIdleAsync();
                _runtime = null;
            }

            if (_agentsClient != null && _triageAgent != null)
            {
                await _agentsClient.Administration.DeleteAgentAsync(_triageAgent.Id);
                await _agentsClient.Administration.DeleteAgentAsync(_fuelAgent!.Id);
                await _agentsClient.Administration.DeleteAgentAsync(_maintenanceAgent!.Id);
                await _agentsClient.Administration.DeleteAgentAsync(_safetyAgent!.Id);
                await _agentsClient.Administration.DeleteAgentAsync(_planningAgent!.Id);
            }

            _logger.LogInformation("Fleet Agent Orchestrator cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Task.Run(async () => await CleanupAsync());
            _disposed = true;
        }
    }
}

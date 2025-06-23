using FleetAssistant.Infrastructure.Plugins;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FleetAssistant.Agents;

/// <summary>
/// Planning Agent that analyzes user queries and coordinates calls to specialized agents
/// </summary>
public class PlanningAgent : BaseAgent
{
    private readonly FuelAgent? _fuelAgent;
    private readonly MaintenanceAgent? _maintenanceAgent;
    private readonly SafetyAgent? _safetyAgent;

    public PlanningAgent(
        ILogger<PlanningAgent> logger, 
        IKernelBuilder kernelBuilder) 
        : base(logger, kernelBuilder)
    {
        _fuelAgent = null;
        _maintenanceAgent = null;
        _safetyAgent = null;
    }

    public PlanningAgent(
        ILogger<PlanningAgent> logger, 
        IKernelBuilder kernelBuilder, 
        IIntegrationPluginRegistry pluginRegistry,
        FuelAgent? fuelAgent = null,
        MaintenanceAgent? maintenanceAgent = null,
        SafetyAgent? safetyAgent = null) 
        : base(logger, kernelBuilder, pluginRegistry)
    {
        _fuelAgent = fuelAgent;
        _maintenanceAgent = maintenanceAgent;
        _safetyAgent = safetyAgent;
    }

    /// <summary>
    /// Processes a fleet query by analyzing intent and coordinating specialized agents
    /// </summary>
    /// <param name="request">The fleet query request</param>
    /// <param name="userContext">User context with tenant information</param>
    /// <returns>Coordinated response from multiple agents</returns>
    public async Task<FleetQueryResponse> ProcessQueryAsync(FleetQueryRequest request, UserContext userContext)
    {
        try
        {
            _logger.LogInformation("Planning agent processing query for tenant {TenantId}: {Message}", 
                userContext.TenantId, request.Message);            // Create kernel for this planning session
            var kernel = await CreateKernelAsync(userContext);

            // Analyze the user's intent and determine which agents to call
            var (planningResult, intendedAgents) = await AnalyzeIntentAsync(kernel, request, userContext);

            // Coordinate with specialized agents based on intent analysis
            var agentResponses = await CoordinateSpecializedAgentsAsync(request, userContext, intendedAgents);

            // Combine responses from all agents
            var combinedResponse = await CombineAgentResponsesAsync(kernel, planningResult, agentResponses, request, userContext);

            var response = new FleetQueryResponse
            {
                Response = combinedResponse.Response,
                AgentData = combinedResponse.Data,
                AgentsUsed = combinedResponse.AgentsUsed,
                Warnings = combinedResponse.Warnings,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Planning agent completed processing for tenant {TenantId}", userContext.TenantId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in planning agent for tenant {TenantId}", userContext.TenantId);

            var errorResponse = HandleError(ex, "query processing");
            return new FleetQueryResponse
            {
                Response = errorResponse.ErrorMessage,                AgentData = new Dictionary<string, object>
                {
                    ["error"] = errorResponse.ErrorMessage,
                    ["partialData"] = errorResponse.PartialData ?? "none"
                },
                AgentsUsed = new List<string> { "PlanningAgent" },
                Warnings = errorResponse.Warnings,
                Timestamp = DateTime.UtcNow
            };
        }
    }    /// <summary>
    /// Analyzes user intent and determines which specialized agents should be called
    /// </summary>
    private async Task<(AgentResponse planningResult, List<string> intendedAgents)> AnalyzeIntentAsync(Kernel kernel, FleetQueryRequest request, UserContext userContext)
    {
        try
        {
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

            // Create a conversation history
            var chatHistory = new ChatHistory();
            
            // Add system prompt for planning
            chatHistory.AddSystemMessage(GetPlanningSystemPrompt(userContext));

            // Add conversation history if provided
            if (request.ConversationHistory?.Any() == true)
            {
                foreach (var message in request.ConversationHistory.TakeLast(10)) // Limit to last 10 messages
                {
                    if (message.Role.ToLower() == "user")
                        chatHistory.AddUserMessage(message.Content);
                    else if (message.Role.ToLower() == "assistant")
                        chatHistory.AddAssistantMessage(message.Content);
                }
            }

            // Add current user message
            var userMessage = request.Message;
            if (request.Context?.Any() == true)
            {
                userMessage += $"\n\nAdditional context: {string.Join(", ", request.Context.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}";
            }
            chatHistory.AddUserMessage(userMessage);

            // Get planning response from Azure AI Foundry
            var result = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 1000,
                    Temperature = 0.3, // Lower temperature for more consistent planning
                    TopP = 0.9
                });

            var planningResponse = result.Content ?? "I understand you're asking about fleet management. Let me help you with that.";

            // Analyze what agents we would call (placeholder for now)
            var intendedAgents = AnalyzeRequiredAgents(request.Message);            return (new AgentResponse
            {
                Success = true,
                Response = planningResponse,
                Data = new Dictionary<string, object>
                {
                    ["userContext"] = new
                    {
                        apiKeyId = userContext.ApiKeyId,
                        apiKeyName = userContext.ApiKeyName,
                        tenantId = userContext.TenantId,
                        environment = userContext.Environment,
                        scopes = userContext.Scopes
                    },
                    ["queryAnalysis"] = new
                    {
                        originalMessage = request.Message,
                        hasConversationHistory = request.ConversationHistory?.Count > 0,
                        hasAdditionalContext = request.Context?.Count > 0,
                        intendedAgents = intendedAgents,
                        planningStrategy = "intent_analysis"
                    },
                    ["modelUsed"] = "azure-openai-gpt-4o"
                }
            }, intendedAgents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze intent for query: {Message}", request.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets the system prompt for the planning agent
    /// </summary>
    private string GetPlanningSystemPrompt(UserContext userContext)
    {
        return $@"You are a Fleet Management AI Planning Assistant for {userContext.TenantId}.

Your role is to analyze user queries about fleet management and provide helpful responses. You can help with:

**Vehicle Operations:**
- Vehicle location and tracking
- Fuel efficiency and consumption analysis  
- Maintenance scheduling and history
- Driver behavior and safety metrics

**Financial Analysis:**
- Total Cost of Ownership (TCO) calculations
- Fuel cost analysis
- Maintenance cost tracking
- Insurance and compliance costs

**Compliance & Safety:**
- DOT compliance monitoring
- Safety incident tracking
- Driver certification management
- Vehicle inspection schedules

**Reporting & Analytics:**
- Fleet performance dashboards
- Utilization reports
- Cost analysis and optimization
- Predictive maintenance recommendations

When responding:
1. Provide clear, actionable information
2. Ask clarifying questions if the request is ambiguous
3. Suggest related information that might be helpful
4. Explain any limitations in available data

Current tenant: {userContext.TenantId}
Available scopes: {string.Join(", ", userContext.Scopes)}

Be conversational but professional. Focus on practical fleet management insights.";
    }

    /// <summary>
    /// Analyzes the query to determine which specialized agents would be needed
    /// </summary>
    private List<string> AnalyzeRequiredAgents(string message)
    {
        var agents = new List<string>();
        var lowerMessage = message.ToLower();

        // Simple keyword-based analysis (will be enhanced with AI later)
        if (lowerMessage.Contains("fuel") || lowerMessage.Contains("gas") || lowerMessage.Contains("mpg") || lowerMessage.Contains("efficiency"))
            agents.Add("FuelAgent");

        if (lowerMessage.Contains("maintenance") || lowerMessage.Contains("repair") || lowerMessage.Contains("service"))
            agents.Add("MaintenanceAgent");

        if (lowerMessage.Contains("insurance") || lowerMessage.Contains("coverage") || lowerMessage.Contains("claim"))
            agents.Add("InsuranceAgent");

        if (lowerMessage.Contains("tax") || lowerMessage.Contains("depreciation") || lowerMessage.Contains("tco") || lowerMessage.Contains("cost"))
            agents.Add("TaxAgent");

        if (lowerMessage.Contains("location") || lowerMessage.Contains("route") || lowerMessage.Contains("tracking") || lowerMessage.Contains("gps"))
            agents.Add("LocationAgent");

        if (lowerMessage.Contains("driver") || lowerMessage.Contains("safety") || lowerMessage.Contains("behavior"))
            agents.Add("DriverAgent");

        // If no specific agents identified, suggest general fleet analysis
        if (!agents.Any())
            agents.Add("GeneralFleetAgent");

        return agents;
    }

    /// <summary>
    /// Coordinates calls to specialized agents based on intent analysis
    /// </summary>
    private async Task<Dictionary<string, AgentResponse>> CoordinateSpecializedAgentsAsync(
        FleetQueryRequest request, 
        UserContext userContext, 
        List<string> intendedAgents)
    {
        var agentResponses = new Dictionary<string, AgentResponse>();

        try
        {
            _logger.LogInformation("Coordinating {Count} specialized agents: {Agents}", 
                intendedAgents.Count, string.Join(", ", intendedAgents));

            // Call agents in parallel for better performance
            var agentTasks = new List<Task<(string agentName, AgentResponse response)>>();

            foreach (var agentName in intendedAgents)
            {
                switch (agentName.ToLower())
                {
                    case "fuelagent":
                        if (_fuelAgent != null)
                        {
                            agentTasks.Add(CallAgentAsync("FuelAgent", () => _fuelAgent.GetFuelAnalysisAsync(request, userContext)));
                        }
                        break;

                    case "maintenanceagent":
                        if (_maintenanceAgent != null)
                        {
                            agentTasks.Add(CallAgentAsync("MaintenanceAgent", () => _maintenanceAgent.GetMaintenanceAnalysisAsync(request, userContext)));
                        }
                        break;

                    case "safetyagent":
                        if (_safetyAgent != null)
                        {
                            agentTasks.Add(CallAgentAsync("SafetyAgent", () => _safetyAgent.GetSafetyAnalysisAsync(request, userContext)));
                        }
                        break;

                    default:
                        _logger.LogWarning("Unknown agent requested: {AgentName}", agentName);
                        break;
                }
            }

            // Wait for all agent calls to complete
            if (agentTasks.Any())
            {
                var results = await Task.WhenAll(agentTasks);
                foreach (var (agentName, response) in results)
                {
                    agentResponses[agentName] = response;
                }
            }

            _logger.LogInformation("Completed coordination with {Count} agents", agentResponses.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error coordinating specialized agents");
        }

        return agentResponses;
    }

    /// <summary>
    /// Helper method to call an agent with error handling
    /// </summary>
    private async Task<(string agentName, AgentResponse response)> CallAgentAsync(
        string agentName, 
        Func<Task<AgentResponse>> agentCall)
    {
        try
        {
            var response = await agentCall();
            return (agentName, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling {AgentName}", agentName);
            return (agentName, new AgentResponse
            {
                Success = false,
                Response = $"Error in {agentName}: {ex.Message}",
                Warnings = new List<string> { $"Failed to get response from {agentName}" }
            });
        }
    }    /// <summary>
    /// Combines responses from multiple agents into a cohesive response
    /// </summary>
    private async Task<(string Response, Dictionary<string, object> Data, List<string> AgentsUsed, List<string> Warnings)> CombineAgentResponsesAsync(
        Kernel kernel,
        AgentResponse planningResult,
        Dictionary<string, AgentResponse> agentResponses,
        FleetQueryRequest request,
        UserContext userContext)
    {
        try
        {
            var allWarnings = new List<string>(planningResult.Warnings);
            var agentsUsed = new List<string> { "PlanningAgent" };
            var combinedData = new Dictionary<string, object>();

            // Add planning data
            combinedData["planning"] = planningResult.Data;

            // Process agent responses
            foreach (var (agentName, response) in agentResponses)
            {
                agentsUsed.Add(agentName);
                allWarnings.AddRange(response.Warnings);
                combinedData[agentName.ToLower()] = response.Data;
            }

            // Use AI to synthesize the responses if we have multiple agents
            string finalResponse;
            if (agentResponses.Any())
            {
                finalResponse = await SynthesizeMultiAgentResponseAsync(kernel, planningResult, agentResponses, request, userContext);
            }
            else
            {
                finalResponse = planningResult.Response;
                allWarnings.Add("No specialized agents were available to provide detailed analysis");
            }

            return (finalResponse, combinedData, agentsUsed, allWarnings.Distinct().ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error combining agent responses");
            
            return (planningResult.Response, planningResult.Data, new List<string> { "PlanningAgent" }, new List<string> { "Error combining multi-agent responses" });
        }
    }

    /// <summary>
    /// Uses AI to synthesize responses from multiple agents into a cohesive answer
    /// </summary>
    private async Task<string> SynthesizeMultiAgentResponseAsync(
        Kernel kernel,
        AgentResponse planningResult,
        Dictionary<string, AgentResponse> agentResponses,
        FleetQueryRequest request,
        UserContext userContext)
    {
        try
        {
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            // Add system prompt for synthesis
            chatHistory.AddSystemMessage(GetSynthesisSystemPrompt(userContext));

            // Add the original query and agent responses
            var synthesisPrompt = $@"Original user query: {request.Message}

Planning analysis: {planningResult.Response}

Specialized agent responses:
{string.Join("\n\n", agentResponses.Select(kvp => $"{kvp.Key}: {kvp.Value.Response}"))}

Please synthesize these responses into a comprehensive, coherent answer that addresses the user's original query.";

            chatHistory.AddUserMessage(synthesisPrompt);

            var result = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 2000,
                    Temperature = 0.3,
                    TopP = 0.9
                });

            return result.Content ?? planningResult.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing multi-agent response");
            return planningResult.Response;
        }
    }

    /// <summary>
    /// Gets the system prompt for synthesizing multi-agent responses
    /// </summary>
    private string GetSynthesisSystemPrompt(UserContext userContext)
    {
        return $@"You are a Fleet Management AI Synthesis Coordinator for {userContext.TenantId}.

Your role is to combine insights from multiple specialized agents into a comprehensive, actionable response.

Guidelines:
1. Create a cohesive narrative that addresses the user's original question
2. Highlight key insights from each specialist agent
3. Identify connections and patterns across different data sources
4. Provide actionable recommendations that consider all aspects
5. Prioritize safety and compliance considerations
6. Be clear about data sources and limitations
7. Avoid redundancy while ensuring completeness

Focus on providing practical, implementable advice that leverages the combined expertise of all agents.";
    }
}

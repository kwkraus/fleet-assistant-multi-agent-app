using FleetAssistant.Infrastructure.Plugins;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FleetAssistant.Agents;

/// <summary>
/// Specialized agent for fuel-related queries and analysis
/// </summary>
public class FuelAgent : BaseAgent
{
    public FuelAgent(
        ILogger<FuelAgent> logger,
        IKernelBuilder kernelBuilder,
        IIntegrationPluginRegistry pluginRegistry)
        : base(logger, kernelBuilder, pluginRegistry)
    {
    }

    /// <summary>
    /// Processes fuel-related queries
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="userContext">User context with tenant information</param>
    /// <returns>Fuel analysis response</returns>
    public async Task<AgentResponse> GetFuelAnalysisAsync(FleetQueryRequest request, UserContext userContext)
    {
        try
        {
            _logger.LogInformation("Fuel agent processing query for tenant {TenantId}: {Message}",
                userContext.TenantId, request.Message);

            // Create kernel with fuel-specific plugins
            var kernel = await CreateKernelAsync(userContext);

            // Analyze fuel data using available integrations
            var fuelData = await AnalyzeFuelDataAsync(kernel, request, userContext);

            var response = new AgentResponse
            {
                Success = true,
                Response = fuelData.Response,
                Data = fuelData.Data,
                Warnings = fuelData.Warnings
            };

            _logger.LogInformation("Fuel agent completed processing for tenant {TenantId}", userContext.TenantId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fuel agent for tenant {TenantId}", userContext.TenantId);
            return HandleError(ex, "fuel analysis");
        }
    }

    /// <summary>
    /// Gets the capabilities this agent requires from integration plugins
    /// </summary>
    protected override IEnumerable<string> GetRequiredCapabilities()
    {
        return new[] { "fuel", "vehicle-data" };
    }

    /// <summary>
    /// Analyzes fuel data using available integration plugins
    /// </summary>
    private async Task<AgentResponse> AnalyzeFuelDataAsync(Kernel kernel, FleetQueryRequest request, UserContext userContext)
    {
        try
        {
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            // Add system prompt for fuel analysis
            chatHistory.AddSystemMessage(GetFuelAnalysisSystemPrompt(userContext));

            // Add user message
            var userMessage = $"Analyze fuel data for this query: {request.Message}";
            if (request.Context?.Any() == true)
            {
                userMessage += $"\n\nAdditional context: {string.Join(", ", request.Context.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}";
            }
            chatHistory.AddUserMessage(userMessage);

            // Get response from AI with access to fuel integration plugins
            var result = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 1500,
                    Temperature = 0.3,
                    TopP = 0.9,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                },
                kernel: kernel);

            var fuelAnalysis = result.Content ?? "I can help you analyze fuel data for your fleet.";

            // Collect data from any plugin calls
            var pluginData = new Dictionary<string, object>();
            var warnings = new List<string>();

            // Check if we have access to fuel plugins
            var availablePlugins = kernel.Plugins.Select(p => p.Name).ToList();
            if (!availablePlugins.Any())
            {
                warnings.Add("No fuel integration plugins available for this tenant");
            }

            return new AgentResponse
            {
                Success = true,
                Response = fuelAnalysis,
                Data = new Dictionary<string, object>
                {
                    ["agentType"] = "FuelAgent",
                    ["availableIntegrations"] = availablePlugins,
                    ["analysisCapabilities"] = GetRequiredCapabilities().ToList(),
                    ["pluginData"] = pluginData,
                    ["queryContext"] = new
                    {
                        originalMessage = request.Message,
                        hasContext = request.Context?.Count > 0,
                        tenantId = userContext.TenantId
                    }
                },
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze fuel data for tenant {TenantId}", userContext.TenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets the system prompt for fuel analysis
    /// </summary>
    private string GetFuelAnalysisSystemPrompt(UserContext userContext)
    {
        return $@"You are a Fleet Fuel Analysis Specialist for {userContext.TenantId}.

Your expertise includes:
- Fuel consumption analysis and optimization
- Fuel efficiency calculations and trends
- Fuel cost analysis and budgeting
- Route optimization for fuel savings
- Driver behavior impact on fuel efficiency
- Vehicle maintenance effects on fuel economy

Available Tools:
You have access to integration plugins that can retrieve real-time fuel data from various fleet management systems. Use these tools when specific vehicle data is requested.

Guidelines:
1. Provide specific, actionable fuel insights
2. Include cost analysis when relevant
3. Suggest optimization opportunities
4. Use actual data from integrations when available
5. Explain fuel efficiency metrics clearly
6. Consider seasonal and operational factors

Current tenant: {userContext.TenantId}
Focus on fuel-related aspects of the user's query and provide practical recommendations for fuel cost reduction and efficiency improvement.";
    }
}

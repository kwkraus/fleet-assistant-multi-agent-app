using FleetAssistant.Infrastructure.Plugins;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FleetAssistant.Agents;

/// <summary>
/// Specialized agent for driver safety and behavior analysis
/// </summary>
public class SafetyAgent : BaseAgent
{
    public SafetyAgent(
        ILogger<SafetyAgent> logger, 
        IKernelBuilder kernelBuilder, 
        IIntegrationPluginRegistry pluginRegistry) 
        : base(logger, kernelBuilder, pluginRegistry)
    {
    }

    /// <summary>
    /// Processes safety-related queries
    /// </summary>
    public async Task<AgentResponse> GetSafetyAnalysisAsync(FleetQueryRequest request, UserContext userContext)
    {
        try
        {
            _logger.LogInformation("Safety agent processing query for tenant {TenantId}: {Message}", 
                userContext.TenantId, request.Message);

            var kernel = await CreateKernelAsync(userContext);
            var safetyData = await AnalyzeSafetyDataAsync(kernel, request, userContext);

            var response = new AgentResponse
            {
                Success = true,
                Response = safetyData.Response,
                Data = safetyData.Data,
                Warnings = safetyData.Warnings
            };

            _logger.LogInformation("Safety agent completed processing for tenant {TenantId}", userContext.TenantId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in safety agent for tenant {TenantId}", userContext.TenantId);
            return HandleError(ex, "safety analysis");
        }
    }

    protected override IEnumerable<string> GetRequiredCapabilities()
    {
        return new[] { "safety", "driver-behavior", "compliance", "location" };
    }

    private async Task<AgentResponse> AnalyzeSafetyDataAsync(Kernel kernel, FleetQueryRequest request, UserContext userContext)
    {
        try
        {
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage(GetSafetyAnalysisSystemPrompt(userContext));

            var userMessage = $"Analyze safety data for this query: {request.Message}";
            if (request.Context?.Any() == true)
            {
                userMessage += $"\n\nAdditional context: {string.Join(", ", request.Context.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}";
            }
            chatHistory.AddUserMessage(userMessage);

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

            var safetyAnalysis = result.Content ?? "I can help you analyze safety and driver behavior data for your fleet.";

            var availablePlugins = kernel.Plugins.Select(p => p.Name).ToList();
            var warnings = new List<string>();

            if (!availablePlugins.Any())
            {
                warnings.Add("No safety integration plugins available for this tenant");
            }

            return new AgentResponse
            {
                Success = true,
                Response = safetyAnalysis,
                Data = new Dictionary<string, object>
                {
                    ["agentType"] = "SafetyAgent",
                    ["availableIntegrations"] = availablePlugins,
                    ["analysisCapabilities"] = GetRequiredCapabilities().ToList(),
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
            _logger.LogError(ex, "Failed to analyze safety data for tenant {TenantId}", userContext.TenantId);
            throw;
        }
    }

    private string GetSafetyAnalysisSystemPrompt(UserContext userContext)
    {
        return $@"You are a Fleet Safety Specialist for {userContext.TenantId}.

Your expertise includes:
- Driver behavior analysis and coaching
- Safety event investigation and prevention
- DOT compliance and Hours of Service (HOS) monitoring
- Risk assessment and mitigation strategies
- Safety training program development
- Accident prevention and reporting
- Vehicle inspection compliance

Available Tools:
You have access to integration plugins that can retrieve safety events, driver behavior scores, compliance data, and location tracking from various fleet management systems.

Guidelines:
1. Prioritize driver and public safety above all else
2. Focus on preventive safety measures
3. Provide actionable coaching recommendations
4. Ensure DOT and regulatory compliance
5. Identify high-risk patterns and behaviors
6. Use actual safety data when available
7. Recommend training or intervention when needed

Current tenant: {userContext.TenantId}
Focus on safety-related aspects of the user's query and provide practical recommendations for improving fleet safety and reducing risk.";
    }
}

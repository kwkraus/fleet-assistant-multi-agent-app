using FleetAssistant.Infrastructure.Plugins;
using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FleetAssistant.Agents;

/// <summary>
/// Specialized agent for maintenance-related queries and analysis
/// </summary>
public class MaintenanceAgent : BaseAgent
{
    public MaintenanceAgent(
        ILogger<MaintenanceAgent> logger,
        IKernelBuilder kernelBuilder,
        IIntegrationPluginRegistry pluginRegistry)
        : base(logger, kernelBuilder, pluginRegistry)
    {
    }

    /// <summary>
    /// Processes maintenance-related queries
    /// </summary>
    public async Task<AgentResponse> GetMaintenanceAnalysisAsync(FleetQueryRequest request, UserContext userContext)
    {
        try
        {
            _logger.LogInformation("Maintenance agent processing query for tenant {TenantId}: {Message}",
                userContext.TenantId, request.Message);

            var kernel = await CreateKernelAsync(userContext);
            var maintenanceData = await AnalyzeMaintenanceDataAsync(kernel, request, userContext);

            var response = new AgentResponse
            {
                Success = true,
                Response = maintenanceData.Response,
                Data = maintenanceData.Data,
                Warnings = maintenanceData.Warnings
            };

            _logger.LogInformation("Maintenance agent completed processing for tenant {TenantId}", userContext.TenantId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in maintenance agent for tenant {TenantId}", userContext.TenantId);
            return HandleError(ex, "maintenance analysis");
        }
    }

    protected override IEnumerable<string> GetRequiredCapabilities()
    {
        return new[] { "maintenance", "work-orders", "vehicle-data" };
    }

    private async Task<AgentResponse> AnalyzeMaintenanceDataAsync(Kernel kernel, FleetQueryRequest request, UserContext userContext)
    {
        try
        {
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage(GetMaintenanceAnalysisSystemPrompt(userContext));

            var userMessage = $"Analyze maintenance data for this query: {request.Message}";
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

            var maintenanceAnalysis = result.Content ?? "I can help you analyze maintenance data for your fleet.";

            var availablePlugins = kernel.Plugins.Select(p => p.Name).ToList();
            var warnings = new List<string>();

            if (!availablePlugins.Any())
            {
                warnings.Add("No maintenance integration plugins available for this tenant");
            }

            return new AgentResponse
            {
                Success = true,
                Response = maintenanceAnalysis,
                Data = new Dictionary<string, object>
                {
                    ["agentType"] = "MaintenanceAgent",
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
            _logger.LogError(ex, "Failed to analyze maintenance data for tenant {TenantId}", userContext.TenantId);
            throw;
        }
    }

    private string GetMaintenanceAnalysisSystemPrompt(UserContext userContext)
    {
        return $@"You are a Fleet Maintenance Specialist for {userContext.TenantId}.

Your expertise includes:
- Preventive maintenance scheduling and optimization
- Work order management and prioritization
- Maintenance cost analysis and budgeting
- Equipment lifecycle management
- Compliance with DOT and safety regulations
- Predictive maintenance recommendations
- Vendor and parts management

Available Tools:
You have access to integration plugins that can retrieve maintenance records, work orders, and vehicle diagnostic data from various fleet management systems.

Guidelines:
1. Prioritize safety and compliance requirements
2. Provide cost-effective maintenance recommendations
3. Focus on preventive over reactive maintenance
4. Consider vehicle utilization patterns
5. Identify opportunities for maintenance optimization
6. Use actual maintenance data when available

Current tenant: {userContext.TenantId}
Focus on maintenance-related aspects of the user's query and provide practical recommendations for maintenance optimization and cost control.";
    }
}

using FleetAssistant.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace FleetAssistant.Api.Services;

/// <summary>
/// Implementation of agent service client for Azure AI Foundry integration
/// </summary>
public class AgentServiceClient(ILogger<AgentServiceClient> logger, HttpClient httpClient) : IAgentServiceClient
{
    private readonly ILogger<AgentServiceClient> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;

    /// <summary>
    /// Sends a message to the agent service and streams the response
    /// </summary>
    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending message to agent service: {Message}, ConversationId: {ConversationId}", message, conversationId);

        // Generate mock response
        var mockResponse = GenerateMockFleetResponse(message);
        var words = mockResponse.Split(' ');

        foreach (var word in words)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Streaming cancelled by client");
                yield break;
            }

            // Simulate streaming delay
            await Task.Delay(100, cancellationToken);
            yield return word + " ";
        }
    }

    /// <summary>
    /// Generates a mock fleet management response based on the input message
    /// This should be replaced with actual Azure AI Foundry integration
    /// </summary>
    private string GenerateMockFleetResponse(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        if (lowerMessage.Contains("maintenance") || lowerMessage.Contains("service"))
        {
            return "Based on your fleet data, I recommend scheduling preventive maintenance for vehicles with high mileage. " +
                   "Vehicle #1001 is due for an oil change in 2 days, and Vehicle #1003 needs a brake inspection within the next week. " +
                   "Would you like me to create maintenance schedules for these vehicles?";
        }
        else if (lowerMessage.Contains("fuel") || lowerMessage.Contains("efficiency"))
        {
            return "Your fleet's fuel efficiency has improved by 8% this month compared to last month. " +
                   "The most efficient routes are Route A (15.2 MPG average) and Route C (14.8 MPG average). " +
                   "Consider optimizing Route B, which currently shows 12.1 MPG average. " +
                   "Driver training on eco-friendly driving techniques could further improve efficiency.";
        }
        else if (lowerMessage.Contains("route") || lowerMessage.Contains("logistics"))
        {
            return "I've analyzed your current route optimization. Here are my recommendations: " +
                   "1. Consolidate deliveries in the downtown area to reduce total distance by 15%. " +
                   "2. Shift heavy traffic routes to off-peak hours (before 7 AM or after 6 PM). " +
                   "3. Consider alternative Route D for northbound deliveries - it's 12 minutes faster on average. " +
                   "Would you like me to generate a detailed route optimization plan?";
        }
        else if (lowerMessage.Contains("safety") || lowerMessage.Contains("compliance"))
        {
            return "Your fleet safety metrics show: 2 minor incidents this month (down from 4 last month). " +
                   "All vehicles are compliant with DOT regulations. However, I notice Driver #247 has had 3 hard braking events this week. " +
                   "I recommend additional safety training. Vehicle #1005 is due for safety inspection in 5 days. " +
                   "Overall safety score: 94/100 (Excellent).";
        }
        else if (lowerMessage.Contains("cost") || lowerMessage.Contains("expense") || lowerMessage.Contains("budget"))
        {
            return "Fleet cost analysis for this month: Total operating costs are $47,230 (3% under budget). " +
                   "Breakdown: Fuel costs $28,500, Maintenance $12,200, Insurance $4,830, Other $1,700. " +
                   "Compared to last month, you've saved $1,420 primarily through fuel efficiency improvements. " +
                   "I project continued savings if current optimization trends continue.";
        }
        else if (lowerMessage.Contains("hello") || lowerMessage.Contains("hi") || lowerMessage.Contains("help"))
        {
            return "Hello! I'm your Fleet Assistant, here to help you manage your fleet operations efficiently. " +
                   "I can assist you with maintenance schedules, fuel efficiency optimization, route planning, " +
                   "safety compliance, cost analysis, and operational insights. " +
                   "What aspect of your fleet management would you like to explore today?";
        }
        else
        {
            return "Thank you for your question about fleet management. I can help you with various aspects including " +
                   "vehicle maintenance, fuel efficiency, route optimization, safety compliance, and cost analysis. " +
                   "Could you please provide more specific details about what you'd like assistance with? " +
                   "For example, you could ask about maintenance schedules, fuel costs, route planning, or safety metrics.";
        }
    }
}

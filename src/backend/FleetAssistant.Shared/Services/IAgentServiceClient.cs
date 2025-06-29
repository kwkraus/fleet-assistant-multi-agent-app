using FleetAssistant.Shared.Models;

namespace FleetAssistant.Shared.Services;

/// <summary>
/// Interface for Azure AI Foundry Agent Service client
/// </summary>
public interface IAgentServiceClient
{
    /// <summary>
    /// Sends a message to the hosted agent and returns a streaming response
    /// </summary>
    /// <param name="request">The fleet query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of response chunks</returns>
    IAsyncEnumerable<string> SendMessageStreamAsync(FleetQueryRequest request, CancellationToken cancellationToken = default);
}

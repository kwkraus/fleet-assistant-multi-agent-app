namespace FleetAssistant.Shared.Services;

/// <summary>
/// Interface for Azure AI Foundry Agent Service client
/// </summary>
public interface IAgentServiceClient
{
    /// <summary>
    /// Sends a message to the hosted agent and returns a streaming response
    /// </summary>
    /// <param name="conversationId">The conversation ID for context</param>
    /// <param name="message">The user's message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of response chunks</returns>
    IAsyncEnumerable<string> SendMessageStreamAsync(string conversationId, string message, CancellationToken cancellationToken = default);
}

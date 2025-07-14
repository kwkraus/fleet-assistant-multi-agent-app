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
    /// <param name="conversationId">The conversation ID for context</param>
    /// <param name="message">The user's message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of response chunks</returns>
    IAsyncEnumerable<string> SendMessageStreamAsync(string conversationId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message with file attachments to the hosted agent and returns a streaming response
    /// </summary>
    /// <param name="conversationId">The conversation ID for context</param>
    /// <param name="message">The user's message</param>
    /// <param name="files">Base64 encoded files to attach</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of response chunks</returns>
    IAsyncEnumerable<string> SendMessageWithFilesStreamAsync(string conversationId, string message, List<Base64File> files, CancellationToken cancellationToken = default);
}

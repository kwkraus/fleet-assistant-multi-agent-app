using FleetAssistant.Shared.Models;
using FleetAssistant.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FleetAssistant.WebApi.Controllers;

/// <summary>
/// Chat API controller for streaming chat responses compatible with Vercel AI SDK
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "text/event-stream")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IAgentServiceClient _agentServiceClient;

    public ChatController(
        ILogger<ChatController> logger,
        IAgentServiceClient agentServiceClient)
    {
        _logger = logger;
        _agentServiceClient = agentServiceClient;
    }

    /// <summary>
    /// Send a chat message and receive a streaming response
    /// </summary>
    /// <param name="chatRequest">The chat request containing messages and optional conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Server-Sent Events stream with the agent's response</returns>
    /// <response code="200">Returns a streaming chat response</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpPost]
    [ProducesResponseType(typeof(void), 200, "text/event-stream")]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest chatRequest, CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Received chat request. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            // Validate the chat request
            if (chatRequest?.Messages == null || chatRequest.Messages.Count == 0)
            {
                _logger.LogWarning("Invalid chat request - no messages. CorrelationId: {CorrelationId}", correlationId);
                return BadRequest(new { error = "Messages array is required" });
            }

            // Get the last user message (most recent)
            var lastUserMessage = chatRequest.Messages
                .Where(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                .LastOrDefault();

            if (lastUserMessage == null || string.IsNullOrWhiteSpace(lastUserMessage.Content))
            {
                _logger.LogWarning("No user message found in chat request. CorrelationId: {CorrelationId}", correlationId);
                return BadRequest(new { error = "At least one user message is required" });
            }

            // Extract conversationId
            var conversationId = chatRequest.ConversationId ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Processing chat message: {Message}, ConversationId: {ConversationId}, CorrelationId: {CorrelationId}",
                lastUserMessage.Content, conversationId, correlationId);

            // Set Server-Sent Events headers
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            var messageId = Guid.NewGuid().ToString();

            try
            {
                // Send initial metadata event
                await WriteSSEEvent("metadata", new
                {
                    conversationId = conversationId,
                    messageId = messageId,
                    timestamp = DateTime.UtcNow
                }, cancellationToken);

                var contentBuilder = new StringBuilder();

                // Stream chunks as they arrive
                await foreach (var chunk in _agentServiceClient.SendMessageStreamAsync(conversationId, lastUserMessage.Content, cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Chat request cancelled during streaming. CorrelationId: {CorrelationId}", correlationId);
                        break;
                    }

                    contentBuilder.Append(chunk);

                    // Send chunk event
                    await WriteSSEEvent("chunk", new
                    {
                        content = chunk
                    }, cancellationToken);
                }

                // Send completion event
                await WriteSSEEvent("done", new
                {
                    messageId = messageId,
                    totalContent = contentBuilder.ToString().Trim(),
                    timestamp = DateTime.UtcNow
                }, cancellationToken);

                _logger.LogInformation("Successfully completed streaming chat response. ConversationId: {ConversationId}, CorrelationId: {CorrelationId}", conversationId, correlationId);
            }
            catch (Exception streamingException)
            {
                _logger.LogError(streamingException, "Error during streaming. CorrelationId: {CorrelationId}", correlationId);

                // Send error event
                await WriteSSEEvent("error", new
                {
                    message = "An error occurred while streaming the response.",
                    correlationId = correlationId
                }, CancellationToken.None);
            }

            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat request was cancelled. CorrelationId: {CorrelationId}", correlationId);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new { error = "Internal server error", correlationId });
        }
    }

    /// <summary>
    /// Handle CORS preflight requests for the chat endpoint
    /// </summary>
    /// <returns>OK response with CORS headers</returns>
    [HttpOptions]
    public IActionResult HandleOptions()
    {
        _logger.LogInformation("Handling CORS preflight for chat endpoint");
        return Ok();
    }

    /// <summary>
    /// Get the health status of the chat service
    /// </summary>
    /// <returns>Health status information</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "FleetAssistant.WebApi"
        });
    }

    /// <summary>
    /// Helper method to write Server-Sent Events to the response stream
    /// </summary>
    private async Task WriteSSEEvent(string eventType, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { type = eventType, data }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var sseData = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(sseData);

            await Response.Body.WriteAsync(bytes, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException || cancellationToken.IsCancellationRequested)
        {
            // Client disconnected, ignore
        }
    }
}

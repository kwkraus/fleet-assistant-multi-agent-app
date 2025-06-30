using FleetAssistant.Shared.Models;
using FleetAssistant.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FleetAssistant.Api.Functions;

/// <summary>
/// Chat endpoint compatible with Vercel AI SDK for streaming chat responses
/// </summary>
public class ChatFunction(
    ILogger<ChatFunction> logger,
    IAgentServiceClient agentServiceClient)
{
    private readonly ILogger<ChatFunction> _logger = logger;
    private readonly IAgentServiceClient _agentServiceClient = agentServiceClient;

    [Function("Chat")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] HttpRequest req)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Received chat request. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            // Parse the chat request
            ChatRequest? chatRequest;
            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (chatRequest?.Messages == null || chatRequest.Messages.Count == 0)
                {
                    _logger.LogWarning("Invalid chat request - no messages. CorrelationId: {CorrelationId}", correlationId);
                    return new BadRequestObjectResult(new { error = "Messages array is required" });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse chat request JSON. CorrelationId: {CorrelationId}", correlationId);
                return new BadRequestObjectResult(new { error = "Invalid JSON format" });
            }

            // Get the last user message (most recent)
            var lastUserMessage = chatRequest.Messages
                .Where(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                .LastOrDefault();

            if (lastUserMessage == null || string.IsNullOrWhiteSpace(lastUserMessage.Content))
            {
                _logger.LogWarning("No user message found in chat request. CorrelationId: {CorrelationId}", correlationId);
                return new BadRequestObjectResult(new { error = "At least one user message is required" });
            }

            // Extract conversationId
            var conversationId = chatRequest.ConversationId ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Processing chat message: {Message}, ConversationId: {ConversationId}, CorrelationId: {CorrelationId}",
                lastUserMessage.Content, conversationId, correlationId);

            // Set CORS headers for streaming
            req.HttpContext.Response.Headers.AccessControlAllowOrigin = "*";
            req.HttpContext.Response.Headers.AccessControlAllowMethods = "POST, OPTIONS";
            req.HttpContext.Response.Headers.AccessControlAllowHeaders = "Content-Type, Authorization";
            
            // Set Server-Sent Events headers
            req.HttpContext.Response.Headers.ContentType = "text/event-stream";
            req.HttpContext.Response.Headers.CacheControl = "no-cache";
            req.HttpContext.Response.Headers.Connection = "keep-alive";

            var messageId = Guid.NewGuid().ToString();

            try
            {
                // Send initial metadata event
                await WriteSSEEvent(req.HttpContext.Response, "metadata", new
                {
                    conversationId = conversationId,
                    messageId = messageId,
                    timestamp = DateTime.UtcNow
                }, req.HttpContext.RequestAborted);

                var contentBuilder = new StringBuilder();

                // Stream chunks as they arrive
                await foreach (var chunk in _agentServiceClient.SendMessageStreamAsync(conversationId, lastUserMessage.Content, req.HttpContext.RequestAborted))
                {
                    if (req.HttpContext.RequestAborted.IsCancellationRequested)
                    {
                        _logger.LogInformation("Chat request cancelled during streaming. CorrelationId: {CorrelationId}", correlationId);
                        break;
                    }

                    contentBuilder.Append(chunk);

                    // Send chunk event
                    await WriteSSEEvent(req.HttpContext.Response, "chunk", new
                    {
                        content = chunk
                    }, req.HttpContext.RequestAborted);
                }

                // Send completion event
                await WriteSSEEvent(req.HttpContext.Response, "done", new
                {
                    messageId = messageId,
                    totalContent = contentBuilder.ToString().Trim(),
                    timestamp = DateTime.UtcNow
                }, req.HttpContext.RequestAborted);

                _logger.LogInformation("Successfully completed streaming chat response. ConversationId: {ConversationId}, CorrelationId: {CorrelationId}", conversationId, correlationId);
            }
            catch (Exception streamingException)
            {
                _logger.LogError(streamingException, "Error during streaming. CorrelationId: {CorrelationId}", correlationId);
                
                // Send error event
                await WriteSSEEvent(req.HttpContext.Response, "error", new
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
            return new ObjectResult(new { error = "Internal server error", correlationId })
            {
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Handle CORS preflight requests
    /// </summary>
    [Function("ChatOptions")]
    public IActionResult HandleOptions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "chat")] HttpRequest req)
    {
        _logger.LogInformation("Handling CORS preflight for chat endpoint");

        req.HttpContext.Response.Headers.AccessControlAllowOrigin = "*";
        req.HttpContext.Response.Headers.AccessControlAllowMethods = "POST, OPTIONS";
        req.HttpContext.Response.Headers.AccessControlAllowHeaders = "Content-Type, Authorization";
        req.HttpContext.Response.Headers.AccessControlMaxAge = "86400";

        return new OkResult();
    }

    /// <summary>
    /// Helper method to write Server-Sent Events to the response stream
    /// </summary>
    private static async Task WriteSSEEvent(HttpResponse response, string eventType, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { type = eventType, data }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var sseData = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(sseData);
            
            await response.Body.WriteAsync(bytes, cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException || cancellationToken.IsCancellationRequested)
        {
            // Client disconnected, ignore
        }
    }
}

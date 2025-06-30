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

            // Set CORS headers
            req.HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
            req.HttpContext.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
            req.HttpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";

            var messageId = Guid.NewGuid().ToString();
            var responseBuilder = new StringBuilder();

            // Collect the complete response
            await foreach (var chunk in _agentServiceClient.SendMessageStreamAsync(conversationId, lastUserMessage.Content, req.HttpContext.RequestAborted))
            {
                if (req.HttpContext.RequestAborted.IsCancellationRequested)
                {
                    _logger.LogInformation("Chat request cancelled. CorrelationId: {CorrelationId}", correlationId);
                    break;
                }

                responseBuilder.Append(chunk);
            }

            var completeResponse = responseBuilder.ToString().Trim();

            // Return the response in the exact format the AI SDK expects
            var chatMessage = new ChatMessage
            {
                Id = messageId,
                Role = "assistant",
                Content = completeResponse,
                CreatedAt = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(chatMessage, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Successfully completed chat response. CorrelationId: {CorrelationId}", correlationId);

            return new ContentResult
            {
                Content = jsonResponse,
                ContentType = "application/json",
                StatusCode = 200
            };
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
}

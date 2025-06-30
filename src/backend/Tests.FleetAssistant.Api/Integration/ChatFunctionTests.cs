using FleetAssistant.Shared.Models;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tests.FleetAssistant.Api.Integration;

/// <summary>
/// Integration tests for the Chat function endpoint
/// These tests assume Azure Functions is running locally on port 7071
/// </summary>
public class ChatFunctionTests
{
    private readonly HttpClient _client;
    private readonly string _baseUrl = "http://localhost:7071";

    public ChatFunctionTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Chat_WithValidRequest_ReturnsStreamingResponse()
    {
        // Arrange
        var chatRequest = new ChatRequest
        {
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Role = "user", Content = "Tell me about fleet maintenance" }
            }
        };

        var json = JsonSerializer.Serialize(chatRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        try
        {
            var response = await _client.PostAsync($"{_baseUrl}/api/chat", content);

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(responseContent);
            Assert.Contains("maintenance", responseContent.ToLowerInvariant());
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused"))
        {
            // Skip test if Azure Functions is not running
            Assert.Fail("Azure Functions is not running on localhost:7071. Start it with 'func start' to run this test.");
        }
    }

    [Fact]
    public async Task ChatOptions_ReturnsCorrectCorsHeaders()
    {
        try
        {
            // Act
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Options, $"{_baseUrl}/api/chat"));

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
            Assert.True(response.Headers.Contains("Access-Control-Allow-Methods"));
            Assert.True(response.Headers.Contains("Access-Control-Allow-Headers"));
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused"))
        {
            // Skip test if Azure Functions is not running
            Assert.Fail("Azure Functions is not running on localhost:7071. Start it with 'func start' to run this test.");
        }
    }
}

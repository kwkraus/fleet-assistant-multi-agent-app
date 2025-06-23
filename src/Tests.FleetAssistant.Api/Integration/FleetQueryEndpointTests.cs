using FleetAssistant.Shared.Models;
using System.Text.Json;

namespace Tests.FleetAssistant.Api.Integration;

/// <summary>
/// Integration tests demonstrating the fleet query endpoint
/// Note: These are demonstration tests showing the expected behavior
/// In a real integration test environment, you would use TestHost/WebApplicationFactory
/// </summary>
public class FleetQueryEndpointTests
{
    [Fact]
    public void FleetQueryRequest_SerializesCorrectly()
    {
        // Arrange
        var request = new FleetQueryRequest
        {
            Message = "What's the fuel efficiency of vehicle ABC123?",
            ConversationHistory = new List<ConversationMessage>
            {
                new ConversationMessage
                {
                    Role = "user",
                    Content = "Hello",
                    Timestamp = DateTime.UtcNow.AddMinutes(-5)
                }
            },
            Context = new Dictionary<string, object>
            {
                ["vehicleId"] = "ABC123",
                ["timeframe"] = "last30days"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var deserializedRequest = JsonSerializer.Deserialize<FleetQueryRequest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(deserializedRequest);
        Assert.Equal(request.Message, deserializedRequest.Message);
        Assert.Single(deserializedRequest.ConversationHistory!);
        Assert.Equal("ABC123", deserializedRequest.Context!["vehicleId"].ToString());
    }

    [Fact]
    public void FleetQueryResponse_SerializesCorrectly()
    {
        // Arrange
        var response = new FleetQueryResponse
        {
            Response = "Vehicle ABC123 has an average fuel efficiency of 8.5 MPG over the last 30 days.",
            AgentData = new Dictionary<string, object>
            {
                ["fuelData"] = new { averageMpg = 8.5, totalGallons = 120.5 },
                ["maintenanceData"] = new { lastService = "2024-11-15", nextDue = "2024-12-15" }
            },
            Warnings = new List<string> { "Some data unavailable due to GPS tracking issue" },
            AgentsUsed = new List<string> { "FuelAgent", "MaintenanceAgent" },
            ProcessingTimeMs = 1250
        };

        // Act
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var deserializedResponse = JsonSerializer.Deserialize<FleetQueryResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(deserializedResponse);
        Assert.Equal(response.Response, deserializedResponse.Response);
        Assert.Equal(2, deserializedResponse.AgentData.Count);
        Assert.Single(deserializedResponse.Warnings!);
        Assert.Equal(2, deserializedResponse.AgentsUsed.Count);
        Assert.Equal(1250, deserializedResponse.ProcessingTimeMs);
    }
    [Fact]
    public void ApiKey_FollowsExpectedFormat()
    {
        // This demonstrates the expected API key format: fa_dev_xxxxxxxxxxxxxxxxxxxx
        var sampleApiKey = "fa_dev_1234567890abcdef12345678";

        // Test API key format validation
        Assert.StartsWith("fa_dev_", sampleApiKey);
        Assert.Equal(31, sampleApiKey.Length); // fa_dev_ (7) + 24 random chars        // This API key format can be used to test the endpoint:
        // curl -X POST https://localhost:7071/api/fleet/query \
        //   -H "Authorization: Bearer fa_dev_1234567890abcdef12345678" \
        //   -H "Content-Type: application/json" \
        //   -d '{"message": "What vehicles need maintenance?"}'
        //
        // Or using X-API-Key header:
        // curl -X POST https://localhost:7071/api/fleet/query \
        //   -H "X-API-Key: fa_dev_1234567890abcdef12345678" \
        //   -H "Content-Type: application/json" \
        //   -d '{"message": "What vehicles need maintenance?"}'

        Assert.True(sampleApiKey.Length == 31);
    }

    [Fact]
    public void UserContext_WithApiKey_SerializesCorrectly()
    {
        // Arrange
        var userContext = new UserContext
        {
            ApiKeyId = "key_123",
            ApiKeyName = "Contoso Fleet API Key",
            TenantId = "contoso-fleet",
            Environment = "development",
            Scopes = new List<string> { "fleet:read", "fleet:query", "fleet:admin" },
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastUsedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                ["created_by"] = "admin@contoso.com",
                ["purpose"] = "API testing"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(userContext, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var deserializedContext = JsonSerializer.Deserialize<UserContext>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(deserializedContext);
        Assert.Equal(userContext.ApiKeyId, deserializedContext.ApiKeyId);
        Assert.Equal(userContext.TenantId, deserializedContext.TenantId);
        Assert.Equal(3, deserializedContext.Scopes.Count);
        Assert.Equal(2, deserializedContext.Metadata.Count);
    }
}

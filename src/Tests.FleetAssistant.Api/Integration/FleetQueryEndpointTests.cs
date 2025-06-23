using FleetAssistant.Shared.Models;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Xunit;

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
    public void CreateSampleJwtToken_ForDevelopmentTesting()
    {
        // This demonstrates how to create a JWT token for testing the endpoint
        var handler = new JwtSecurityTokenHandler();
        var claims = new[]
        {
            new System.Security.Claims.Claim("sub", "test-user-123"),
            new System.Security.Claims.Claim("email", "fleet.manager@contoso.com"),
            new System.Security.Claims.Claim("tenant_id", "contoso-fleet"),
            new System.Security.Claims.Claim("roles", "fleet-manager"),
            new System.Security.Claims.Claim("tenants", "contoso-fleet"),
            new System.Security.Claims.Claim("tenants", "contoso-backup")
        };

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "fleet-assistant-dev",
            Audience = "fleet-assistant-api"
        };

        var token = handler.CreateEncodedJwt(tokenDescriptor);

        // This token can be used to test the API endpoint:
        // curl -X POST https://localhost:7071/api/fleet/query \
        //   -H "Authorization: Bearer {token}" \
        //   -H "Content-Type: application/json" \
        //   -d '{"message": "What vehicles need maintenance?"}'

        Assert.NotNull(token);
        Assert.True(token.Length > 100); // Basic sanity check
    }
}

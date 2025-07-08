# Planning Agent Implementation Guide

The Planning Agent has been successfully implemented as part of the Fleet Management AI Assistant. This document explains how to configure and test the Planning Agent with Azure AI Foundry.

## Overview

The Planning Agent is responsible for:
- Analyzing user queries about fleet management
- Determining what kind of assistance is needed
- Providing intelligent responses using Azure OpenAI
- Coordinating with specialized agents (future implementation)

## Configuration

### 1. Azure OpenAI Setup

To use the Planning Agent with real Azure OpenAI/Azure AI Foundry, update the following environment variables in `local.settings.json`:

```json
{
    "Values": {
        "AZURE_OPENAI_ENDPOINT": "https://your-foundry-endpoint.openai.azure.com/",
        "AZURE_OPENAI_API_KEY": "your-api-key-here",
        "AZURE_OPENAI_DEPLOYMENT_NAME": "gpt-4o"
    }
}
```

### 2. Development/Testing Mode

For development without Azure OpenAI configured, the Planning Agent will:
- Handle errors gracefully
- Return meaningful error messages
- Continue to work for testing API structure

## Architecture

### Planning Agent Flow

1. **Request Reception**: Receives `FleetQueryRequest` from the main HTTP endpoint
2. **Kernel Creation**: Creates a tenant-specific Semantic Kernel instance
3. **Intent Analysis**: Uses Azure OpenAI to analyze the user's query
4. **Agent Coordination**: Determines which specialized agents to call (planned)
5. **Response Generation**: Returns a coordinated `FleetQueryResponse`

### Key Classes

- **`BaseAgent`**: Abstract base class with common functionality
- **`PlanningAgent`**: Main orchestrator for fleet queries
- **`AgentResponse`**: Standardized response structure
- **`AgentErrorResponse`**: Error handling with graceful degradation

## Testing

### 1. Unit Tests

Run the agent-specific tests:
```bash
dotnet test Tests.FleetAssistant.Agents
```

### 2. Integration Testing

Test the full flow with API Key authentication:

```bash
# Start the function
cd src/FleetAssistant.Api
func start

# Test with PowerShell
$headers = @{ "Authorization" = "Bearer <your-development-api-key>"; "Content-Type" = "application/json" }
$body = @{ 
    message = "What vehicles need maintenance in the next 30 days?"
    context = @{ timeframe = "next30days"; priority = "high" }
} | ConvertTo-Json -Depth 2

Invoke-RestMethod -Uri "http://localhost:7071/api/fleet/query" -Method Post -Headers $headers -Body $body
```

### 3. Expected Response Structure

```json
{
  "response": "AI-generated response about fleet maintenance needs...",
  "agentData": {
    "userContext": {
      "apiKeyId": "key-id",
      "tenantId": "tenant-name",
      "scopes": ["fleet:read", "fleet:query"]
    },
    "queryAnalysis": {
      "originalMessage": "What vehicles need maintenance...",
      "intendedAgents": ["MaintenanceAgent"],
      "planningStrategy": "intent_analysis"
    },
    "modelUsed": "azure-openai-gpt-4o"
  },
  "agentsUsed": ["PlanningAgent"],
  "warnings": [],
  "timestamp": "2024-06-23T16:30:00.000Z",
  "processingTimeMs": 1250
}
```

## Error Handling

The Planning Agent implements graceful error handling:

### Without Azure OpenAI Configuration

If Azure OpenAI is not configured, the agent will:
- Log the configuration issue
- Return a helpful error message
- Suggest configuration steps
- Not crash the entire system

### With Network/API Issues

If Azure OpenAI is temporarily unavailable:
- Returns partial response with warning
- Logs the issue for monitoring
- Suggests retrying the request

## System Prompt

The Planning Agent uses a comprehensive system prompt that includes:

- **Role Definition**: Fleet Management AI Planning Assistant
- **Capabilities**: Vehicle operations, financial analysis, compliance, reporting
- **Tenant Context**: Uses tenant ID and scopes for personalization
- **Response Guidelines**: Clear, actionable, professional tone

## Intent Analysis

The Planning Agent analyzes queries to identify required specialist agents:

- **FuelAgent**: Fuel efficiency, consumption, costs
- **MaintenanceAgent**: Service schedules, repairs, inspections
- **InsuranceAgent**: Coverage, claims, compliance
- **TaxAgent**: Depreciation, tax implications, TCO
- **LocationAgent**: GPS tracking, routing, geofencing
- **DriverAgent**: Safety, behavior, certifications

## Next Steps

### 1. Specialized Agent Implementation

Next iteration will implement:
- Individual specialized agents (FuelAgent, MaintenanceAgent, etc.)
- Agent coordination logic in PlanningAgent
- Parallel agent execution with result aggregation

### 2. Integration Plugin System

Following the blueprint:
- Plugin registry for tenant-specific integrations
- GeoTab, Fleetio, Samsara plugin implementations
- Dynamic plugin loading based on tenant configuration

### 3. Advanced Features

Future enhancements:
- Conversation memory management
- Multi-turn conversation support
- Predictive analytics integration
- Real-time data streaming

## Monitoring

The Planning Agent includes comprehensive logging:

- **Request Tracking**: Each query with tenant context
- **Performance Metrics**: Processing time, model usage
- **Error Tracking**: Detailed error information with context
- **Agent Coordination**: Which agents were called and why

## Production Considerations

For production deployment:

1. **Security**: Store Azure OpenAI keys in Azure Key Vault
2. **Scaling**: Configure appropriate Azure OpenAI quotas
3. **Monitoring**: Set up Application Insights dashboards
4. **Rate Limiting**: Implement tenant-specific quotas
5. **Cost Control**: Monitor and alert on Azure OpenAI usage

The Planning Agent is now ready for development and testing, providing a solid foundation for the complete multi-agent fleet management system.

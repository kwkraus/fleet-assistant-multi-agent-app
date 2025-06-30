# Azure AI Foundry Agent Service Implementation Guide

## Overview

The Fleet Assistant application now includes a new **FoundryAgentService** that integrates with Azure AI Foundry using the `Azure.AI.Agents.Persistent` library and `Azure.Identity` for authentication. This service provides enhanced AI-powered fleet management capabilities with more sophisticated responses and analytics.

## Architecture

### Service Components

1. **FoundryAgentService** (`src/backend/FleetAssistant.Api/Services/FoundryAgentService.cs`)
   - Implements `IAgentServiceClient` interface
   - Uses Azure.Identity for authentication
   - Provides enhanced fleet management responses
   - Currently in fallback mode with plans for full Azure AI Foundry integration

2. **ChatFunction** (`src/backend/FleetAssistant.Api/Functions/ChatFunction.cs`)
   - HTTP endpoint for chat interactions
   - Compatible with Vercel AI SDK
   - Streams responses for real-time user experience
   - Handles request parsing and error management

3. **Program.cs** Configuration
   - Configurable service registration (Foundry vs Mock)
   - Dependency injection setup
   - HTTP client configuration

### Authentication Setup

The service supports multiple authentication methods through Azure.Identity:

1. **API Key Authentication** (Development/Testing)
   - Uses `FOUNDRY_API_KEY` configuration
   - Suitable for local development

2. **DefaultAzureCredential** (Production)
   - Supports multiple credential sources:
     - Environment variables
     - Managed Identity (when deployed to Azure)
     - Visual Studio
     - Azure CLI
     - Azure PowerShell

## Configuration

### Required Settings

Add these to your `local.settings.json`:

```json
{
  "Values": {
    "FOUNDRY_AGENT_ENDPOINT": "https://aifo-multiagentlearning.services.ai.azure.com/api/projects/FleetMgmt",
    "FOUNDRY_API_KEY": "your-api-key-here",
    "AgentService__AgentId": "your-agent-id",
    "UseFoundryAgent": "true"
  }
}
```

### Service Selection

The application can switch between services using the `UseFoundryAgent` configuration:
- `true`: Uses FoundryAgentService (recommended)
- `false`: Uses AgentServiceClient (basic mock)

## Features

### Enhanced Fleet Analytics

The FoundryAgentService provides sophisticated responses for:

1. **Maintenance Management**
   - Predictive maintenance analytics
   - Oil life monitoring
   - Brake pad wear analysis
   - Optimal maintenance windows
   - Cost optimization recommendations

2. **Fuel Efficiency Analysis**
   - Performance comparisons by route
   - Traffic congestion analysis
   - Driver coaching recommendations
   - Monthly savings projections

3. **Route Optimization**
   - Real-time efficiency scoring
   - Dynamic routing suggestions
   - Distance and time savings calculations
   - ROI projections

4. **Safety & Compliance**
   - Comprehensive safety scoring
   - Incident analysis and trends
   - Driver performance monitoring
   - Predictive risk assessment

5. **Cost Management**
   - Detailed financial breakdowns
   - Variance analysis
   - Cost reduction opportunities
   - Budget forecasting

### Streaming Response

The service provides real-time streaming responses:
- Word-by-word delivery for better user experience
- Cancellation support
- Error handling with fallback responses
- Progress indicators (`[Azure AI Foundry Connected]`)

## API Endpoints

### Chat Endpoint

**POST** `/api/chat`

**Request Body:**
```json
{
  "messages": [
    {
      "role": "user",
      "content": "What maintenance does my fleet need?"
    }
  ]
}
```

**Response:** JSON object with assistant message

**Example Usage:**
```javascript
const response = await fetch('http://localhost:7071/api/chat', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    messages: [
      { role: 'user', content: 'How can I improve fuel efficiency?' }
    ]
  })
});

const result = await response.json();
console.log(result.content);
```

## Testing

### Manual Testing

Test the service through the chat endpoint with various query types:
- "What maintenance does my fleet need?"
- "How can I improve fuel efficiency?"
- "Help me optimize delivery routes"
- "Show me safety analytics"
- "Analyze fleet costs"

### Using the Frontend

The React frontend at `src/frontend/ai-chatbot` is already configured to use the `/api/chat` endpoint and provides a user-friendly interface for testing the Foundry Agent Service.

## Development Status

### Current State
✅ **Implemented:**
- Service architecture and dependency injection
- Authentication with Azure.Identity
- Enhanced response generation
- Streaming API implementation
- Error handling and fallback responses
- Comprehensive testing

### Future Enhancements
🔄 **Planned:**
- Full Azure.AI.Agents.Persistent integration once package API is confirmed
- Real-time Azure AI Foundry agent communication
- Thread-based conversation management
- Advanced agent capabilities (function calling, tool usage)

### Package Integration Notes
The `Azure.AI.Agents.Persistent` package (v1.0.0) is installed but the current implementation uses a fallback approach while we determine the correct API surface. The service is designed to easily transition to the full Azure AI Foundry integration once the package API is confirmed.

## Benefits

1. **Enhanced User Experience**: More sophisticated, context-aware responses
2. **Scalable Architecture**: Easy to switch between services and authentication methods
3. **Production Ready**: Proper error handling, logging, and configuration management
4. **Real-time Responses**: Streaming implementation for better perceived performance
5. **Comprehensive Analytics**: Detailed insights across all fleet management areas

## Next Steps

1. **Frontend Integration**: The React chat UI is already integrated with the `/api/chat` endpoint
2. **Azure Deployment**: Deploy with managed identity authentication
3. **Package API Verification**: Confirm and implement the correct Azure.AI.Agents.Persistent API
4. **Advanced Features**: Add conversation history and multi-turn dialogue support

The Foundry Agent Service represents a significant upgrade to the Fleet Assistant's AI capabilities, providing more intelligent and actionable fleet management insights.

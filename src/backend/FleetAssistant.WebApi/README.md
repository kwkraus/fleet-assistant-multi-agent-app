# Fleet Assistant WebAPI

This project is an ASP.NET Core WebAPI implementation that replaces the Azure Functions version for easier deployment and management.

## Migration from Azure Functions

This WebAPI maintains all the functionality of the original Azure Functions project while providing:

- **Swagger/OpenAPI Documentation**: Accessible at the root URL when running in development mode
- **Easier Deployment**: Standard ASP.NET Core deployment patterns
- **Better Testing**: Traditional controller-based testing approaches
- **Enhanced Development Experience**: Hot reload, better debugging, integrated tooling

## Features

- **Chat API**: Server-Sent Events streaming compatible with Vercel AI SDK
- **Azure AI Foundry Integration**: Uses the same `Azure.AI.Agents.Persistent` SDK
- **CORS Support**: Configured for frontend integration
- **Health Endpoint**: Basic health checking at `/api/chat/health`
- **Application Insights**: Integrated telemetry and monitoring

## Configuration

The application requires the following configuration values:

```json
{
  "FOUNDRY_AGENT_ENDPOINT": "https://your-foundry-endpoint.cognitiveservices.azure.com/",
  "UseFoundryAgent": true,
  "AgentService": {
    "AgentId": "your-agent-id"
  },
  "ApplicationInsights": {
    "ConnectionString": "your-application-insights-connection-string"
  }
}
```

## API Endpoints

### POST /api/chat
Sends a chat message and receives a streaming response.

**Request Body:**
```json
{
  "messages": [
    {
      "role": "user",
      "content": "Your message here"
    }
  ],
  "conversationId": "optional-conversation-id"
}
```

**Response:** Server-Sent Events stream with chat response chunks.

### OPTIONS /api/chat
Handles CORS preflight requests.

### GET /api/chat/health
Returns health status of the service.

## Running the Application

### Development
```bash
cd src/backend/FleetAssistant.WebApi
dotnet run
```

The application will start on:
- HTTPS: https://localhost:7074
- HTTP: http://localhost:5074
- Swagger UI: Available at the root URL in development mode

### Production
```bash
dotnet publish -c Release
```

## Dependencies

This project maintains the same Azure AI Foundry dependencies as the original Functions project:

- `Azure.AI.Agents.Persistent` - Azure AI Foundry agent integration
- `Azure.Identity` - Azure authentication
- `Microsoft.ApplicationInsights.AspNetCore` - Telemetry
- `Swashbuckle.AspNetCore` - Swagger/OpenAPI documentation

## Project References

- `FleetAssistant.Shared` - Shared models and interfaces
- `FleetAssistant.Infrastructure` - Infrastructure services (if needed)

## Authentication

The application uses `DefaultAzureCredential` for Azure authentication, supporting:
- Managed Identity (for Azure deployment)
- Environment variables
- Visual Studio/VS Code authentication (for local development)

## Deployment

This ASP.NET Core application can be deployed to:
- Azure App Service
- Azure Container Apps
- Azure Kubernetes Service
- Any standard web hosting platform supporting .NET 8

## Testing

The application maintains compatibility with the existing frontend and can be tested using the same test scripts in the root directory:
- `test-chat-endpoint.js`
- `test-streaming-chat.js`
- `test-foundry-agent.js`

# Fleet Assistant Multi-Agent App - AI Coding Instructions

## Architecture Overview

This is a **multi-agent fleet management system** with three main components:
- **Backend**: ASP.NET Core WebAPI (`src/backend/FleetAssistant.WebApi`) using Azure AI Foundry Agent Service via `Azure.AI.Agents.Persistent` SDK
- **Frontend**: Next.js 15 chat interface (`src/frontend/ai-chatbot`) with custom SSE streaming implementation
- **Shared**: Common models and interfaces (`src/backend/FleetAssistant.Shared`)

### Key Design Patterns

**Multi-Agent Coordination**: The system uses Azure AI Foundry Agent Service (`PersistentAgentsClient`) to interact with hosted agents. The backend maintains conversation-to-thread mapping using `ConcurrentDictionary<string, string>`.

**Streaming Chat Architecture**: Server-Sent Events (SSE) streaming from WebAPI to frontend. Backend uses `IAsyncEnumerable<string>` pattern with custom SSE event formatting. Frontend uses custom SSE reader (not Vercel AI SDK `useChat`).

**Repository Pattern**: All data access through `IRepository<T>` with Entity Framework Core, supporting both SQL Server and in-memory databases.

## Development Workflows

### Running the Application
```powershell
# Backend (from src/backend/FleetAssistant.WebApi)
dotnet run

# Frontend (from src/frontend/ai-chatbot)  
npm run dev

# Integration testing
.\testing\test-integration.ps1
```

### Key Configuration Requirements
- `FoundryAgentService:AgentEndpoint`: Azure AI Foundry service URL
- `FoundryAgentService:AgentId`: The hosted agent identifier
- `FoundryAgentService:RunPollingDelayMs`: Polling interval for run status (default: 100ms)
- `FoundryAgentService:StreamingDelayMs`: Delay between streaming chunks (default: 50ms)
- `DefaultAzureCredential` for Azure authentication (no API keys needed)
- `UseFoundryAgent`: Boolean flag to enable/disable Foundry Agent integration (default: true)

### Testing Patterns
Use the Node.js test scripts in `/testing/` directory:
- `test-webapi-chat.js`: Streaming chat endpoint verification
- `test-integration.ps1`: Full-stack integration test
- Test WebAPI on `http://localhost:5074/api/chat` (or port 7074 for HTTPS)
- Test frontend on `http://localhost:3000`

## Critical Implementation Details

### Agent Service Integration
The `FoundryAgentService` class manages conversation state using a `ConcurrentDictionary<string, string>` mapping conversation IDs to thread IDs. **Always maintain this mapping** for conversation continuity.

### Streaming Response Pattern
```csharp
// In controllers, use this pattern for SSE streaming:
Response.Headers.ContentType = "text/event-stream";
Response.Headers.CacheControl = "no-cache";
// Note: Connection header not needed for HTTP/2+

await WriteSSEEvent("metadata", new { conversationId, messageId, timestamp = DateTime.UtcNow }, cancellationToken);

await foreach (var chunk in _agentServiceClient.SendMessageStreamAsync(conversationId, message, cancellationToken))
{
    await WriteSSEEvent("chunk", new { content = chunk }, cancellationToken);
}

await WriteSSEEvent("done", new { messageId, timestamp = DateTime.UtcNow }, cancellationToken);
```

### Entity Framework Conventions
- Use `FleetAssistantDbContext` with both SQL Server and InMemory providers
- Repository pattern with `IRepository<T>` base interface
- Models in `FleetAssistant.Shared.Models` namespace
- DTOs in `FleetAssistant.Shared.DTOs` namespace

### Frontend Integration Points
- Custom SSE implementation with manual `ReadableStream` handling
- API endpoint: WebAPI `/api/chat` expecting `{ messages: ChatMessage[], conversationId?: string }`
- SSE event types: `metadata`, `chunk`, `done`, `error`
- Does NOT use Vercel AI SDK's `useChat` hook - uses custom streaming logic in `Chat.tsx`
- Tailwind CSS v4 with Radix UI components
- Next.js 15 with App Router and React 19

## Project-Specific Conventions

### Dependency Injection Structure
- `IAgentServiceClient` → `FoundryAgentService` (Azure AI integration)
- `IStorageService` → `BlobStorageService` (document storage)
- Repository pattern for all data entities
- Health checks for EF Core and Foundry Agent connectivity

### Error Handling Pattern
Use correlation IDs in all controllers:
```csharp
var correlationId = Guid.NewGuid().ToString();
_logger.LogInformation("Operation started. CorrelationId: {CorrelationId}", correlationId);
```

## Integration Points

**Azure Services**: Uses `DefaultAzureCredential` for seamless Azure integration. No connection strings needed for Azure AI Foundry.

**Database**: EF Core with code-first migrations. Uses in-memory database for development/testing when no connection string provided.

**Document Storage**: Azure Blob Storage integration via `BlobStorageOptions` configuration.

**Monitoring**: Application Insights integration with structured logging using correlation IDs throughout the request pipeline.

## Key Files to Reference

- `src/backend/FleetAssistant.WebApi/Controllers/ChatController.cs` - Streaming chat implementation
- `src/backend/FleetAssistant.WebApi/Services/FoundryAgentService.cs` - Azure AI Foundry integration
- `src/backend/FleetAssistant.WebApi/Program.cs` - DI container configuration
- `docs/MULTI_AGENT_INTEGRATION_GUIDE.md` - Architecture documentation
- `testing/test-integration.ps1` - Development workflow validation

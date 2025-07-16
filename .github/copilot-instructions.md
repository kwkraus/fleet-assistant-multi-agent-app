# Fleet Assistant Multi-Agent App - AI Coding Instructions

## Architecture Overview

This is a **multi-agent fleet management system** with three main components:
- **Backend**: ASP.NET Core WebAPI (`src/backend/FleetAssistant.WebApi`) using Azure AI Foundry Agent Service
- **Frontend**: Next.js chat interface (`src/frontend/ai-chatbot`) with Vercel AI SDK integration  
- **Shared**: Common models and interfaces (`src/backend/FleetAssistant.Shared`)

### Key Design Patterns

**Multi-Agent Coordination**: The system uses a Planning Agent that orchestrates specialized agents (FuelAgent, MaintenanceAgent, SafetyAgent) via Azure AI Foundry's `Azure.AI.Agents.Persistent` SDK.

**Streaming Chat Architecture**: Server-Sent Events (SSE) streaming from WebAPI to frontend using `IAsyncEnumerable<string>` pattern compatible with Vercel AI SDK.

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
- `FOUNDRY_AGENT_ENDPOINT`: Azure AI Foundry service URL
- `AgentService:AgentId`: The hosted agent identifier
- `DefaultAzureCredential` for Azure authentication (no API keys needed)

### Testing Patterns
Use the Node.js test scripts in `/testing/` directory:
- `test-webapi-chat.js`: Streaming chat endpoint verification
- `test-integration.ps1`: Full-stack integration test
- Test WebAPI on `http://localhost:5074/api/chat`
- Test frontend on `http://localhost:3000`

## Critical Implementation Details

### Agent Service Integration
The `FoundryAgentService` class manages conversation state using a `ConcurrentDictionary<string, string>` mapping conversation IDs to thread IDs. **Always maintain this mapping** for conversation continuity.

### Streaming Response Pattern
```csharp
// In controllers, use this pattern for SSE streaming:
Response.Headers.Add("Content-Type", "text/event-stream");
Response.Headers.Add("Cache-Control", "no-cache");
await foreach (var chunk in _agentServiceClient.SendMessageStreamAsync())
{
    await Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n");
}
```

### Entity Framework Conventions
- Use `FleetAssistantDbContext` with both SQL Server and InMemory providers
- Repository pattern with `IRepository<T>` base interface
- Models in `FleetAssistant.Shared.Models` namespace
- DTOs in `FleetAssistant.Shared.DTOs` namespace

### Frontend Integration Points
- Uses `@ai-sdk/react` with `useChat` hook
- API endpoint: `/api/chat` expecting `{ messages: ChatMessage[] }`
- Streaming responses auto-handled by Vercel AI SDK
- Tailwind CSS with Radix UI components

## Project-Specific Conventions

### Dependency Injection Structure
- `IAgentServiceClient` → `FoundryAgentService` (Azure AI integration)
- `IBlobStorageService` → `BlobStorageService` (document storage)
- Repository pattern for all data entities
- Health checks for EF Core and Foundry Agent connectivity

### Error Handling Pattern
Use correlation IDs in all controllers:
```csharp
var correlationId = Guid.NewGuid().ToString();
_logger.LogInformation("Operation started. CorrelationId: {CorrelationId}", correlationId);
```

### Multi-Tenant Architecture
The system supports multi-tenant configuration via API keys with tenant-specific agent integrations (GeoTab, Fleetio, Samsara plugins).

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

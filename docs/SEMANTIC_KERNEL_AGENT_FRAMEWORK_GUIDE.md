# Fleet Assistant - Semantic Kernel Agent Framework Implementation

## Overview

This Fleet Assistant application has been redesigned to use the new **Semantic Kernel Agent Framework** with the **Handoff Orchestration** pattern and **AzureAI Agents**. This modern architecture provides better scalability, maintainability, and conversational capabilities.

## Architecture

### Agent Framework Components

1. **AzureAI Agents**: Each specialized agent (Fuel, Maintenance, Safety, Planning, Triage) is implemented as an AzureAI Agent
2. **Handoff Orchestration**: Agents can seamlessly hand off conversations to other agents based on context
3. **Single Kernel Management**: All agents are managed under one Kernel for efficient resource utilization
4. **Plugin-Based Functionality**: Each agent has specialized plugins for their domain expertise

### Agent Hierarchy

```
TriageAgent (Main Coordinator)
├── FuelAgent (Fuel Management)
├── MaintenanceAgent (Vehicle Maintenance)
├── SafetyAgent (Safety & Compliance)
└── PlanningAgent (Route Planning & Coordination)
```

### Handoff Relationships

- **TriageAgent** → Can hand off to any specialist agent
- **FuelAgent** → Can hand back to Triage or to PlanningAgent for route optimization
- **MaintenanceAgent** → Can hand back to Triage or to SafetyAgent for safety issues
- **SafetyAgent** → Can hand back to Triage or to MaintenanceAgent for maintenance-related safety
- **PlanningAgent** → Can hand back to Triage or to FuelAgent/MaintenanceAgent for resource considerations

## Key Benefits

1. **Natural Conversations**: Agents can hand off mid-conversation maintaining context
2. **Specialized Expertise**: Each agent focuses on their domain with specific plugins
3. **Scalable Architecture**: Easy to add new agents or modify existing ones
4. **Azure AI Integration**: Leverages Azure AI Foundry for advanced capabilities
5. **Unified Management**: Single orchestration service manages all agents

## Configuration

### Required Environment Variables

```bash
AZURE_AI_FOUNDRY_ENDPOINT=https://your-foundry-endpoint.cognitiveservices.azure.com/
AZURE_AI_MODEL_DEPLOYMENT=gpt-4o
```

### Azure AI Foundry Setup

1. Create an Azure AI Foundry project
2. Deploy a GPT-4 model (or preferred model)
3. Configure authentication (using DefaultAzureCredential)
4. Update the endpoint in configuration

## API Endpoints

### Fleet Query (Main Endpoint)
- **POST** `/api/fleet/query`
- Processes fleet-related queries using agent orchestration
- Routes to appropriate specialists automatically

### Health Check
- **GET** `/api/health`
- Returns status of the orchestration service and all agents

## Usage Examples

### Example 1: Fuel Query
```json
{
  "message": "What are the current fuel levels for all vehicles?"
}
```

**Flow**: TriageAgent → FuelAgent (uses FuelManagementPlugin)

### Example 2: Complex Query
```json
{
  "message": "I need to plan a route that considers fuel efficiency and vehicle maintenance schedules"
}
```

**Flow**: TriageAgent → PlanningAgent → FuelAgent → MaintenanceAgent → PlanningAgent (final response)

### Example 3: Safety Incident
```json
{
  "message": "Report a safety incident involving vehicle V001 - minor collision"
}
```

**Flow**: TriageAgent → SafetyAgent (uses SafetyPlugin)

## Development Guidelines

### Adding New Agents

1. Create a new plugin class in `FleetAssistant.Agents.Plugins`
2. Create the agent in `FleetAgentOrchestrator.CreateAgentsAsync()`
3. Add handoff relationships in `SetupHandoffRelationships()`

### Adding New Capabilities

1. Add new `[KernelFunction]` methods to existing plugins
2. Update agent instructions to reference new capabilities
3. Test handoff scenarios

### Testing

- Unit tests for individual plugins
- Integration tests for agent orchestration
- End-to-end tests for complete workflows

## Monitoring and Observability

- Application Insights integration for telemetry
- Structured logging throughout the orchestration
- Health check endpoints for service monitoring
- Conversation history tracking

## Security Considerations

- Azure AD authentication for Azure AI services
- API key authentication for client requests
- Tenant isolation for multi-tenant scenarios
- Secure credential management

## Performance Optimization

- Agent initialization is done once during startup
- Connection pooling for Azure AI services
- Caching of frequent queries (future enhancement)
- Async/await throughout for non-blocking operations

## Future Enhancements

1. **Conversation Memory**: Persist conversation history across sessions
2. **Custom Tools**: Integration with external fleet management systems
3. **Voice Interface**: Add speech-to-text capabilities
4. **Analytics Dashboard**: Real-time insights into agent performance
5. **Advanced Routing**: ML-based query routing optimization

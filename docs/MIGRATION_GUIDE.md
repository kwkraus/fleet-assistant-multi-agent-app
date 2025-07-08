# Migration Guide: From Custom Agents to Semantic Kernel Agent Framework

## Overview

This guide explains how the Fleet Assistant application was migrated from a custom multi-agent implementation to the new **Semantic Kernel Agent Framework** with **Handoff Orchestration** and **AzureAI Agents**.

## Key Changes

### Before (Custom Implementation)
- **BaseAgent**: Abstract class with manual kernel management
- **PlanningAgent**: Central coordinator calling other agents directly
- **Individual Agents**: FuelAgent, MaintenanceAgent, SafetyAgent as separate classes
- **Manual Orchestration**: Planning agent manually decided which agents to call
- **Static Plugins**: Plugins registered once at startup

### After (Semantic Kernel Agent Framework)
- **AzureAI Agents**: Each agent is an AzureAI Agent with built-in conversation management
- **Handoff Orchestration**: Agents can hand off conversations to each other dynamically
- **Unified Orchestrator**: Single service manages all agents and handoffs
- **Dynamic Routing**: Agents decide handoffs based on conversation context
- **Plugin-per-Agent**: Each agent has domain-specific plugins

## Architecture Comparison

### Old Architecture
```
PlanningAgent
├── Calls FuelAgent directly
├── Calls MaintenanceAgent directly
├── Calls SafetyAgent directly
└── Aggregates responses manually
```

### New Architecture
```
FleetAgentOrchestrator
├── TriageAgent (entry point)
├── FuelAgent (with FuelManagementPlugin)
├── MaintenanceAgent (with MaintenancePlugin)
├── SafetyAgent (with SafetyPlugin)
└── PlanningAgent (with PlanningPlugin)

Handoff Relationships:
- TriageAgent → Any specialist
- Specialists → Back to Triage or to other specialists
- Context preserved across handoffs
```

## Code Changes

### 1. Removed Files
- `BaseAgent.cs` (replaced by AzureAI Agent base functionality)
- `FuelAgent.cs` (logic moved to FuelManagementPlugin)
- `MaintenanceAgent.cs` (logic moved to MaintenancePlugin)
- `SafetyAgent.cs` (logic moved to SafetyPlugin)
- Original `PlanningAgent.cs` (replaced by orchestration service)

### 2. New Files
- `FleetAgentOrchestrator.cs` - Main orchestration logic
- `FleetAgentOrchestrationService.cs` - Service wrapper
- `IFleetAgentOrchestrationService.cs` - Service interface
- `Plugins/FuelManagementPlugin.cs` - Fuel operations
- `Plugins/MaintenancePlugin.cs` - Maintenance operations
- `Plugins/SafetyPlugin.cs` - Safety operations
- `Plugins/PlanningPlugin.cs` - Planning operations

### 3. Updated Files
- `FleetQueryFunction.cs` - Now uses orchestration service
- `Program.cs` - Registers orchestration service instead of individual agents
- `local.settings.json` - Added Azure AI Foundry configuration

## Configuration Migration

### Old Configuration
```json
{
  "AZURE_OPENAI_ENDPOINT": "https://your-openai-endpoint.openai.azure.com/",
  "AZURE_OPENAI_API_KEY": "your-api-key",
  "AZURE_OPENAI_DEPLOYMENT_NAME": "gpt-4o"
}
```

### New Configuration
```json
{
  "AZURE_AI_FOUNDRY_ENDPOINT": "https://your-foundry-endpoint.cognitiveservices.azure.com/",
  "AZURE_AI_MODEL_DEPLOYMENT": "gpt-4o"
}
```

## NuGet Package Changes

### Added Packages
```xml
<PackageReference Include="Microsoft.SemanticKernel.Agents.AzureAI" Version="1.26.0-alpha" />
<PackageReference Include="Microsoft.SemanticKernel.Agents.Core" Version="1.26.0-alpha" />
<PackageReference Include="Azure.Identity" Version="1.12.1" />
```

## Behavioral Changes

### 1. Query Processing
**Before**: Planning agent analyzed intent and called specific agents
**After**: Triage agent routes to specialists, who can hand off to each other

### 2. Response Format
**Before**: Single aggregated response from planning agent
**After**: Conversation history with multiple agent interactions

### 3. Error Handling
**Before**: Try-catch in each agent with manual fallback
**After**: Built-in error handling with graceful degradation

### 4. Conversation Context
**Before**: Context lost between agent calls
**After**: Context preserved across handoffs

## Benefits of Migration

### 1. **Better User Experience**
- Natural conversations that flow between specialists
- Context preservation across agent handoffs
- More relevant responses from domain experts

### 2. **Improved Scalability**
- Easy to add new agents without changing existing code
- Built-in conversation management
- Optimized resource usage

### 3. **Enhanced Maintainability**
- Clear separation of concerns (one plugin per domain)
- Standardized agent interactions
- Centralized orchestration logic

### 4. **Advanced Capabilities**
- Leverage Azure AI Foundry advanced features
- Built-in conversation threading
- Automatic tool calling

## Testing Strategy

### 1. **Unit Tests**
- Individual plugin functionality
- Orchestration service methods
- Configuration validation

### 2. **Integration Tests**
- Agent handoff scenarios
- End-to-end conversation flows
- Error handling paths

### 3. **Performance Tests**
- Agent initialization time
- Query processing speed
- Memory usage optimization

## Rollback Plan

If rollback is needed:

1. **Revert NuGet packages** to previous versions
2. **Restore original agent classes** from version control
3. **Update Program.cs** to register original agents
4. **Revert configuration** to OpenAI direct connection
5. **Update FleetQueryFunction** to use PlanningAgent

## Monitoring and Alerts

### Key Metrics to Monitor
- Agent initialization success rate
- Query processing time
- Handoff success rate
- Error rates by agent type

### Recommended Alerts
- Orchestration service health check failures
- High error rates (>5%)
- Slow query processing (>30 seconds)
- Azure AI service quota exhaustion

## Next Steps

1. **Deploy to staging environment** for testing
2. **Run comprehensive integration tests**
3. **Monitor performance metrics**
4. **Gather user feedback** on conversation quality
5. **Optimize handoff relationships** based on usage patterns

## Support and Documentation

- See `SEMANTIC_KERNEL_AGENT_FRAMEWORK_GUIDE.md` for detailed usage
- Check Azure AI Foundry documentation for service setup
- Semantic Kernel Agent Framework samples on GitHub
- Internal documentation for API endpoints and configuration

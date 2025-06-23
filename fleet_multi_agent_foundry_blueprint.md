
# Fleet Multi-Agent Virtual Assistant — Step-by-Step Blueprint (Azure AI Foundry Edition)

This blueprint builds a multi-tenant fleet management AI assistant using **Azure AI Foundry** for LLM orchestration and **Semantic Kernel** for agent coordination. The system uses a single orchestrator that routes to specialized agents with tenant-specific integrations.

## Architecture Overview

- **Single Azure Function** with orchestrator + specialized agents
- **Planning Agent** coordinates all requests and calls sub-agents
- **Tenant-specific Integration Plugins** (GeoTab, Fleetio, Samsara, etc.)
- **Azure AI Foundry** for model routing and selection
- **OIDC/OAuth** authentication with role-based authorization
- **Graceful degradation** when integrations fail

---

## 0. Initialize Solution Structure

### 0.1. Create Projects & Shared Libraries
```text
- Create solution `FleetAssistant.sln` with projects:
    - `FleetAssistant.Api` (.NET Azure Functions v4 - Single Function)
    - `FleetAssistant.Agents` (class library for all agent implementations)
    - `FleetAssistant.Shared` (models, DTOs, utilities)
    - `FleetAssistant.Infrastructure` (plugins, credentials, config)
    - `Tests.FleetAssistant.Api` (XUnit test project)
    - `Tests.FleetAssistant.Agents` (XUnit test project)
```

---

## 1. Build Planning Agent & Core Infrastructure

### 1.1. Create Main HTTP Endpoint
```text
- In `FleetAssistant.Api`, add POST `/api/fleet/query`.
- Accepts: `{ message: string, conversationHistory?: object[], context?: object }`.
- JWT-based authentication with tenant validation.
- Returns: `{ response: string, agentData: object, warnings?: string[] }`.
```

### 1.2. Implement Authentication & Authorization
```text
- Add JWT middleware for OIDC/OAuth token validation.
- Extract user roles and authorized tenantIds from JWT claims.
- Create authorization service for tenant access validation.
- Test: Valid JWT allows access, invalid/expired JWT returns 401.
```

### 1.3. Implement Planning Agent with Azure AI Foundry
```text
- In `FleetAssistant.Agents`, create `PlanningAgent` class.
- Instantiate Semantic Kernel with Azure AI Foundry model router:
    - Use Azure AI Foundry SDK for model routing (no direct OpenAI calls).
    - Pass tenantId and user context to Foundry for model selection.
    - Create planning prompts to determine which sub-agents to call.
- Example flow:
    User: "What's the TCO for vehicle ABC123?"
    Planning Agent: Determines needs fuel + maintenance + insurance data
    Planning Agent: Calls FuelAgent, MaintenanceAgent, InsuranceAgent
    Planning Agent: Aggregates results with graceful degradation
- Test: Planning agent can parse user intent and route to appropriate sub-agents.
```

---

## 2. Build Integration Plugin System

### 2.1. Define Integration Plugin Interfaces
```text
- In `FleetAssistant.Infrastructure`:
    - `IIntegrationPluginBuilder` with:
        string Key { get; }
        Task<KernelPlugin?> BuildPluginAsync(string tenantId);
    - `IIntegrationConfigStore` for getting tenant-enabled integrations.
    - `ICredentialStore` for secure tenant credential retrieval.
    - `IntegrationPluginRegistry` that DI-injects all builders.
```

### 2.2. Implement Configuration & Credential Stores
```text
- `IntegrationConfigStore`:
    - `GetEnabledIntegrationsAsync(tenantId)` returns list of enabled integration keys.
    - Use `IDistributedCache` with 30min TTL for tenant configs.
    - Stub implementation returns hardcoded list for testing.
- `CredentialStore`:
    - `GetCredentialsAsync(tenantId, integrationKey)` returns auth details.
    - Stub implementation with fake credentials, later implement Key Vault.
- Test: Config store returns correct enabled integrations per tenant.
```

### 2.3. Create Stub Integration Plugin Builders
```text
- Add concrete plugin builders in `FleetAssistant.Infrastructure`:
    - `GeoTabPluginBuilder`, `FleetioPluginBuilder`, `SamsaraPluginBuilder`
    - Each implements `IIntegrationPluginBuilder` with unique Key.
    - BuildPluginAsync() creates Semantic Kernel plugin with dummy methods.
    - Example: GeoTabPlugin with `GetFuelDataAsync(vehicleId, startDate, endDate)`.
- Wire builders into `IntegrationPluginRegistry` via DI.
- Test: Registry returns correct builder by key; builder creates working plugin.
```

---

## 3. Build Specialized Agents

### 3.1. Create Base Agent Infrastructure
```text
- In `FleetAssistant.Agents`, create `BaseAgent` abstract class:
    - Protected method for creating Azure AI Foundry-enabled kernel per agent.
    - `RegisterIntegrationPluginsAsync(kernel, tenantId)` method.
    - Error handling for graceful degradation.
- Each agent gets its own kernel instance with relevant plugins only.
```

### 3.2. Implement Fuel Agent
```text
- Create `FuelAgent` inheriting from `BaseAgent`.
- `GetFuelAnalysisAsync(tenantId, vehicleId, timeframe, conversationContext)`.
- Agent creates its own kernel with Azure AI Foundry router.
- Dynamically loads fuel-related integration plugins based on tenant config.
- Returns structured fuel data or graceful error message.
- Test: Agent can process fuel queries with mock integrations.
```

### 3.3. Implement Additional Specialist Agents
```text
- Create `MaintenanceAgent`, `InsuranceAgent`, `TaxAgent` following same pattern.
- Each agent:
    - Has its own Azure AI Foundry-enabled kernel.
    - Loads only relevant integration plugins for its domain.
    - Handles errors gracefully (returns partial data + warnings).
    - Accepts conversation context from Planning Agent.
- Test: Each agent can process domain-specific queries independently.
```

### 3.4. Wire Agents into Planning Agent
```text
- Planning Agent orchestrates calls to specialist agents:
    - Analyzes user intent to determine which agents to call.
    - Passes relevant context (vehicleId, timeframe) to each agent.
    - Aggregates responses with graceful degradation.
    - Returns combined response with warnings for any failed agents.
- Integration test: End-to-end flow through planning agent to specialist agents.
```

---

## 4. Implement Real Integration Plugins

### 4.1. Build GeoTab Integration Plugin
```text
- Implement real GeoTab API client in `GeoTabPluginBuilder`.
- Methods: `GetFuelDataAsync`, `GetMaintenanceDataAsync`, `GetVehicleDataAsync`.
- Use tenant credentials from `ICredentialStore`.
- Implement session caching with `IDistributedCache`:
    - Cache GeoTab session tokens with TTL (6 hours).
    - Refresh sessions automatically on expiration.
- Handle API errors gracefully (return null, log warnings).
- Test: Plugin successfully authenticates and retrieves mock data from GeoTab.
```

### 4.2. Implement Additional Integration Plugins
```text
- Create `FleetioPluginBuilder`, `SamsaraPluginBuilder` following GeoTab pattern.
- Each plugin handles its own authentication, session management, and caching.
- Plugins pull their own detailed configuration on-demand (not from planning agent).
- Standardize error handling across all plugins.
- Test: Multiple plugins can operate simultaneously for multi-integration tenants.
```

---

## 5. Implement Advanced Features

### 5.1. Add Comprehensive Error Handling & Observability
```text
- Wrap all agent calls, plugin operations, and external API calls in try/catch.
- Use structured logging with tenantId, userId, agentType, integrationKey context.
- Add OpenTelemetry tracing for:
    - HTTP requests to the main endpoint
    - Planning agent orchestration
    - Individual agent execution
    - Integration plugin API calls
- Return HTTP 500 with traceId for unhandled exceptions.
- Test: Verify proper error boundaries and logging in failure scenarios.
```

### 5.2. Enhance Configuration Management
```text
- Implement production-ready `IntegrationConfigStore`:
    - Database-backed tenant configuration storage.
    - Admin API endpoints for managing tenant integrations.
    - Cache invalidation mechanisms for real-time updates.
- Implement production `CredentialStore` with Azure Key Vault.
- Add configuration validation and health checks.
- Test: Configuration changes propagate correctly to agents.
```

---

## 6. Testing Strategy

### 6.1. Unit Tests
```text
- Test all agents independently with mocked dependencies.
- Test plugin builders and integration factories.
- Test configuration stores with different tenant scenarios.
- Test authentication and authorization logic.
- Mock Azure AI Foundry and external integration APIs.
```

### 6.2. Integration Tests
```text
- End-to-end tests through planning agent to specialist agents.
- Test multi-tenant scenarios with different integration portfolios.
- Test graceful degradation when integrations fail.
- Test conversation context passing between planning agent and sub-agents.
- Test with real Azure AI Foundry integration (staging environment).
```

### 6.3. Load & Performance Tests
```text
- Test concurrent requests from multiple tenants.
- Verify caching effectiveness for configurations and sessions.
- Test Azure AI Foundry rate limiting and fallback behavior.
- Measure response times for complex multi-agent scenarios.
```

---

## 7. Production Readiness

### 7.1. Security Hardening
```text
- Implement comprehensive input validation and sanitization.
- Add rate limiting per tenant/user (deferred from initial implementation).
- Audit logging for all tenant data access.
- Encrypt all cached data (Redis encryption at rest).
- Regular security scanning of dependencies.
```

### 7.2. Monitoring & Alerting
```text
- Application Insights dashboards for key metrics:
    - Request volume and response times per tenant
    - Agent success/failure rates
    - Integration plugin health
    - Azure AI Foundry usage and costs
- Alerts for system health, error rates, and cost thresholds.
- Log analytics queries for troubleshooting tenant issues.
```

---

## 8. Infrastructure as Code & Cloud Provisioning

### 8.1. Azure Resource Provisioning
```text
- Create Bicep/Terraform templates for:
    - Azure Function App (single function, multiple agents)
    - Azure Cache for Redis (configuration, session, and integration caching)
    - Azure Key Vault (tenant credentials and secrets)
    - Application Insights (monitoring and telemetry)
    - Azure AI Foundry resources (model routing and management)
    - Azure SQL Database (tenant configuration and audit logs)
- Environment-specific parameter files (dev, staging, production).
```

### 8.2. Configuration Management
```text
- Environment-specific app settings for Azure Function.
- Connection strings for Redis, SQL, and Key Vault.
- Azure AI Foundry configuration (endpoints, keys, model preferences).
- Feature flags for gradual rollout of new integrations.
```

---

## 9. CI/CD Pipeline Implementation

### 9.1. GitHub Actions Workflow
```text
- Configure GitHub Actions pipeline:
    - On PR: restore, build, run unit tests, run integration tests
    - On merge to main: build artifact, deploy to staging, run E2E tests
    - On release: deploy to production with approval gates
- Include security scanning and dependency vulnerability checks.
- Automated rollback mechanisms for failed deployments.
```

### 9.2. Deployment Strategy
```text
- Blue-green deployment for zero-downtime updates.
- Database migration scripts with rollback procedures.
- Configuration drift detection and remediation.
- Post-deployment health checks and monitoring.
```

---

## 10. Best Practices & Architecture Decisions

### 10.1. Key Architectural Principles
- **Single Function Architecture**: Reduces cold starts, simplifies deployment
- **Orchestrator-First Design**: Planning agent coordinates all interactions
- **Per-Agent Kernels**: Clean separation with Azure AI Foundry integration
- **Hybrid Configuration**: Lightweight planning, detailed plugin-level config
- **Graceful Degradation**: System provides value even when integrations fail
- **Tenant Isolation**: Complete separation of data and configurations

### 10.2. Future Evolution Paths
- **Conversation Management**: Can evolve from UI-managed to server-side Redis storage
- **Rate Limiting**: Add tenant-specific quotas and cost controls in production
- **Plugin Granularity**: Split integration plugins by capability if needed
- **Multi-Region**: Expand to multiple Azure regions for global deployment
- **Event-Driven**: Add Service Bus for asynchronous plugin processing

---

## 11. Final Developer Onboarding & Execution

### 11.1. Development Workflow
```text
- Clone repository and set up local development environment.
- Configure local Azure AI Foundry connection and development credentials.
- Run solution locally with stub integrations for initial development.
- Follow incremental development approach:
    1. Get planning agent working with basic routing
    2. Add one specialist agent at a time
    3. Implement one integration plugin at a time
    4. Add advanced features (caching, error handling, monitoring)
- Comprehensive testing at each stage before moving to next component.
```

### 11.2. Production Deployment Checklist
```text
- All unit and integration tests passing
- Security review completed (authentication, authorization, data encryption)
- Performance testing completed with expected load
- Monitoring and alerting configured
- Disaster recovery procedures documented
- Tenant onboarding and integration configuration workflows tested
- Azure AI Foundry quotas and cost controls configured
- Documentation complete for operations team
```

---

## 12. Summary

This blueprint provides a **production-ready, multi-tenant fleet management AI assistant** that:

✅ **Starts with the orchestrator** for logical architecture development  
✅ **Uses single Azure Function** for simplified deployment and reduced cold starts  
✅ **Implements per-agent kernels** with Azure AI Foundry for clean separation  
✅ **Provides tenant-specific integrations** through dynamic plugin system  
✅ **Handles failures gracefully** with partial results and clear error messages  
✅ **Supports enterprise authentication** with OIDC/OAuth and role-based access  
✅ **Maintains conversation context** through UI-managed state (evolvable to server-side)  
✅ **Follows production best practices** for security, monitoring, and scalability  

**Key Benefits:**
- **Extensible**: Add new integrations without core code changes
- **Scalable**: Multi-tenant with proper isolation and caching
- **Resilient**: Graceful degradation when external services fail
- **Maintainable**: Clear separation of concerns and comprehensive testing
- **Enterprise-Ready**: Authentication, authorization, monitoring, and audit trails

**Next Steps:** Follow the blueprint incrementally, starting with the planning agent and building out specialist agents one at a time. The architecture supports continuous evolution and feature additions without major refactoring.


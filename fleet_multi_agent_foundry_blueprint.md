
# Fleet Assistant Agent Service Integration — Step-by-Step Blueprint

This blueprint builds a simple fleet management AI assistant that connects to hosted agents in **Azure AI Foundry Agent Service**. The system uses **DefaultAzureCredential** for Entra ID authentication and streams responses back to the caller.

## Architecture Overview

- **Single Azure Function** that connects to hosted agents
- **Azure AI Foundry Agent Service** hosts the fleet management agent
- **Entra ID authentication** via DefaultAzureCredential
- **Streaming responses** from agent service to caller
- **Simple message forwarding** with no local processing

---

## 0. Initialize Solution Structure

### 0.1. Create Projects & Shared Libraries
```text
- Create solution `FleetAssistant.sln` with projects:
    - `FleetAssistant.Api` (.NET Azure Functions v4 - Single Function)
    - `FleetAssistant.Shared` (models, DTOs, utilities)
    - `Tests.FleetAssistant.Api` (XUnit test project)
```

---

## 1. Build Agent Service Integration

### 1.1. Create Main HTTP Endpoint
```text
- In `FleetAssistant.Api`, add POST `/api/fleet/query`.
- Accepts: `{ message: string, conversationId?: string, context?: object }`.
- No authentication required for initial implementation.
- Returns: Streaming response from Azure AI Foundry Agent Service.
```

### 1.2. Implement Azure AI Foundry Agent Service Client
```text
- Add Azure AI Foundry SDK NuGet package.
- Configure DefaultAzureCredential for Entra ID authentication.
- Create client to connect to hosted agent by Agent ID.
- Handle streaming responses from agent service.
- Forward responses directly to HTTP response stream.
```

### 1.3. Configure Agent Connection
```text
- Store Agent Service endpoint and Agent ID in function app settings.
- Use DefaultAzureCredential for authentication to Agent Service.
- Configure managed identity for the Azure Function.
- Test: Function can successfully connect to and communicate with hosted agent.
```

---

## 2. Implement Response Streaming

### 2.1. Configure Streaming Response
```text
- Implement Server-Sent Events (SSE) or WebSocket streaming.
- Forward agent service streaming responses to client in real-time.
- Handle connection timeouts and interruptions gracefully.
- Implement proper error handling for streaming scenarios.
```

### 2.2. Add Response Processing
```text
- Minimal response processing - primarily pass-through.
- Add basic logging for request/response tracking.
- Implement request/response correlation IDs.
- Test: Streaming responses work correctly for various message types.
```

---

## 3. Error Handling & Monitoring

### 3.1. Implement Error Handling
```text
- Handle Azure AI Foundry Agent Service connectivity issues.
- Implement retry logic with exponential backoff.
- Handle authentication failures and token refresh.
- Return appropriate HTTP status codes for different error scenarios.
```

### 3.2. Add Basic Monitoring
```text
- Application Insights integration for request tracking.
- Log agent service calls and response times.
- Monitor authentication success/failure rates.
- Track streaming response performance metrics.
```

---

## 4. Testing Strategy

### 4.1. Unit Tests
```text
- Test Azure AI Foundry Agent Service client with mocked responses.
- Test streaming response handling and error scenarios.
- Test authentication and credential management.
- Mock agent service connections for isolated testing.
```

### 4.2. Integration Tests
```text
- End-to-end tests with real Azure AI Foundry Agent Service.
- Test streaming responses with various message types and lengths.
- Test authentication with DefaultAzureCredential in Azure environment.
- Test error handling when agent service is unavailable.
```

### 4.3. Performance Tests
```text
- Test concurrent requests to agent service.
- Measure streaming response latency and throughput.
- Test authentication token refresh under load.
- Verify memory usage during long streaming responses.
```

---

## 5. Production Readiness

### 5.1. Security Configuration
```text
- Configure managed identity for Azure Function.
- Grant appropriate permissions to Azure AI Foundry Agent Service.
- Implement input validation and sanitization.
- Enable HTTPS-only communication.
- Configure CORS for frontend integration.
```

### 5.2. Monitoring & Alerting
```text
- Application Insights dashboards for key metrics:
    - Request volume and response times
    - Agent service connectivity and success rates
    - Authentication success/failure rates
    - Streaming response performance
- Alerts for system health and error rates.
- Log analytics for troubleshooting issues.
```

---

## 6. Infrastructure as Code & Cloud Provisioning

### 6.1. Azure Resource Provisioning
```text
- Create Bicep/Terraform templates for:
    - Azure Function App (single function for agent service integration)
    - Azure AI Foundry workspace and agent service
    - Application Insights (monitoring and telemetry)
    - Managed Identity configuration
- Environment-specific parameter files (dev, staging, production).
```

### 6.2. Configuration Management
```text
- Environment-specific app settings for Azure Function.
- Azure AI Foundry Agent Service endpoint and agent ID configuration.
- Managed identity permissions for agent service access.
- Application Insights connection strings.
```

---

## 7. CI/CD Pipeline Implementation

### 7.1. GitHub Actions Workflow
```text
- Configure GitHub Actions pipeline:
    - On PR: restore, build, run unit tests, run integration tests
    - On merge to main: build artifact, deploy to staging, run E2E tests
    - On release: deploy to production with approval gates
- Include security scanning and dependency vulnerability checks.
- Automated rollback mechanisms for failed deployments.
```

### 7.2. Deployment Strategy
```text
- Blue-green deployment for zero-downtime updates.
- Infrastructure deployment with Bicep/Terraform.
- Post-deployment health checks and monitoring.
- Configuration validation in each environment.
```

---

## 8. Best Practices & Architecture Decisions

### 8.1. Key Architectural Principles
- **Simple Proxy Architecture**: Minimal processing, focus on reliable message forwarding
- **Hosted Agent Integration**: Leverage Azure AI Foundry Agent Service capabilities
- **Entra ID Authentication**: Secure, enterprise-ready authentication via managed identity
- **Streaming First**: Real-time response streaming for better user experience
- **Cloud-Native**: Fully leverages Azure platform services

### 8.2. Future Evolution Paths
- **Multi-Tenancy**: Add tenant isolation and configuration when needed
- **Rate Limiting**: Implement per-user quotas and cost controls
- **Caching**: Add response caching for frequently asked questions
- **Multiple Agents**: Connect to different agents based on query type
- **Advanced Routing**: Implement intelligent agent selection logic

---

## 9. Final Developer Onboarding & Execution

### 9.1. Development Workflow
```text
- Clone repository and set up local development environment.
- Configure local Azure credentials for development (Azure CLI login).
- Set up Azure AI Foundry workspace and deploy fleet management agent.
- Configure function app settings with agent service details.
- Run solution locally with Azure Function Core Tools.
- Test with hosted agent service connection.
```

### 9.2. Production Deployment Checklist
```text
- All unit and integration tests passing
- Managed identity configured and permissions granted
- Azure AI Foundry Agent Service deployed and accessible
- Monitoring and alerting configured
- Performance testing completed with expected load
- Documentation complete for operations team
```

---

## 10. Summary

This blueprint provides a **simple, production-ready fleet assistant** that:

✅ **Connects to Azure AI Foundry Agent Service** for hosted agent capabilities  
✅ **Uses single Azure Function** for simplified deployment and minimal overhead  
✅ **Implements Entra ID authentication** via DefaultAzureCredential and managed identity  
✅ **Streams responses in real-time** for better user experience  
✅ **Focuses on reliable message forwarding** with minimal local processing  
✅ **Follows Azure best practices** for authentication, monitoring, and security  

**Key Benefits:**
- **Simple**: Minimal complexity, easy to understand and maintain
- **Secure**: Enterprise-ready authentication via Entra ID
- **Scalable**: Leverages Azure AI Foundry Agent Service scalability
- **Performant**: Streaming responses with low latency
- **Cloud-Native**: Fully leverages Azure platform capabilities

**Next Steps:** 
1. Set up Azure AI Foundry workspace and deploy fleet management agent
2. Create Azure Function with agent service integration
3. Configure managed identity and permissions
4. Implement streaming response handling
5. Add monitoring and error handling
6. Deploy and test end-to-end functionality

The architecture supports future enhancements like multi-tenancy, advanced routing, and additional agents without major refactoring.


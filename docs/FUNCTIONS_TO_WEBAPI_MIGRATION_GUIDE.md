# Migration Guide: Azure Functions to ASP.NET Core WebAPI

## Overview

This guide helps you migrate from the Azure Functions implementation (`FleetAssistant.Api`) to the new ASP.NET Core WebAPI implementation (`FleetAssistant.WebApi`).

## Key Differences

### Project Structure
- **Functions**: Uses `Microsoft.Azure.Functions.Worker` with `[Function]` attributes
- **WebAPI**: Uses `Microsoft.AspNetCore.Mvc` with `[ApiController]` and action methods

### Configuration
- **Functions**: Uses `host.json` and `local.settings.json`
- **WebAPI**: Uses `appsettings.json` and `appsettings.Development.json`

### Hosting
- **Functions**: Serverless, event-driven execution
- **WebAPI**: Always-on web server with standard HTTP hosting

## Migration Steps

### 1. Configuration Migration

Copy your configuration values from the Functions project:

**From:** `FleetAssistant.Api/local.settings.json`
```json
{
  "Values": {
    "FOUNDRY_AGENT_ENDPOINT": "your-endpoint",
    "AgentService__AgentId": "your-agent-id"
  }
}
```

**To:** `FleetAssistant.WebApi/appsettings.json`
```json
{
  "FOUNDRY_AGENT_ENDPOINT": "your-endpoint",
  "AgentService": {
    "AgentId": "your-agent-id"
  }
}
```

### 2. Frontend Integration

Update your frontend to point to the new WebAPI endpoints:

**Old Functions URL:** `https://your-function-app.azurewebsites.net/api/Chat`
**New WebAPI URL:** `https://your-webapp.azurewebsites.net/api/chat`

The API contract remains the same - no changes needed to request/response formats.

### 3. Testing

Use the existing test scripts with updated URLs:

```javascript
// Update test-chat-endpoint.js
const baseUrl = 'http://localhost:5074'; // or https://localhost:7074
```

### 4. Deployment Options

#### Option A: Azure App Service (Recommended)
```bash
# Build and publish
dotnet publish -c Release

# Deploy using Azure CLI
az webapp deploy --resource-group <rg-name> --name <app-name> --src-path ./bin/Release/net8.0/publish
```

#### Option B: Azure Container Apps
```bash
# Create Dockerfile (if needed)
# Build container image
# Deploy to Container Apps
```

#### Option C: Azure Kubernetes Service
```bash
# Create Kubernetes manifests
# Deploy to AKS cluster
```

## Benefits of Migration

### Development Benefits
- **Swagger/OpenAPI**: Automatic API documentation and testing interface
- **Hot Reload**: Faster development iteration
- **Standard Debugging**: Traditional breakpoint debugging
- **Better Testing**: Controller-based unit testing

### Operational Benefits
- **Predictable Scaling**: Standard web app scaling vs. serverless cold starts
- **Easier Monitoring**: Standard ASP.NET Core monitoring and logging
- **Deployment Flexibility**: Multiple hosting options
- **Cost Predictability**: Fixed hosting costs vs. consumption-based

### Performance Benefits
- **No Cold Starts**: Always-warm instances
- **Connection Pooling**: Better resource utilization
- **Request Pipeline**: Optimized for web API scenarios

## Rollback Plan

If you need to rollback to the Functions implementation:

1. Keep the Functions project (`FleetAssistant.Api`) in the solution
2. Update frontend URLs back to Functions endpoints
3. Redeploy the Functions app if needed

## Side-by-Side Testing

You can run both implementations simultaneously:

1. **Functions**: Use port 7071 (default Functions port)
2. **WebAPI**: Use port 5074/7074 (configured ports)
3. **Frontend**: Switch between endpoints for testing

## Cleanup After Migration

Once you've verified the WebAPI works correctly:

1. **Keep for Reference**: Don't delete the Functions project immediately
2. **Update CI/CD**: Modify deployment pipelines to build/deploy WebAPI
3. **Update Documentation**: Update API documentation and guides
4. **Remove Functions Resources**: Decommission Azure Functions resources after successful migration

## Troubleshooting

### Common Issues

1. **Configuration**: Ensure all config values are properly migrated
2. **CORS**: WebAPI has different CORS configuration than Functions
3. **Routing**: URL paths may differ slightly (`/api/Chat` vs `/api/chat`)
4. **Authentication**: Azure Identity behavior may differ between hosting models

### Debugging

1. Check Application Insights logs for both implementations
2. Use Swagger UI for API testing: `https://localhost:7074/swagger`
3. Compare response formats between implementations
4. Verify Azure AI Foundry connectivity

## Next Steps

1. **Test thoroughly**: Validate all functionality works as expected
2. **Performance testing**: Compare performance characteristics
3. **Monitoring setup**: Configure monitoring for the new WebAPI
4. **Documentation updates**: Update README and API documentation
5. **Team training**: Ensure team understands new deployment model

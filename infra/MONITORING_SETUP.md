# Monitoring and Alerting Setup - Fleet Assistant

This guide provides comprehensive monitoring and alerting configuration for the Fleet Assistant multi-agent application.

## Table of Contents

- [Architecture](#architecture)
- [Application Insights Configuration](#application-insights-configuration)
- [Log Analytics Workspace](#log-analytics-workspace)
- [Custom Metrics and Telemetry](#custom-metrics-and-telemetry)
- [Alerts and Notifications](#alerts-and-notifications)
- [Dashboards](#dashboards)
- [Performance Monitoring](#performance-monitoring)
- [Troubleshooting](#troubleshooting)

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Application Components                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐    │
│  │ Static Web   │    │ App Service  │    │  AI Foundry  │    │
│  │ App          │    │ (Backend)    │    │  Agents      │    │
│  │ (Frontend)   │    │              │    │              │    │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘    │
│         │                   │                   │              │
│         │ Telemetry         │ Telemetry         │ Logs         │
│         └───────────────────┴───────────────────┘              │
│                             │                                  │
└─────────────────────────────┼──────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Application Insights                          │
│  • Request tracking                                              │
│  • Dependency tracking (SQL, Storage, AI Foundry)               │
│  • Exception logging                                             │
│  • Custom events and metrics                                     │
│  • Distributed tracing                                           │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Log Analytics Workspace                         │
│  • Centralized log storage                                       │
│  • Query language (KQL)                                          │
│  • Retention policies                                            │
│  • Cross-resource queries                                        │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Azure Monitor Alerts                          │
│  • Metric alerts                                                 │
│  • Log query alerts                                              │
│  • Action groups                                                 │
│  • Notification channels                                         │
└─────────────────────────────────────────────────────────────────┘
```

## Application Insights Configuration

### Connection String

The Application Insights connection string is automatically configured during deployment:

```bash
# Retrieve connection string
az monitor app-insights component show \
  --resource-group fleet-rg-prod \
  --app fleet-appinsights-prod \
  --query connectionString -o tsv
```

### Backend Integration

Application Insights is already integrated in the backend via configuration:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=xxx;LiveEndpoint=xxx"
  }
}
```

In `Program.cs`:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Frontend Integration (Static Web App)

For Next.js frontend monitoring:

1. Install Application Insights SDK:
   ```bash
   npm install @microsoft/applicationinsights-web
   ```

2. Initialize in `_app.tsx`:
   ```typescript
   import { ApplicationInsights } from '@microsoft/applicationinsights-web'
   
   const appInsights = new ApplicationInsights({
     config: {
       connectionString: process.env.NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING
     }
   })
   appInsights.loadAppInsights()
   appInsights.trackPageView()
   ```

## Log Analytics Workspace

### Workspace Configuration

The Log Analytics workspace is deployed with:
- **SKU**: PerGB2018 (pay-as-you-go)
- **Retention**: 90 days (configurable)
- **Daily cap**: Unlimited (set limits for cost control)

### Set Daily Cap

```bash
az monitor log-analytics workspace update \
  --resource-group fleet-rg-prod \
  --workspace-name fleet-logs-prod \
  --quota 5  # 5 GB per day
```

### Enable Diagnostic Settings

Enable diagnostic logs for all resources:

```bash
# App Service
az monitor diagnostic-settings create \
  --name AppServiceDiagnostics \
  --resource /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Web/sites/fleet-api-prod \
  --workspace fleet-logs-prod \
  --logs '[{"category": "AppServiceHTTPLogs", "enabled": true}, {"category": "AppServiceConsoleLogs", "enabled": true}]' \
  --metrics '[{"category": "AllMetrics", "enabled": true}]'

# SQL Database
az monitor diagnostic-settings create \
  --name SqlDiagnostics \
  --resource /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Sql/servers/fleet-sql-prod/databases/fleetdbprod \
  --workspace fleet-logs-prod \
  --logs '[{"category": "SQLInsights", "enabled": true}, {"category": "QueryStoreRuntimeStatistics", "enabled": true}]' \
  --metrics '[{"category": "AllMetrics", "enabled": true}]'

# Storage Account
az monitor diagnostic-settings create \
  --name StorageDiagnostics \
  --resource /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Storage/storageAccounts/fleetstprod \
  --workspace fleet-logs-prod \
  --logs '[{"category": "StorageRead", "enabled": true}, {"category": "StorageWrite", "enabled": true}]' \
  --metrics '[{"category": "Transaction", "enabled": true}]'
```

## Custom Metrics and Telemetry

### Foundry Agent Metrics

Track AI Foundry agent performance:

```csharp
// In FoundryAgentService.cs
private readonly TelemetryClient _telemetryClient;

public async IAsyncEnumerable<string> SendMessageStreamAsync(
    string conversationId,
    string message,
    CancellationToken cancellationToken = default)
{
    var startTime = DateTime.UtcNow;
    var properties = new Dictionary<string, string>
    {
        { "ConversationId", conversationId },
        { "MessageLength", message.Length.ToString() }
    };

    try
    {
        // Send message and stream response
        await foreach (var chunk in SendMessageToAgentAsync(conversationId, message, cancellationToken))
        {
            yield return chunk;
        }

        // Track success
        var duration = DateTime.UtcNow - startTime;
        _telemetryClient.TrackMetric("FoundryAgentLatency", duration.TotalMilliseconds, properties);
        _telemetryClient.TrackEvent("FoundryAgentSuccess", properties);
    }
    catch (Exception ex)
    {
        // Track failure
        properties.Add("ErrorMessage", ex.Message);
        _telemetryClient.TrackException(ex, properties);
        _telemetryClient.TrackEvent("FoundryAgentFailure", properties);
        throw;
    }
}
```

### SSE Streaming Metrics

Track Server-Sent Events performance:

```csharp
// In ChatController.cs
[HttpPost]
public async Task Chat([FromBody] ChatRequest request)
{
    var correlationId = Guid.NewGuid().ToString();
    var startTime = DateTime.UtcNow;
    var chunkCount = 0;

    try
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        await foreach (var chunk in _agentServiceClient.SendMessageStreamAsync(
            request.ConversationId, request.Message, HttpContext.RequestAborted))
        {
            chunkCount++;
            await WriteSSEEvent("chunk", new { content = chunk }, HttpContext.RequestAborted);
        }

        // Track metrics
        var duration = DateTime.UtcNow - startTime;
        _telemetryClient.TrackMetric("SSEStreamDuration", duration.TotalMilliseconds);
        _telemetryClient.TrackMetric("SSEChunkCount", chunkCount);
        _telemetryClient.TrackEvent("SSEStreamCompleted", new Dictionary<string, string>
        {
            { "CorrelationId", correlationId },
            { "ChunkCount", chunkCount.ToString() },
            { "DurationMs", duration.TotalMilliseconds.ToString() }
        });
    }
    catch (Exception ex)
    {
        _telemetryClient.TrackException(ex);
        throw;
    }
}
```

### Database Performance Tracking

```csharp
// Track EF Core query performance
public class PerformanceInterceptor : DbCommandInterceptor
{
    private readonly TelemetryClient _telemetryClient;

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        _telemetryClient.TrackMetric("DatabaseQueryDuration", eventData.Duration.TotalMilliseconds);
        _telemetryClient.TrackDependency("SQL Database", command.CommandText, 
            DateTimeOffset.UtcNow.AddMilliseconds(-eventData.Duration.TotalMilliseconds),
            eventData.Duration,
            eventData.IsAsync);

        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
```

## Alerts and Notifications

### Create Action Group

Set up notification channels:

```bash
az monitor action-group create \
  --resource-group fleet-rg-prod \
  --name FleetOpsTeam \
  --short-name FleetOps \
  --email-receiver Name=OpsTeam Email=ops@example.com \
  --sms-receiver Name=OnCall CountryCode=1 PhoneNumber=5551234567 \
  --webhook-receiver Name=Slack ServiceUri=https://hooks.slack.com/services/YOUR/WEBHOOK/URL
```

### High CPU Alert

```bash
az monitor metrics alert create \
  --resource-group fleet-rg-prod \
  --name HighCPUAlert \
  --scopes /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Web/sites/fleet-api-prod \
  --condition "avg Percentage CPU > 85" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action FleetOpsTeam \
  --severity 2 \
  --description "App Service CPU usage is above 85%"
```

### High Memory Alert

```bash
az monitor metrics alert create \
  --resource-group fleet-rg-prod \
  --name HighMemoryAlert \
  --scopes /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Web/sites/fleet-api-prod \
  --condition "avg MemoryPercentage > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action FleetOpsTeam \
  --severity 2
```

### Foundry Agent Failure Alert

```bash
az monitor metrics alert create \
  --resource-group fleet-rg-prod \
  --name FoundryAgentFailureAlert \
  --scopes /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/microsoft.insights/components/fleet-appinsights-prod \
  --condition "count customEvents/count > 5 where customDimensions.EventName == 'FoundryAgentFailure'" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action FleetOpsTeam \
  --severity 1
```

### SQL Database High DTU Alert

```bash
az monitor metrics alert create \
  --resource-group fleet-rg-prod \
  --name HighDTUAlert \
  --scopes /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Sql/servers/fleet-sql-prod/databases/fleetdbprod \
  --condition "avg dtu_consumption_percent > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action FleetOpsTeam \
  --severity 2
```

### HTTP 5xx Error Alert

```bash
az monitor metrics alert create \
  --resource-group fleet-rg-prod \
  --name Http5xxAlert \
  --scopes /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Web/sites/fleet-api-prod \
  --condition "total Http5xx > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action FleetOpsTeam \
  --severity 1
```

### Log Query Alerts

Create alerts based on Log Analytics queries:

```bash
# Failed login attempts
az monitor scheduled-query create \
  --resource-group fleet-rg-prod \
  --name FailedLoginAlert \
  --scopes /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/microsoft.insights/components/fleet-appinsights-prod \
  --condition "count > 5" \
  --condition-query "requests | where resultCode == '401' | summarize count() by bin(timestamp, 5m)" \
  --window-size 5m \
  --evaluation-frequency 5m \
  --action FleetOpsTeam \
  --severity 2
```

## Dashboards

### Create Azure Dashboard

1. Navigate to Azure Portal > Dashboard
2. Click "New dashboard" > "Blank dashboard"
3. Add tiles:

#### Key Metrics Tile

```json
{
  "type": "Extension/HubsExtension/PartType/MonitorChartPart",
  "settings": {
    "content": {
      "options": {
        "chart": {
          "metrics": [
            {
              "resourceMetadata": {
                "id": "/subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Web/sites/fleet-api-prod"
              },
              "name": "Requests",
              "aggregationType": "Sum"
            },
            {
              "resourceMetadata": {
                "id": "/subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Web/sites/fleet-api-prod"
              },
              "name": "Http5xx",
              "aggregationType": "Sum"
            }
          ],
          "title": "App Service Requests"
        }
      }
    }
  }
}
```

### Kusto Queries for Dashboards

#### Top 10 Slowest Requests

```kusto
requests
| where timestamp > ago(1h)
| summarize avg(duration) by name
| top 10 by avg_duration desc
| render barchart
```

#### Foundry Agent Success Rate

```kusto
customEvents
| where name in ("FoundryAgentSuccess", "FoundryAgentFailure")
| summarize Total = count(), Failures = countif(name == "FoundryAgentFailure") by bin(timestamp, 5m)
| extend SuccessRate = (Total - Failures) * 100.0 / Total
| render timechart
```

#### Database Query Performance

```kusto
dependencies
| where type == "SQL"
| summarize avg(duration), percentile(duration, 95), percentile(duration, 99) by bin(timestamp, 5m)
| render timechart
```

#### SSE Connection Metrics

```kusto
customEvents
| where name == "SSEStreamCompleted"
| extend ChunkCount = toint(customDimensions.ChunkCount)
| extend DurationMs = toint(customDimensions.DurationMs)
| summarize avg(ChunkCount), avg(DurationMs) by bin(timestamp, 5m)
| render timechart
```

## Performance Monitoring

### Application Insights Live Metrics

Access real-time metrics:

1. Navigate to Application Insights resource
2. Click "Live Metrics" in the left menu
3. Monitor:
   - Incoming requests per second
   - Request duration
   - Dependency calls
   - Exceptions per second
   - Server metrics (CPU, memory)

### Performance Testing

Use Azure Load Testing to simulate traffic:

```bash
# Create load testing resource
az load create \
  --resource-group fleet-rg-prod \
  --name fleet-loadtest-prod \
  --location eastus

# Run load test
az load test create \
  --resource-group fleet-rg-prod \
  --load-test-resource fleet-loadtest-prod \
  --test-id chat-endpoint-test \
  --display-name "Chat Endpoint Load Test" \
  --test-plan chat-test.jmx \
  --engine-instances 1
```

### Application Map

View dependencies and performance:

1. Navigate to Application Insights > Application Map
2. Review:
   - Component dependencies
   - Call volumes
   - Response times
   - Failure rates

## Troubleshooting

### Common Queries

#### Find exceptions

```kusto
exceptions
| where timestamp > ago(1h)
| summarize count() by type, outerMessage
| order by count_ desc
```

#### Slow database queries

```kusto
dependencies
| where type == "SQL" and duration > 1000
| order by timestamp desc
| project timestamp, name, duration, success
```

#### Failed Foundry Agent calls

```kusto
customEvents
| where name == "FoundryAgentFailure"
| extend ErrorMessage = tostring(customDimensions.ErrorMessage)
| project timestamp, ErrorMessage, customDimensions
```

#### HTTP error rates

```kusto
requests
| where timestamp > ago(1h)
| summarize Total = count(), Errors = countif(resultCode >= 400) by bin(timestamp, 5m)
| extend ErrorRate = Errors * 100.0 / Total
| render timechart
```

### Export Logs

```bash
# Export Application Insights logs
az monitor app-insights query \
  --app fleet-appinsights-prod \
  --resource-group fleet-rg-prod \
  --analytics-query "requests | where timestamp > ago(1h)" \
  --offset 1h \
  --output json > requests.json
```

## Cost Optimization

### Log Analytics Cost Management

1. **Set data cap**:
   ```bash
   az monitor log-analytics workspace update \
     --resource-group fleet-rg-prod \
     --workspace-name fleet-logs-prod \
     --quota 5  # 5 GB per day
   ```

2. **Adjust retention**:
   ```bash
   az monitor log-analytics workspace update \
     --resource-group fleet-rg-prod \
     --workspace-name fleet-logs-prod \
     --retention-time 30  # 30 days instead of 90
   ```

3. **Filter telemetry** (in code):
   ```csharp
   services.AddApplicationInsightsTelemetryProcessor<FilterTelemetryProcessor>();
   ```

### Application Insights Sampling

Configure adaptive sampling to reduce costs:

```json
{
  "ApplicationInsights": {
    "EnableAdaptiveSampling": true,
    "SamplingPercentage": 10
  }
}
```

## Best Practices

✅ Enable Application Insights for all tiers (frontend, backend)
✅ Use correlation IDs for distributed tracing
✅ Set up alert action groups with multiple channels
✅ Create dashboards for different audiences (ops, dev, business)
✅ Regularly review and update alert thresholds
✅ Use Log Analytics workbooks for deep-dive analysis
✅ Set data retention policies based on compliance requirements
✅ Enable diagnostic settings for all Azure resources
✅ Monitor costs and set spending alerts

## References

- [Application Insights Documentation](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Log Analytics Query Language](https://docs.microsoft.com/azure/azure-monitor/logs/log-query-overview)
- [Azure Monitor Alerts](https://docs.microsoft.com/azure/azure-monitor/alerts/alerts-overview)
- [Kusto Query Language (KQL)](https://docs.microsoft.com/azure/data-explorer/kusto/query/)

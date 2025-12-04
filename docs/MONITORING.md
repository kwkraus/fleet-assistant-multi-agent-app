# Fleet Assistant - Monitoring and Alerting Guide

This guide covers monitoring, observability, and alerting for the Fleet Assistant infrastructure.

## Overview

The monitoring stack includes:
- **Application Insights**: Application performance monitoring (APM)
- **Log Analytics**: Centralized logging and querying
- **Azure Monitor**: Metrics, alerts, and dashboards
- **Workbooks**: Custom visualization and reporting

## Application Insights

### Key Metrics Tracked

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| Request Rate | Requests per second | - |
| Response Time | Average request duration | > 2 seconds |
| Failed Requests | HTTP 5xx errors | > 10 in 15 min |
| Exceptions | Unhandled exceptions | > 5 in 15 min |
| Dependency Calls | External service calls | - |
| AI Foundry Latency | Agent response time | > 5 seconds |

### Accessing Application Insights

**Azure Portal:**
```
Navigate to: Resource Group → Application Insights → <app-insights-name>
```

**Azure CLI:**
```bash
# Get instrumentation key
az monitor app-insights component show \
  --resource-group fleet-assistant-prod-rg \
  --app fleet-ai-prod \
  --query instrumentationKey --output tsv

# Query recent requests
az monitor app-insights metrics show \
  --resource-group fleet-assistant-prod-rg \
  --app fleet-ai-prod \
  --metric requests/count \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-01T23:59:59Z
```

### Custom Telemetry

The backend should emit custom events for business metrics:

```csharp
// Track AI Foundry agent calls
_telemetryClient.TrackEvent("FoundryAgentCall", new Dictionary<string, string>
{
    { "AgentId", agentId },
    { "ConversationId", conversationId },
    { "ResponseTime", responseTime.ToString() }
});

// Track SSE streaming metrics
_telemetryClient.TrackMetric("SSE_StreamDuration", streamDuration.TotalSeconds);
_telemetryClient.TrackMetric("SSE_ChunkCount", chunkCount);
```

## Log Analytics Queries

### Common Queries

#### 1. Failed Requests in Last Hour
```kusto
requests
| where timestamp > ago(1h)
| where success == false
| summarize count() by resultCode, operation_Name
| order by count_ desc
```

#### 2. AI Foundry Performance
```kusto
dependencies
| where target contains "ai.azure.com"
| summarize avg(duration), percentile(duration, 95) by operation_Name
| order by avg_duration desc
```

#### 3. Exception Analysis
```kusto
exceptions
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
| order by count_ desc
```

#### 4. User Activity
```kusto
customEvents
| where name == "FoundryAgentCall"
| summarize RequestCount = count() by bin(timestamp, 1h)
| render timechart
```

#### 5. Performance Bottlenecks
```kusto
requests
| where duration > 2000  // Over 2 seconds
| project timestamp, name, duration, url
| order by duration desc
| take 50
```

### Execute Queries

**Azure Portal:** Application Insights → Logs → Enter query

**Azure CLI:**
```bash
az monitor log-analytics query \
  --workspace <workspace-id> \
  --analytics-query "requests | where timestamp > ago(1h) | take 100" \
  --output table
```

## Alerts Configuration

### Pre-Configured Alerts

The deployment creates these alerts automatically:

#### 1. High Server Errors
- **Condition**: More than 10 failed requests in 15 minutes
- **Severity**: 2 (Warning)
- **Action**: Email notification

#### 2. High Response Time
- **Condition**: Average response time > 2 seconds
- **Severity**: 3 (Informational)
- **Action**: Email notification

#### 3. High Exception Rate
- **Condition**: More than 5 exceptions in 15 minutes
- **Severity**: 2 (Warning)
- **Action**: Email notification

#### 4. Availability Test Failure
- **Condition**: Health check fails in 2+ locations
- **Severity**: 1 (Error)
- **Action**: Email notification

### Adding Custom Alerts

#### Example: AI Foundry Timeout Alert

```bash
az monitor metrics alert create \
  --name foundry-timeout-alert \
  --resource-group fleet-assistant-prod-rg \
  --scopes $(az monitor app-insights component show --resource-group fleet-assistant-prod-rg --app fleet-ai-prod --query id -o tsv) \
  --condition "avg dependencies/duration where target contains 'ai.azure.com' > 5000" \
  --window-size 15m \
  --evaluation-frequency 5m \
  --action fleet-ag-prod \
  --description "Alert when AI Foundry response time exceeds 5 seconds"
```

#### Example: High Chat Volume Alert

```bash
az monitor metrics alert create \
  --name high-chat-volume-alert \
  --resource-group fleet-assistant-prod-rg \
  --scopes $(az monitor app-insights component show --resource-group fleet-assistant-prod-rg --app fleet-ai-prod --query id -o tsv) \
  --condition "count customEvents/count where name == 'FoundryAgentCall' > 1000" \
  --window-size 15m \
  --evaluation-frequency 5m \
  --action fleet-ag-prod \
  --description "Alert when chat volume exceeds 1000 requests in 15 minutes"
```

### Notification Channels

Update the Action Group to add notification channels:

```bash
# Add SMS notification
az monitor action-group update \
  --resource-group fleet-assistant-prod-rg \
  --name fleet-ag-prod \
  --add-action sms AdminPhone +12025551234

# Add webhook notification
az monitor action-group update \
  --resource-group fleet-assistant-prod-rg \
  --name fleet-ag-prod \
  --add-action webhook AlertWebhook https://your-alerting-system.com/webhook

# Add Azure Function action
az monitor action-group update \
  --resource-group fleet-assistant-prod-rg \
  --name fleet-ag-prod \
  --add-action azurefunction AlertFunction $(az functionapp show --name your-function-app --resource-group your-rg --query id -o tsv)
```

## Availability Testing

### Health Check Endpoint

The deployment configures availability tests for `/healthz` endpoint monitoring:

**Test Configuration:**
- **Frequency**: 5 minutes
- **Locations**: East US, West US, West Europe
- **Timeout**: 30 seconds
- **Success Criteria**: HTTP 200, SSL valid

### Custom Health Checks

Implement comprehensive health checks in the backend:

```csharp
// FoundryHealthCheck.cs
public class FoundryHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Test AI Foundry connectivity
            var response = await _agentClient.TestConnectionAsync(cancellationToken);
            
            return response.IsSuccess 
                ? HealthCheckResult.Healthy("AI Foundry connection successful")
                : HealthCheckResult.Degraded("AI Foundry connection slow");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("AI Foundry connection failed", ex);
        }
    }
}
```

## Dashboards and Workbooks

### Pre-Built Workbook

The deployment creates a monitoring workbook with:
- Request volume and failure trends
- Response time distribution
- Exception analysis
- Dependency performance

**Access:**
```
Azure Portal → Application Insights → Workbooks → Fleet Assistant Monitoring Dashboard
```

### Creating Custom Dashboards

**Example: Real-time Operations Dashboard**

1. Navigate to Azure Portal → Dashboard → New Dashboard
2. Add tiles:
   - App Insights Metrics Chart (Requests/sec)
   - App Insights Metrics Chart (Response Time)
   - App Insights Metrics Chart (Failed Requests)
   - Log Analytics Query (AI Foundry Calls)
3. Save and share with team

**Via Azure CLI:**
```bash
# Export dashboard JSON
az portal dashboard show \
  --resource-group fleet-assistant-prod-rg \
  --name fleet-ops-dashboard \
  --output json > dashboard.json

# Import dashboard
az portal dashboard create \
  --resource-group fleet-assistant-staging-rg \
  --name fleet-ops-dashboard \
  --input-path dashboard.json
```

## Performance Monitoring

### Application Performance

**Key metrics to monitor:**
1. **Request throughput**: Requests per second
2. **Response time**: P50, P95, P99 percentiles
3. **Error rate**: Percentage of failed requests
4. **Dependency latency**: External service call times

**Query for performance baseline:**
```kusto
requests
| where timestamp > ago(7d)
| summarize 
    P50 = percentile(duration, 50),
    P95 = percentile(duration, 95),
    P99 = percentile(duration, 99),
    RequestCount = count()
by bin(timestamp, 1h), operation_Name
| render timechart
```

### Infrastructure Performance

**App Service Metrics:**
```bash
# CPU percentage
az monitor metrics list \
  --resource $(az webapp show --name fleet-api-prod --resource-group fleet-assistant-prod-rg --query id -o tsv) \
  --metric "CpuPercentage" \
  --start-time 2024-01-01T00:00:00Z \
  --interval PT1H

# Memory percentage
az monitor metrics list \
  --resource $(az webapp show --name fleet-api-prod --resource-group fleet-assistant-prod-rg --query id -o tsv) \
  --metric "MemoryPercentage" \
  --interval PT1H
```

**Front Door Metrics:**
```bash
# Request count
az monitor metrics list \
  --resource $(az afd profile show --name fleet-fd-prod --resource-group fleet-assistant-prod-rg --query id -o tsv) \
  --metric "RequestCount" \
  --interval PT1H

# Backend health percentage
az monitor metrics list \
  --resource $(az afd profile show --name fleet-fd-prod --resource-group fleet-assistant-prod-rg --query id -o tsv) \
  --metric "BackendHealthPercentage" \
  --interval PT5M
```

## Cost Monitoring

Track and optimize monitoring costs:

```bash
# Set up cost alert
az consumption budget create \
  --resource-group fleet-assistant-prod-rg \
  --budget-name monitoring-budget \
  --amount 100 \
  --time-grain monthly \
  --category Cost \
  --threshold 80

# View current costs
az consumption usage list \
  --start-date 2024-01-01 \
  --end-date 2024-01-31 \
  --query "[?contains(instanceId, 'ApplicationInsights') || contains(instanceId, 'LogAnalytics')]"
```

**Cost Optimization:**
- Configure daily cap on Log Analytics (dev/staging)
- Set appropriate log retention periods
- Use sampling for high-volume apps
- Archive old logs to blob storage

## Troubleshooting with Logs

### Scenario 1: Application Slow

```kusto
// Identify slow operations
requests
| where timestamp > ago(1h)
| where duration > 2000
| join (dependencies) on operation_Id
| project timestamp, operation_Name, duration, dependencyName = name1, dependencyDuration = duration1
| order by duration desc
```

### Scenario 2: AI Foundry Failures

```kusto
// Find AI Foundry errors
dependencies
| where target contains "ai.azure.com"
| where success == false
| project timestamp, operation_Name, resultCode, data
| order by timestamp desc
```

### Scenario 3: SSE Streaming Issues

```kusto
// Check SSE connection metrics
customMetrics
| where name startswith "SSE_"
| summarize avg(value), max(value), count() by name
```

## Best Practices

1. **Set baseline metrics**: Establish normal operating ranges
2. **Alert on anomalies**: Use smart detection for unusual patterns
3. **Reduce alert fatigue**: Tune thresholds to reduce false positives
4. **Correlate events**: Link logs, metrics, and traces
5. **Regular reviews**: Weekly review of key metrics and alerts
6. **Capacity planning**: Monitor growth trends for scaling
7. **Document incidents**: Use runbooks for common issues

## Integration with DevOps

### Application Insights in CI/CD

```yaml
# GitHub Actions: Query AI insights after deployment
- name: Check Application Health
  run: |
    az monitor app-insights metrics show \
      --resource-group fleet-assistant-prod-rg \
      --app fleet-ai-prod \
      --metric "requests/failed" \
      --start-time $(date -u -d '5 minutes ago' '+%Y-%m-%dT%H:%M:%SZ') \
      --output json | jq '.value[0].timeseries[0].data | map(select(.total > 0))'
```

### Automated Remediation

Create Azure Automation runbooks triggered by alerts:
- Auto-scale App Service on high CPU
- Restart unhealthy instances
- Clear cache on memory pressure
- Notify on-call engineer via PagerDuty

## Support

For monitoring issues:
1. Verify diagnostic settings are enabled on all resources
2. Check Log Analytics workspace connectivity
3. Ensure Application Insights instrumentation key is configured
4. Review alert rules and action groups
5. Contact Azure support for platform issues

## Additional Resources

- [Application Insights Documentation](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Log Analytics Query Language](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/log-query-overview)
- [Azure Monitor Best Practices](https://learn.microsoft.com/en-us/azure/azure-monitor/best-practices)
- [Kusto Query Language (KQL)](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/query/)

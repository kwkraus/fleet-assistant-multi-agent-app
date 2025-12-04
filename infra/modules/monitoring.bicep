// ========================================
// Monitoring Module
// ========================================
// This module provisions Application Insights, Log Analytics, and diagnostic settings
// for comprehensive monitoring and observability

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environmentName string

@description('Base name for resources')
param baseName string

@description('Tags to apply to all resources')
param tags object = {}

@description('Enable daily cap on Log Analytics data ingestion')
param enableDailyCap bool = false

@description('Daily cap in GB (only used if enableDailyCap is true)')
param dailyCapGb int = 1

@description('Log retention in days')
param logRetentionDays int = 30

// ========================================
// Log Analytics Workspace
// ========================================

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${baseName}-law-${environmentName}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: logRetentionDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: enableDailyCap ? {
      dailyQuotaGb: dailyCapGb
    } : null
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ========================================
// Application Insights
// ========================================

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-ai-${environmentName}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    RetentionInDays: logRetentionDays
  }
}

// ========================================
// Alerts and Action Groups
// ========================================

// Action Group for alerts (email/SMS/webhook notifications)
resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: '${baseName}-ag-${environmentName}'
  location: 'global'
  tags: tags
  properties: {
    groupShortName: 'FleetAlert'
    enabled: true
    emailReceivers: [
      {
        name: 'AdminEmail'
        emailAddress: 'admin@example.com' // Should be parameterized
        useCommonAlertSchema: true
      }
    ]
  }
}

// Alert: High HTTP Server Errors (5xx)
resource highServerErrorsAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${baseName}-high-server-errors-${environmentName}'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when server errors exceed threshold'
    severity: 2
    enabled: true
    scopes: [
      applicationInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighServerErrors'
          metricName: 'requests/failed'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 10
          timeAggregation: 'Count'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Alert: High Response Time
resource highResponseTimeAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${baseName}-high-response-time-${environmentName}'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when average response time exceeds 2 seconds'
    severity: 3
    enabled: true
    scopes: [
      applicationInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighResponseTime'
          metricName: 'requests/duration'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 2000 // milliseconds
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Alert: High Exception Rate
resource highExceptionRateAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${baseName}-high-exceptions-${environmentName}'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when exception rate is high'
    severity: 2
    enabled: true
    scopes: [
      applicationInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighExceptions'
          metricName: 'exceptions/count'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Count'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// ========================================
// Application Insights Availability Test
// ========================================

resource availabilityTest 'Microsoft.Insights/webtests@2022-06-15' = {
  name: '${baseName}-availability-test-${environmentName}'
  location: location
  tags: union(tags, {
    'hidden-link:${applicationInsights.id}': 'Resource'
  })
  kind: 'standard'
  properties: {
    SyntheticMonitorId: '${baseName}-availability-${environmentName}'
    Name: 'Health Check Availability Test'
    Description: 'Monitors health endpoint availability'
    Enabled: true
    Frequency: 300 // 5 minutes
    Timeout: 30
    Kind: 'standard'
    RetryEnabled: true
    Locations: [
      {
        Id: 'us-va-ash-azr' // East US
      }
      {
        Id: 'us-ca-sjc-azr' // West US
      }
      {
        Id: 'emea-nl-ams-azr' // West Europe
      }
    ]
    Request: {
      RequestUrl: 'https://example.com/healthz' // Should be parameterized with actual backend URL
      HttpVerb: 'GET'
      ParseDependentRequests: false
    }
    ValidationRules: {
      ExpectedHttpStatusCode: 200
      SSLCheck: true
      SSLCertRemainingLifetimeCheck: 7
    }
  }
}

// Alert for availability test failures
resource availabilityAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${baseName}-availability-alert-${environmentName}'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when availability test fails'
    severity: 1
    enabled: true
    scopes: [
      availabilityTest.id
      applicationInsights.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.WebtestLocationAvailabilityCriteria'
      webTestId: availabilityTest.id
      componentId: applicationInsights.id
      failedLocationCount: 2
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// ========================================
// Workbook for Fleet Assistant Monitoring
// ========================================

resource monitoringWorkbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: guid('${baseName}-workbook-${environmentName}')
  location: location
  tags: tags
  kind: 'shared'
  properties: {
    displayName: 'Fleet Assistant Monitoring Dashboard'
    serializedData: string({
      version: 'Notebook/1.0'
      items: [
        {
          type: 1
          content: {
            json: '## Fleet Assistant Application Monitoring\n\nComprehensive dashboard for monitoring the Fleet Assistant multi-agent application.'
          }
        }
        {
          type: 10
          content: {
            chartId: 'workbookRequests'
            version: 'MetricsItem/2.0'
            size: 2
            chartType: 2
            resourceType: 'microsoft.insights/components'
            metricScope: 0
            resourceIds: [
              applicationInsights.id
            ]
            timeContext: {
              durationMs: 3600000
            }
            metrics: [
              {
                namespace: 'microsoft.insights/components'
                metric: 'requests/count'
                aggregation: 1
              }
              {
                namespace: 'microsoft.insights/components'
                metric: 'requests/failed'
                aggregation: 1
              }
            ]
            title: 'Request Volume and Failures'
            gridSettings: {
              rowLimit: 10000
            }
          }
        }
        {
          type: 10
          content: {
            chartId: 'workbookPerformance'
            version: 'MetricsItem/2.0'
            size: 2
            chartType: 2
            resourceType: 'microsoft.insights/components'
            metricScope: 0
            resourceIds: [
              applicationInsights.id
            ]
            timeContext: {
              durationMs: 3600000
            }
            metrics: [
              {
                namespace: 'microsoft.insights/components'
                metric: 'requests/duration'
                aggregation: 4
              }
            ]
            title: 'Response Time'
            gridSettings: {
              rowLimit: 10000
            }
          }
        }
      ]
    })
    category: 'workbook'
    sourceId: applicationInsights.id
  }
}

// ========================================
// Outputs
// ========================================

output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output logAnalyticsWorkspaceCustomerId string = logAnalyticsWorkspace.properties.customerId

output applicationInsightsId string = applicationInsights.id
output applicationInsightsName string = applicationInsights.name
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString

output actionGroupId string = actionGroup.id

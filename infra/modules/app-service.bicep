// ========================================
// App Service Module
// ========================================
// This module provisions App Service Plan and App Service for the ASP.NET Core backend
// with VNet integration, private endpoints, autoscaling, and SSE-optimized configuration

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environmentName string

@description('Base name for resources')
param baseName string

@description('Tags to apply to all resources')
param tags object = {}

// ========================================
// App Service Configuration Parameters
// ========================================

@description('App Service Plan SKU')
@allowed([
  'B1' // Basic (dev/test)
  'B2'
  'B3'
  'S1' // Standard
  'S2'
  'S3'
  'P1v3' // Premium v3 (recommended for production)
  'P2v3'
  'P3v3'
])
param appServicePlanSku string = 'P1v3'

@description('Number of instances')
@minValue(1)
@maxValue(30)
param instanceCount int = 2

@description('Enable autoscaling')
param enableAutoscaling bool = true

@description('Minimum autoscale instance count')
param minInstanceCount int = 2

@description('Maximum autoscale instance count')
param maxInstanceCount int = 10

// ========================================
// Networking Parameters
// ========================================

@description('App Service subnet ID for VNet integration')
param appServiceSubnetId string

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string

@description('Private DNS zone ID for App Service')
param privateDnsZoneIdAppService string

// ========================================
// Identity and Security Parameters
// ========================================

@description('Managed identity resource ID')
param managedIdentityId string

@description('Managed identity client ID')
param managedIdentityClientId string

// ========================================
// Application Configuration Parameters
// ========================================

@description('Azure AI Foundry Agent Endpoint')
param foundryAgentEndpoint string

@description('Azure AI Foundry Agent ID (if known, otherwise leave empty)')
param foundryAgentId string = ''

@description('Application Insights Connection String')
param applicationInsightsConnectionString string

@description('Enable snapshot debugging')
param enableSnapshotDebugger bool = false

// ========================================
// App Service Plan
// ========================================

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${baseName}-asp-${environmentName}'
  location: location
  tags: tags
  sku: {
    name: appServicePlanSku
    capacity: instanceCount
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
    zoneRedundant: environmentName == 'prod' ? true : false
  }
}

// ========================================
// App Service (Backend API)
// ========================================

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: '${baseName}-api-${environmentName}'
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false // Disable for better load distribution
    virtualNetworkSubnetId: appServiceSubnetId
    vnetRouteAllEnabled: true // Route all traffic through VNet
    vnetImagePullEnabled: true
    vnetContentShareEnabled: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      http20Enabled: true // Enable HTTP/2 for better SSE performance
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/healthz'
      httpLoggingEnabled: true
      detailedErrorLoggingEnabled: true
      requestTracingEnabled: environmentName != 'prod'
      webSocketsEnabled: false // SSE uses HTTP, not WebSockets
      // CORS configuration for SSE streaming
      cors: {
        allowedOrigins: [
          '*' // Should be restricted to specific origins in production
        ]
        supportCredentials: false
      }
      // App settings for ASP.NET Core backend
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : 'Development'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'UseFoundryAgent'
          value: 'true'
        }
        {
          name: 'FoundryAgentService__AgentEndpoint'
          value: foundryAgentEndpoint
        }
        {
          name: 'FoundryAgentService__AgentId'
          value: foundryAgentId
        }
        {
          name: 'FoundryAgentService__RunPollingDelayMs'
          value: '100'
        }
        {
          name: 'FoundryAgentService__StreamingDelayMs'
          value: '50'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentityClientId
        }
        {
          name: 'OpenAPIBaseUrl'
          value: 'https://${baseName}-api-${environmentName}.azurewebsites.net'
        }
        // Snapshot debugger settings
        {
          name: 'SnapshotDebugger__IsEnabled'
          value: string(enableSnapshotDebugger)
        }
        {
          name: 'SnapshotDebugger__IsEnabledInDeveloperMode'
          value: 'false'
        }
        {
          name: 'SnapshotDebugger__SnapshotsPerTenMinutesLimit'
          value: '1'
        }
        {
          name: 'SnapshotDebugger__SnapshotsPerDayLimit'
          value: '30'
        }
        // SSE-specific timeout configurations
        {
          name: 'WEBSITE_LOAD_TIMEOUT'
          value: '600' // 10 minutes for long-running SSE connections
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: '' // Empty for in-memory database
          type: 'SQLAzure'
        }
      ]
    }
  }
}

// ========================================
// Autoscaling Rules
// ========================================

resource autoscaleSettings 'Microsoft.Insights/autoscalesettings@2022-10-01' = if (enableAutoscaling) {
  name: '${baseName}-api-autoscale-${environmentName}'
  location: location
  tags: tags
  properties: {
    enabled: true
    targetResourceUri: appServicePlan.id
    profiles: [
      {
        name: 'Auto scale based on CPU and memory'
        capacity: {
          minimum: string(minInstanceCount)
          maximum: string(maxInstanceCount)
          default: string(instanceCount)
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 85
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'MemoryPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 80
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'HttpQueueLength'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 10
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '2'
              cooldown: 'PT5M'
            }
          }
        ]
      }
      {
        name: 'Scale down during off-hours'
        capacity: {
          minimum: string(minInstanceCount)
          maximum: string(maxInstanceCount)
          default: string(minInstanceCount)
        }
        rules: []
        recurrence: {
          frequency: 'Week'
          schedule: {
            timeZone: 'Pacific Standard Time'
            days: [
              'Monday'
              'Tuesday'
              'Wednesday'
              'Thursday'
              'Friday'
            ]
            hours: [
              0
            ]
            minutes: [
              0
            ]
          }
        }
      }
    ]
  }
}

// ========================================
// Private Endpoint for App Service
// ========================================

resource appServicePrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: '${baseName}-api-pe-${environmentName}'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${baseName}-api-pe-connection'
        properties: {
          privateLinkServiceId: appService.id
          groupIds: [
            'sites'
          ]
        }
      }
    ]
  }
}

resource appServicePrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = {
  parent: appServicePrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'app-service-config'
        properties: {
          privateDnsZoneId: privateDnsZoneIdAppService
        }
      }
    ]
  }
}

// ========================================
// Diagnostic Settings
// ========================================

resource appServiceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'app-service-diagnostics'
  scope: appService
  properties: {
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
    logAnalyticsDestinationType: 'Dedicated'
  }
}

// ========================================
// Outputs
// ========================================

output appServiceId string = appService.id
output appServiceName string = appService.name
output appServiceHostname string = appService.properties.defaultHostName
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServicePlanId string = appServicePlan.id
output appServicePrincipalId string = '' // User-assigned identity doesn't have principalId on the app service

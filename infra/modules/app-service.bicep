// ============================================================================
// App Service Module - Backend ASP.NET Core WebAPI with SSE Support
// ============================================================================

@description('App Service Plan name')
param appServicePlanName string

@description('App Service name')
param appServiceName string

@description('Azure region for App Service resources')
param location string

@description('Tags to apply to App Service resources')
param tags object

@description('App Service Plan SKU name')
param skuName string

@description('App Service Plan SKU tier')
param skuTier string

@description('Minimum number of instances for autoscaling')
@minValue(1)
@maxValue(30)
param minInstances int

@description('Maximum number of instances for autoscaling')
@minValue(1)
@maxValue(30)
param maxInstances int

@description('Enable autoscaling')
param autoscaleEnabled bool

@description('Managed identity resource ID')
param managedIdentityId string

@description('Application Insights connection string')
@secure()
param appInsightsConnectionString string

@description('Application Insights instrumentation key')
@secure()
param appInsightsInstrumentationKey string

@description('SQL Database connection string')
@secure()
param sqlConnectionString string

@description('Storage connection string')
@secure()
param storageConnectionString string

@description('Azure AI Foundry agent endpoint')
param foundryAgentEndpoint string

@description('Azure AI Foundry agent ID')
param foundryAgentId string

@description('VNet integration subnet ID')
param vnetIntegrationSubnetId string

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string

@description('App Service private DNS zone ID')
param appServicePrivateDnsZoneId string

@description('Enable snapshot debugging')
param enableSnapshotDebugging bool = false

@description('Enable Always On')
param alwaysOn bool = true

@description('CORS allowed origins (use specific domains for production)')
param corsAllowedOrigins array = ['*']

// ============================================================================
// APP SERVICE PLAN
// ============================================================================

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
    capacity: minInstances
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
    zoneRedundant: false
  }
}

// ============================================================================
// AUTOSCALE SETTINGS
// ============================================================================

resource autoscaleSettings 'Microsoft.Insights/autoscalesettings@2022-10-01' = if (autoscaleEnabled) {
  name: '${appServicePlanName}-autoscale'
  location: location
  tags: tags
  properties: {
    enabled: true
    targetResourceUri: appServicePlan.id
    profiles: [
      {
        name: 'Auto scale based on CPU and Memory'
        capacity: {
          minimum: string(minInstances)
          maximum: string(maxInstances)
          default: string(minInstances)
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
              threshold: 60
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT10M'
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
        ]
      }
    ]
  }
}

// ============================================================================
// APP SERVICE (Backend WebAPI)
// ============================================================================

resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    virtualNetworkSubnetId: vnetIntegrationSubnetId
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: alwaysOn
      http20Enabled: true // Enable HTTP/2 for SSE support
      minTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
      healthCheckPath: '/healthz'
      cors: {
        allowedOrigins: corsAllowedOrigins
        supportCredentials: false
      }
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsights__InstrumentationKey'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: sqlConnectionString
        }
        {
          name: 'BlobStorage__ConnectionString'
          value: storageConnectionString
        }
        {
          name: 'BlobStorage__DefaultContainer'
          value: 'fleet-documents'
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
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'SnapshotDebugger__IsEnabled'
          value: enableSnapshotDebugging ? 'true' : 'false'
        }
        {
          name: 'SnapshotDebugger__SnapshotsPerTenMinutesLimit'
          value: '3'
        }
        {
          name: 'SnapshotDebugger__SnapshotsPerDayLimit'
          value: '30'
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: sqlConnectionString
          type: 'SQLAzure'
        }
      ]
    }
  }
}

// ============================================================================
// PRIVATE ENDPOINT (Optional - for secure access)
// ============================================================================

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = {
  name: '${appServiceName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${appServiceName}-pe-connection'
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

// ============================================================================
// PRIVATE DNS ZONE GROUP
// ============================================================================

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'config1'
        properties: {
          privateDnsZoneId: appServicePrivateDnsZoneId
        }
      }
    ]
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output appServicePlanId string = appServicePlan.id
output appServicePlanName string = appServicePlan.name

output appServiceId string = appService.id
output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServiceFqdn string = appService.properties.defaultHostName
output privateEndpointId string = privateEndpoint.id

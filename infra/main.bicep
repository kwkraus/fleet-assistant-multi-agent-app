// ============================================================================
// Fleet Assistant Multi-Agent Application - Main Bicep Template
// Follows Microsoft Reliable Web App (RWA) Pattern for .NET
// Reference: https://learn.microsoft.com/en-us/azure/architecture/web-apps/guides/enterprise-app-patterns/reliable-web-app/dotnet/guidance
// ============================================================================

targetScope = 'subscription'

// ============================================================================
// PARAMETERS
// ============================================================================

@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Azure region for all resources')
param location string = deployment().location

@description('Application name prefix for resource naming')
@minLength(3)
@maxLength(10)
param applicationName string = 'fleet'

@description('Deployment timestamp for uniqueness')
param deploymentTimestamp string = utcNow('yyyyMMddHHmmss')

@description('Tags to apply to all resources')
param tags object = {
  Application: 'FleetAssistant'
  Environment: environment
  ManagedBy: 'Bicep'
  DeploymentDate: deploymentTimestamp
}

@description('Enable snapshot debugging for App Service')
param enableSnapshotDebugging bool = false

@description('Azure AI Foundry configuration')
param foundryConfig object = {
  aiHubName: '${applicationName}-aihub-${environment}'
  projectName: '${applicationName}-aiproject-${environment}'
  aiServicesName: '${applicationName}-aiservices-${environment}'
}

@description('Networking configuration')
param networkConfig object = {
  hubVnetAddressPrefix: '10.0.0.0/16'
  hubFirewallSubnetPrefix: '10.0.1.0/24'
  hubBastionSubnetPrefix: '10.0.2.0/24'
  spokeVnetAddressPrefix: '10.1.0.0/16'
  spokeAppServiceSubnetPrefix: '10.1.1.0/24'
  spokePrivateEndpointSubnetPrefix: '10.1.2.0/24'
  spokeDatabaseSubnetPrefix: '10.1.3.0/24'
}

@description('App Service configuration per environment')
param appServiceConfig object = {
  dev: {
    skuName: 'B1'
    skuTier: 'Basic'
    minInstances: 1
    maxInstances: 2
    autoscaleEnabled: false
  }
  staging: {
    skuName: 'S1'
    skuTier: 'Standard'
    minInstances: 1
    maxInstances: 3
    autoscaleEnabled: true
  }
  prod: {
    skuName: 'P1v3'
    skuTier: 'PremiumV3'
    minInstances: 2
    maxInstances: 10
    autoscaleEnabled: true
  }
}

@description('SQL Database configuration per environment')
param sqlConfig object = {
  dev: {
    skuName: 'Basic'
    skuTier: 'Basic'
    capacity: 5
  }
  staging: {
    skuName: 'S0'
    skuTier: 'Standard'
    capacity: 10
  }
  prod: {
    skuName: 'S1'
    skuTier: 'Standard'
    capacity: 20
  }
}

@description('SQL administrator login username')
@secure()
param sqlAdminUsername string

@description('SQL administrator login password')
@secure()
param sqlAdminPassword string

// ============================================================================
// VARIABLES
// ============================================================================

var resourceGroupName = '${applicationName}-rg-${environment}'
var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroupName), 0, 6)

// ============================================================================
// RESOURCE GROUP
// ============================================================================

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// ============================================================================
// MANAGED IDENTITY (User-Assigned for shared permissions)
// ============================================================================

module managedIdentity 'modules/managed-identity.bicep' = {
  scope: resourceGroup
  name: 'managedIdentity-deployment'
  params: {
    identityName: '${applicationName}-identity-${environment}'
    location: location
    tags: tags
  }
}

// ============================================================================
// MONITORING
// ============================================================================

module monitoring 'modules/monitoring.bicep' = {
  scope: resourceGroup
  name: 'monitoring-deployment'
  params: {
    workspaceName: '${applicationName}-logs-${environment}'
    appInsightsName: '${applicationName}-appinsights-${environment}'
    location: location
    tags: tags
  }
}

// ============================================================================
// NETWORKING
// ============================================================================

module networking 'modules/networking.bicep' = {
  scope: resourceGroup
  name: 'networking-deployment'
  params: {
    applicationName: applicationName
    environment: environment
    location: location
    tags: tags
    hubVnetAddressPrefix: networkConfig.hubVnetAddressPrefix
    hubFirewallSubnetPrefix: networkConfig.hubFirewallSubnetPrefix
    hubBastionSubnetPrefix: networkConfig.hubBastionSubnetPrefix
    spokeVnetAddressPrefix: networkConfig.spokeVnetAddressPrefix
    spokeAppServiceSubnetPrefix: networkConfig.spokeAppServiceSubnetPrefix
    spokePrivateEndpointSubnetPrefix: networkConfig.spokePrivateEndpointSubnetPrefix
    spokeDatabaseSubnetPrefix: networkConfig.spokeDatabaseSubnetPrefix
  }
}

// ============================================================================
// STORAGE
// ============================================================================

module storage 'modules/storage.bicep' = {
  scope: resourceGroup
  name: 'storage-deployment'
  params: {
    storageAccountName: '${applicationName}st${uniqueSuffix}' // Shorter name to fit 24 char limit
    containerName: 'fleet-documents'
    location: location
    tags: tags
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
    privateDnsZoneId: networking.outputs.blobPrivateDnsZoneId
  }
  dependsOn: [
    networking
  ]
}

// ============================================================================
// DATABASE
// ============================================================================

module database 'modules/database.bicep' = {
  scope: resourceGroup
  name: 'database-deployment'
  params: {
    sqlServerName: '${applicationName}-sql-${environment}-${uniqueSuffix}'
    databaseName: '${applicationName}db${environment}'
    location: location
    tags: tags
    adminUsername: sqlAdminUsername
    adminPassword: sqlAdminPassword
    skuName: sqlConfig[environment].skuName
    skuTier: sqlConfig[environment].skuTier
    capacity: sqlConfig[environment].capacity
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
    privateDnsZoneId: networking.outputs.sqlPrivateDnsZoneId
  }
  dependsOn: [
    networking
  ]
}

// ============================================================================
// AZURE AI FOUNDRY
// ============================================================================

module aiFoundry 'modules/ai-foundry.bicep' = {
  scope: resourceGroup
  name: 'ai-foundry-deployment'
  params: {
    aiHubName: foundryConfig.aiHubName
    projectName: foundryConfig.projectName
    aiServicesName: foundryConfig.aiServicesName
    location: location
    tags: tags
    managedIdentityId: managedIdentity.outputs.identityId
    managedIdentityPrincipalId: managedIdentity.outputs.principalId
  }
  dependsOn: [
    managedIdentity
  ]
}

// ============================================================================
// APP SERVICE (Backend)
// ============================================================================

module appService 'modules/app-service.bicep' = {
  scope: resourceGroup
  name: 'app-service-deployment'
  params: {
    appServicePlanName: '${applicationName}-plan-${environment}'
    appServiceName: '${applicationName}-api-${environment}-${uniqueSuffix}'
    location: location
    tags: tags
    skuName: appServiceConfig[environment].skuName
    skuTier: appServiceConfig[environment].skuTier
    minInstances: appServiceConfig[environment].minInstances
    maxInstances: appServiceConfig[environment].maxInstances
    autoscaleEnabled: appServiceConfig[environment].autoscaleEnabled
    managedIdentityId: managedIdentity.outputs.identityId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    appInsightsInstrumentationKey: monitoring.outputs.appInsightsInstrumentationKey
    sqlConnectionString: database.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
    foundryAgentEndpoint: aiFoundry.outputs.agentEndpoint
    foundryAgentId: aiFoundry.outputs.agentId
    vnetIntegrationSubnetId: networking.outputs.appServiceSubnetId
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
    appServicePrivateDnsZoneId: networking.outputs.appServicePrivateDnsZoneId
    enableSnapshotDebugging: enableSnapshotDebugging
  }
  dependsOn: [
    monitoring
    database
    storage
    aiFoundry
    networking
    managedIdentity
  ]
}

// ============================================================================
// STATIC WEB APP (Frontend)
// ============================================================================

module staticWebApp 'modules/static-web-app.bicep' = {
  scope: resourceGroup
  name: 'static-web-app-deployment'
  params: {
    staticWebAppName: '${applicationName}-frontend-${environment}'
    location: location
    tags: tags
    backendApiUrl: appService.outputs.appServiceUrl
    skuName: environment == 'prod' ? 'Standard' : 'Free'
  }
  dependsOn: [
    appService
  ]
}

// ============================================================================
// SECURITY (Front Door, WAF)
// ============================================================================

module security 'modules/security.bicep' = {
  scope: resourceGroup
  name: 'security-deployment'
  params: {
    frontDoorName: '${applicationName}-fd-${environment}-${uniqueSuffix}'
    wafPolicyName: '${applicationName}waf${environment}'
    location: 'global' // Front Door is a global service
    tags: tags
    backendAppServiceFqdn: appService.outputs.appServiceFqdn
    frontendStaticWebAppFqdn: staticWebApp.outputs.defaultHostname
  }
  dependsOn: [
    appService
    staticWebApp
  ]
}

// ============================================================================
// OUTPUTS
// ============================================================================

output resourceGroupName string = resourceGroupName
output location string = location
output environment string = environment

// Networking
output hubVnetId string = networking.outputs.hubVnetId
output spokeVnetId string = networking.outputs.spokeVnetId

// Managed Identity
output managedIdentityId string = managedIdentity.outputs.identityId
output managedIdentityClientId string = managedIdentity.outputs.clientId
output managedIdentityPrincipalId string = managedIdentity.outputs.principalId

// Monitoring
output appInsightsInstrumentationKey string = monitoring.outputs.appInsightsInstrumentationKey
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString
output logAnalyticsWorkspaceId string = monitoring.outputs.workspaceId

// Storage
output storageAccountName string = storage.outputs.storageAccountName
output storageConnectionString string = storage.outputs.connectionString

// Database
output sqlServerName string = database.outputs.sqlServerName
output sqlDatabaseName string = database.outputs.databaseName
output sqlConnectionString string = database.outputs.connectionString

// Azure AI Foundry
output aiHubName string = aiFoundry.outputs.aiHubName
output aiProjectName string = aiFoundry.outputs.aiProjectName
output foundryAgentEndpoint string = aiFoundry.outputs.agentEndpoint
output foundryAgentId string = aiFoundry.outputs.agentId

// App Service
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output appServiceFqdn string = appService.outputs.appServiceFqdn

// Static Web App
output staticWebAppName string = staticWebApp.outputs.staticWebAppName
output staticWebAppUrl string = staticWebApp.outputs.defaultHostname
output staticWebAppDeploymentToken string = staticWebApp.outputs.deploymentToken

// Front Door
output frontDoorEndpoint string = security.outputs.frontDoorEndpoint
output frontDoorId string = security.outputs.frontDoorId

// Deployment Information
output deploymentTimestamp string = deploymentTimestamp

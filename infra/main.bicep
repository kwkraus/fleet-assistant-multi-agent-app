// ========================================
// Main Bicep Template - Fleet Assistant Infrastructure
// ========================================
// This template orchestrates all modules to provision the complete
// Fleet Assistant solution following the Microsoft Reliable Web App pattern

targetScope = 'resourceGroup'

// ========================================
// Parameters
// ========================================

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environmentName string

@description('Base name for all resources (will be prefixed/suffixed as needed)')
@minLength(3)
@maxLength(10)
param baseName string

@description('Resource tags')
param tags object = {
  Environment: environmentName
  Application: 'FleetAssistant'
  ManagedBy: 'Bicep'
  Pattern: 'ReliableWebApp'
}

// ========================================
// Networking Parameters
// ========================================

@description('Hub VNet address space')
param hubVNetAddressPrefix string = '10.0.0.0/16'

@description('Spoke VNet address space')
param spokeVNetAddressPrefix string = '10.1.0.0/16'

// ========================================
// Security Parameters
// ========================================

@description('Enable WAF for Front Door')
param enableWaf bool = (environmentName == 'prod')

// ========================================
// App Service Parameters
// ========================================

@description('App Service Plan SKU')
param appServicePlanSku string = (environmentName == 'prod') ? 'P1v3' : (environmentName == 'staging') ? 'S1' : 'B1'

@description('Number of App Service instances')
param appServiceInstanceCount int = (environmentName == 'prod') ? 2 : 1

@description('Enable autoscaling')
param enableAutoscaling bool = (environmentName == 'prod')

@description('Minimum autoscale instance count')
param minInstanceCount int = (environmentName == 'prod') ? 2 : 1

@description('Maximum autoscale instance count')
param maxInstanceCount int = (environmentName == 'prod') ? 10 : 3

// ========================================
// AI Foundry Parameters
// ========================================

@description('AI Services SKU')
@allowed([
  'S0'
  'F0'
])
param aiServicesSku string = (environmentName == 'prod') ? 'S0' : 'S0'

@description('Azure AI Foundry Agent ID (leave empty if not yet created)')
param foundryAgentId string = ''

// ========================================
// Static Web App Parameters
// ========================================

@description('Static Web App SKU')
@allowed([
  'Free'
  'Standard'
])
param staticWebAppSku string = (environmentName == 'prod') ? 'Standard' : 'Free'

@description('GitHub repository URL for Static Web App CI/CD')
param repositoryUrl string = ''

@description('GitHub repository branch')
param repositoryBranch string = 'main'

@description('GitHub repository token for deployment')
@secure()
param repositoryToken string = ''

// ========================================
// Monitoring Parameters
// ========================================

@description('Enable daily cap on Log Analytics ingestion')
param enableDailyCap bool = (environmentName != 'prod')

@description('Daily cap in GB')
param dailyCapGb int = (environmentName == 'dev') ? 1 : 5

@description('Log retention in days')
param logRetentionDays int = (environmentName == 'prod') ? 90 : 30

// ========================================
// Other Parameters
// ========================================

@description('Enable snapshot debugger')
param enableSnapshotDebugger bool = false

// ========================================
// Module: Networking
// ========================================

module networking './modules/networking.bicep' = {
  name: 'networking-${environmentName}-${uniqueString(resourceGroup().id)}'
  params: {
    location: location
    environmentName: environmentName
    baseName: baseName
    tags: tags
    hubVNetAddressPrefix: hubVNetAddressPrefix
    spokeVNetAddressPrefix: spokeVNetAddressPrefix
  }
}

// ========================================
// Module: Monitoring
// ========================================

module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring-${environmentName}-${uniqueString(resourceGroup().id)}'
  params: {
    location: location
    environmentName: environmentName
    baseName: baseName
    tags: tags
    enableDailyCap: enableDailyCap
    dailyCapGb: dailyCapGb
    logRetentionDays: logRetentionDays
  }
}

// ========================================
// Module: Security (Managed Identities, Firewall)
// ========================================

// Note: We need placeholder hostnames for Front Door origin configuration
// These will be updated after the backend and frontend are deployed
var placeholderBackendHostname = '${baseName}-api-${environmentName}.azurewebsites.net'
var placeholderFrontendHostname = '${baseName}-swa-${environmentName}.azurestaticapps.net'

module security './modules/security.bicep' = {
  name: 'security-${environmentName}-${uniqueString(resourceGroup().id)}'
  params: {
    location: location
    environmentName: environmentName
    baseName: baseName
    tags: tags
    hubVNetId: networking.outputs.hubVNetId
    backendHostname: placeholderBackendHostname
    frontendHostname: placeholderFrontendHostname
    enableWaf: enableWaf
  }
}

// ========================================
// Module: AI Foundry
// ========================================

module aiFoundry './modules/ai-foundry.bicep' = {
  name: 'ai-foundry-${environmentName}-${uniqueString(resourceGroup().id)}'
  params: {
    location: location
    environmentName: environmentName
    baseName: baseName
    tags: tags
    managedIdentityPrincipalId: security.outputs.appServiceManagedIdentityPrincipalId
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
    privateDnsZoneIdCognitiveServices: networking.outputs.privateDnsZoneIdCognitiveServices
    aiServicesSku: aiServicesSku
  }
}

// ========================================
// Module: App Service (Backend API)
// ========================================

module appService './modules/app-service.bicep' = {
  name: 'app-service-${environmentName}-${uniqueString(resourceGroup().id)}'
  params: {
    location: location
    environmentName: environmentName
    baseName: baseName
    tags: tags
    appServicePlanSku: appServicePlanSku
    instanceCount: appServiceInstanceCount
    enableAutoscaling: enableAutoscaling
    minInstanceCount: minInstanceCount
    maxInstanceCount: maxInstanceCount
    appServiceSubnetId: networking.outputs.appServiceSubnetId
    privateEndpointSubnetId: networking.outputs.privateEndpointSubnetId
    privateDnsZoneIdAppService: networking.outputs.privateDnsZoneIdAppService
    managedIdentityId: security.outputs.appServiceManagedIdentityId
    managedIdentityClientId: security.outputs.appServiceManagedIdentityClientId
    foundryAgentEndpoint: aiFoundry.outputs.foundryAgentEndpoint
    foundryAgentId: foundryAgentId
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    enableSnapshotDebugger: enableSnapshotDebugger
  }
}

// ========================================
// Module: Static Web App (Frontend)
// ========================================

module staticWebApp './modules/static-web-app.bicep' = {
  name: 'static-web-app-${environmentName}-${uniqueString(resourceGroup().id)}'
  params: {
    location: location
    environmentName: environmentName
    baseName: baseName
    tags: tags
    staticWebAppSku: staticWebAppSku
    backendApiUrl: appService.outputs.appServiceUrl
    repositoryUrl: repositoryUrl
    repositoryBranch: repositoryBranch
    repositoryToken: repositoryToken
  }
}

// ========================================
// Outputs
// ========================================

// Networking Outputs
output hubVNetName string = networking.outputs.hubVNetName
output spokeVNetName string = networking.outputs.spokeVNetName

// Security Outputs
output frontDoorEndpointHostname string = security.outputs.frontDoorEndpointHostname
output frontDoorUrl string = 'https://${security.outputs.frontDoorEndpointHostname}'
output azureFirewallPrivateIp string = security.outputs.azureFirewallPrivateIp

// Managed Identity Outputs
output appServiceManagedIdentityClientId string = security.outputs.appServiceManagedIdentityClientId

// AI Foundry Outputs
output aiServicesEndpoint string = aiFoundry.outputs.aiServicesEndpoint
output aiProjectName string = aiFoundry.outputs.aiProjectName
output foundryAgentEndpoint string = aiFoundry.outputs.foundryAgentEndpoint
output foundryProjectResourceId string = aiFoundry.outputs.foundryProjectResourceId

// App Service Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output appServiceHostname string = appService.outputs.appServiceHostname

// Static Web App Outputs
output staticWebAppName string = staticWebApp.outputs.staticWebAppName
output staticWebAppUrl string = staticWebApp.outputs.staticWebAppUrl
output staticWebAppHostname string = staticWebApp.outputs.staticWebAppDefaultHostname

// Monitoring Outputs
output applicationInsightsName string = monitoring.outputs.applicationInsightsName
output applicationInsightsConnectionString string = monitoring.outputs.applicationInsightsConnectionString
output logAnalyticsWorkspaceName string = monitoring.outputs.logAnalyticsWorkspaceName

// Deployment Summary
output deploymentSummary object = {
  environment: environmentName
  region: location
  frontDoorUrl: 'https://${security.outputs.frontDoorEndpointHostname}'
  staticWebAppUrl: staticWebApp.outputs.staticWebAppUrl
  appServiceUrl: appService.outputs.appServiceUrl
  aiProjectName: aiFoundry.outputs.aiProjectName
  foundryAgentEndpoint: aiFoundry.outputs.foundryAgentEndpoint
  nextSteps: [
    '1. Create an agent in the Azure AI Foundry project portal'
    '2. Update the deployment with the agent ID: az deployment group create --parameters foundryAgentId=<your-agent-id>'
    '3. Deploy the backend application code to the App Service'
    '4. Deploy the frontend application to the Static Web App'
    '5. Configure custom domains in Front Door (if needed)'
    '6. Review and configure monitoring alerts'
  ]
}

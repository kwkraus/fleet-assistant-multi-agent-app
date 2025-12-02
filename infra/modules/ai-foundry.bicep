// ============================================================================
// Azure AI Foundry Module
// Provisions AI Hub, AI Project, and AI Services for multi-agent system
// ============================================================================

@description('AI Hub name')
param aiHubName string

@description('AI Project name')
param projectName string

@description('AI Services name (Cognitive Services)')
param aiServicesName string

@description('Azure region for AI resources')
param location string

@description('Tags to apply to AI resources')
param tags object

@description('Managed identity resource ID')
param managedIdentityId string

@description('Managed identity principal ID for RBAC assignments')
param managedIdentityPrincipalId string

@description('AI Services SKU')
@allowed(['S0', 'F0'])
param aiServicesSku string = 'S0'

// ============================================================================
// AI SERVICES (Cognitive Services Multi-Service Account)
// ============================================================================

resource aiServices 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: aiServicesName
  location: location
  tags: tags
  kind: 'CognitiveServices'
  sku: {
    name: aiServicesSku
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    apiProperties: {
      statisticsEnabled: false
    }
    customSubDomainName: aiServicesName
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

// ============================================================================
// AI HUB (Machine Learning Workspace for AI Foundry)
// Note: AI Hub uses the Machine Learning workspace resource type
// ============================================================================

resource aiHub 'Microsoft.MachineLearningServices/workspaces@2024-04-01' = {
  name: aiHubName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  kind: 'Hub'
  properties: {
    friendlyName: aiHubName
    description: 'AI Hub for Fleet Assistant Multi-Agent System'
    publicNetworkAccess: 'Enabled'
    discoveryUrl: 'https://${location}.api.azureml.ms/discovery'
  }
}

// ============================================================================
// AI PROJECT (Machine Learning Workspace of kind 'Project')
// ============================================================================

resource aiProject 'Microsoft.MachineLearningServices/workspaces@2024-04-01' = {
  name: projectName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  kind: 'Project'
  properties: {
    friendlyName: projectName
    description: 'Fleet Assistant Multi-Agent Project'
    publicNetworkAccess: 'Enabled'
    hubResourceId: aiHub.id
  }
  dependsOn: [
    aiHub
  ]
}

// ============================================================================
// RBAC ROLE ASSIGNMENT - Grant Managed Identity access to AI Services
// ============================================================================

// Cognitive Services User role
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, managedIdentityPrincipalId, cognitiveServicesUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output aiHubId string = aiHub.id
output aiHubName string = aiHub.name

output aiProjectId string = aiProject.id
output aiProjectName string = aiProject.name

output aiServicesId string = aiServices.id
output aiServicesName string = aiServices.name
output aiServicesEndpoint string = aiServices.properties.endpoint

// Agent configuration for backend App Service
// The agent endpoint follows the Azure AI Foundry pattern
output agentEndpoint string = 'https://${location}.api.azureml.ms/agents/v1.0/subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.MachineLearningServices/workspaces/${aiProject.name}'

// Note: Agent ID should be configured based on the specific agent deployed in the AI Foundry project
// This is a placeholder that should be replaced with the actual agent ID after deployment
output agentId string = 'fleet-planning-agent'

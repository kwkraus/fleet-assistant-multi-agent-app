// ========================================
// AI Foundry Module
// ========================================
// This module provisions Azure AI Foundry Project, AI Hub, AI Services,
// and configures secure access for the Fleet Assistant multi-agent system

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environmentName string

@description('Base name for resources')
param baseName string

@description('Tags to apply to all resources')
param tags object = {}

@description('Managed identity principal ID for RBAC assignments')
param managedIdentityPrincipalId string

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string

@description('Private DNS zone ID for Cognitive Services')
param privateDnsZoneIdCognitiveServices string

@description('AI Services SKU')
@allowed([
  'S0' // Standard
  'F0' // Free (for dev/test only)
])
param aiServicesSku string = 'S0'

// ========================================
// Storage Account for AI Hub
// ========================================

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${baseName}aihubst${environmentName}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// ========================================
// AI Services (Cognitive Services Multi-Service)
// ========================================

resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: '${baseName}-ai-services-${environmentName}'
  location: location
  tags: tags
  sku: {
    name: aiServicesSku
  }
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: '${baseName}-ai-${environmentName}'
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
    disableLocalAuth: false // Required for AI Foundry integration
  }
}

// Private Endpoint for AI Services
resource aiServicesPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: '${baseName}-ai-pe-${environmentName}'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${baseName}-ai-pe-connection'
        properties: {
          privateLinkServiceId: aiServices.id
          groupIds: [
            'account'
          ]
        }
      }
    ]
  }
}

resource aiServicesPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = {
  parent: aiServicesPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'cognitive-services-config'
        properties: {
          privateDnsZoneId: privateDnsZoneIdCognitiveServices
        }
      }
    ]
  }
}

// ========================================
// Azure AI Hub
// ========================================

resource aiHub 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: '${baseName}-ai-hub-${environmentName}'
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  kind: 'Hub'
  properties: {
    friendlyName: 'Fleet Assistant AI Hub - ${environmentName}'
    description: 'Azure AI Hub for Fleet Assistant multi-agent system'
    storageAccount: storageAccount.id
    publicNetworkAccess: 'Disabled'
    managedNetwork: {
      isolationMode: 'AllowInternetOutbound'
    }
  }
}

// ========================================
// Azure AI Foundry Project
// ========================================

resource aiProject 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: '${baseName}-ai-project-${environmentName}'
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  kind: 'Project'
  properties: {
    friendlyName: 'Fleet Assistant Project - ${environmentName}'
    description: 'Azure AI Foundry Project for Fleet Assistant with multi-agent orchestration'
    hubResourceId: aiHub.id
    publicNetworkAccess: 'Disabled'
  }
}

// ========================================
// RBAC Assignments
// ========================================

// Cognitive Services OpenAI User role for managed identity
var cognitiveServicesOpenAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

resource aiServicesRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, managedIdentityPrincipalId, cognitiveServicesOpenAIUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUserRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Cognitive Services User role for managed identity (broader access)
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'

resource aiServicesUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, managedIdentityPrincipalId, cognitiveServicesUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Blob Data Contributor for AI Hub
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, managedIdentityPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Azure Machine Learning Workspace Connection Secrets Reader (for AI Project)
var mlWorkspaceConnectionSecretsReaderRoleId = 'ea01e6af-a1c1-4350-9563-ad00f8c72ec5'

resource aiProjectRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiProject.id, managedIdentityPrincipalId, mlWorkspaceConnectionSecretsReaderRoleId)
  scope: aiProject
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', mlWorkspaceConnectionSecretsReaderRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ========================================
// Diagnostic Settings
// ========================================

// Note: Log Analytics workspace should be passed in from monitoring module
// For now, we'll enable diagnostics to storage account

resource aiServicesDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'ai-services-diagnostics'
  scope: aiServices
  properties: {
    storageAccountId: storageAccount.id
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
    ]
  }
}

// ========================================
// Outputs
// ========================================

// AI Services
output aiServicesId string = aiServices.id
output aiServicesName string = aiServices.name
output aiServicesEndpoint string = aiServices.properties.endpoint
// Note: AI Services key should be retrieved separately using Azure CLI or Key Vault
// Do not output sensitive keys in deployment outputs

// AI Hub
output aiHubId string = aiHub.id
output aiHubName string = aiHub.name

// AI Project
output aiProjectId string = aiProject.id
output aiProjectName string = aiProject.name
output aiProjectEndpoint string = 'https://${aiProject.properties.discoveryUrl}/api/projects/${aiProject.name}'

// Storage
output storageAccountId string = storageAccount.id
output storageAccountName string = storageAccount.name

// Foundry Agent Configuration (for App Service)
// The agent endpoint format follows Azure AI Foundry convention
output foundryAgentEndpoint string = 'https://${location}.api.azureml.ms/projects/${aiProject.name}'
// Note: The actual AgentId will need to be created within the AI Foundry project
// This is typically done through the Azure AI Foundry portal or SDK after deployment
output foundryProjectResourceId string = aiProject.id

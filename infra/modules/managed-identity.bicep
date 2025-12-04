// ============================================================================
// Managed Identity Module - User-Assigned Managed Identity
// Used for shared permissions across resources (App Service, AI Foundry, etc.)
// ============================================================================

@description('Name of the managed identity')
param identityName string

@description('Azure region for the identity')
param location string

@description('Tags to apply to the identity')
param tags object

// ============================================================================
// MANAGED IDENTITY
// ============================================================================

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

// ============================================================================
// OUTPUTS
// ============================================================================

output identityId string = managedIdentity.id
output clientId string = managedIdentity.properties.clientId
output principalId string = managedIdentity.properties.principalId
output identityName string = managedIdentity.name

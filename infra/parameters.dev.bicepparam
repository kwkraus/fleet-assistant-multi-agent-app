// ============================================================================
// Development Environment Parameters
// ============================================================================

using './main.bicep'

// Environment configuration
param environment = 'dev'
param location = 'eastus'
param applicationName = 'fleet'

// SQL credentials (use Azure Key Vault or secure parameter passing in production)
param sqlAdminUsername = 'sqladmin'
// Note: Pass sqlAdminPassword securely via command line: --parameters sqlAdminPassword='YourSecurePassword123!'

// Tags
param tags = {
  Application: 'FleetAssistant'
  Environment: 'dev'
  ManagedBy: 'Bicep'
  CostCenter: 'Engineering'
  Owner: 'DevTeam'
}

// Feature flags
param enableSnapshotDebugging = false

// Azure AI Foundry configuration
param foundryConfig = {
  aiHubName: 'fleet-aihub-dev'
  projectName: 'fleet-aiproject-dev'
  aiServicesName: 'fleet-aiservices-dev'
}

// Networking configuration (dev environment uses default ranges)
param networkConfig = {
  hubVnetAddressPrefix: '10.0.0.0/16'
  hubFirewallSubnetPrefix: '10.0.1.0/24'
  hubBastionSubnetPrefix: '10.0.2.0/24'
  spokeVnetAddressPrefix: '10.1.0.0/16'
  spokeAppServiceSubnetPrefix: '10.1.1.0/24'
  spokePrivateEndpointSubnetPrefix: '10.1.2.0/24'
  spokeDatabaseSubnetPrefix: '10.1.3.0/24'
}

// App Service configuration (development - small/cost-effective)
param appServiceConfig = {
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

// SQL Database configuration (development - minimal tier)
param sqlConfig = {
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

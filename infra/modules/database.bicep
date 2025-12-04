// ============================================================================
// Database Module - Azure SQL Database with Private Endpoint
// ============================================================================

@description('SQL Server name (must be globally unique)')
param sqlServerName string

@description('SQL Database name')
param databaseName string

@description('Azure region for database resources')
param location string

@description('Tags to apply to database resources')
param tags object

@description('SQL administrator login username')
@secure()
param adminUsername string

@description('SQL administrator login password')
@secure()
param adminPassword string

@description('Database SKU name')
param skuName string

@description('Database SKU tier')
param skuTier string

@description('Database capacity (DTUs or vCores)')
param capacity int

@description('Private endpoint subnet ID')
param privateEndpointSubnetId string

@description('Private DNS zone ID for SQL Database')
param privateDnsZoneId string

@description('Enable public network access')
param publicNetworkAccess bool = false

@description('Maximum database size in bytes')
param maxSizeBytes int = 2147483648 // 2GB default

// ============================================================================
// SQL SERVER
// ============================================================================

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: adminUsername
    administratorLoginPassword: adminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: publicNetworkAccess ? 'Enabled' : 'Disabled'
  }
}

// ============================================================================
// SQL DATABASE
// ============================================================================

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
    capacity: capacity
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: maxSizeBytes
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
  }
}

// ============================================================================
// FIREWALL RULES (Allow Azure Services when public access is enabled)
// ============================================================================

resource firewallRule 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = if (publicNetworkAccess) {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ============================================================================
// PRIVATE ENDPOINT
// ============================================================================

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = {
  name: '${sqlServerName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${sqlServerName}-pe-connection'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: [
            'sqlServer'
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
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output sqlServerId string = sqlServer.id
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseId string = sqlDatabase.id
output databaseName string = sqlDatabase.name
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Persist Security Info=False;User ID=${adminUsername};Password=${adminPassword};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
output privateEndpointId string = privateEndpoint.id

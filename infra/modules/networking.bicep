// ============================================================================
// Networking Module - Hub-and-Spoke VNet Topology
// Implements Microsoft Reliable Web App networking pattern
// ============================================================================

@description('Application name prefix')
param applicationName string

@description('Environment name')
param environment string

@description('Azure region for networking resources')
param location string

@description('Tags to apply to networking resources')
param tags object

// Hub VNet parameters
@description('Hub VNet address prefix')
param hubVnetAddressPrefix string

@description('Hub Firewall subnet prefix')
param hubFirewallSubnetPrefix string

@description('Hub Bastion subnet prefix')
param hubBastionSubnetPrefix string

// Spoke VNet parameters
@description('Spoke VNet address prefix')
param spokeVnetAddressPrefix string

@description('Spoke App Service integration subnet prefix')
param spokeAppServiceSubnetPrefix string

@description('Spoke Private Endpoint subnet prefix')
param spokePrivateEndpointSubnetPrefix string

@description('Spoke Database subnet prefix')
param spokeDatabaseSubnetPrefix string

// ============================================================================
// HUB VIRTUAL NETWORK
// ============================================================================

resource hubVnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: '${applicationName}-hub-vnet-${environment}'
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        hubVnetAddressPrefix
      ]
    }
    subnets: [
      {
        name: 'AzureFirewallSubnet'
        properties: {
          addressPrefix: hubFirewallSubnetPrefix
          serviceEndpoints: []
        }
      }
      {
        name: 'AzureBastionSubnet'
        properties: {
          addressPrefix: hubBastionSubnetPrefix
          serviceEndpoints: []
        }
      }
    ]
  }
}

// ============================================================================
// SPOKE VIRTUAL NETWORK
// ============================================================================

resource spokeVnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: '${applicationName}-spoke-vnet-${environment}'
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        spokeVnetAddressPrefix
      ]
    }
    subnets: [
      {
        name: 'AppServiceSubnet'
        properties: {
          addressPrefix: spokeAppServiceSubnetPrefix
          delegations: [
            {
              name: 'delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
          serviceEndpoints: [
            {
              service: 'Microsoft.Storage'
              locations: [
                location
              ]
            }
            {
              service: 'Microsoft.Sql'
              locations: [
                location
              ]
            }
          ]
          networkSecurityGroup: {
            id: appServiceNsg.id
          }
        }
      }
      {
        name: 'PrivateEndpointSubnet'
        properties: {
          addressPrefix: spokePrivateEndpointSubnetPrefix
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
          serviceEndpoints: []
          networkSecurityGroup: {
            id: privateEndpointNsg.id
          }
        }
      }
      {
        name: 'DatabaseSubnet'
        properties: {
          addressPrefix: spokeDatabaseSubnetPrefix
          serviceEndpoints: [
            {
              service: 'Microsoft.Sql'
              locations: [
                location
              ]
            }
          ]
          networkSecurityGroup: {
            id: databaseNsg.id
          }
        }
      }
    ]
  }
}

// ============================================================================
// NETWORK SECURITY GROUPS
// ============================================================================

// App Service NSG
resource appServiceNsg 'Microsoft.Network/networkSecurityGroups@2023-05-01' = {
  name: '${applicationName}-appservice-nsg-${environment}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowHttpsInbound'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: '*'
          description: 'Allow HTTPS inbound traffic'
        }
      }
      {
        name: 'AllowHttpInbound'
        properties: {
          priority: 110
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: '*'
          description: 'Allow HTTP inbound traffic'
        }
      }
      {
        name: 'AllowAppServiceOutbound'
        properties: {
          priority: 100
          direction: 'Outbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          description: 'Allow all outbound traffic from App Service'
        }
      }
    ]
  }
}

// Private Endpoint NSG
resource privateEndpointNsg 'Microsoft.Network/networkSecurityGroups@2023-05-01' = {
  name: '${applicationName}-pe-nsg-${environment}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowVNetInbound'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'VirtualNetwork'
          destinationAddressPrefix: 'VirtualNetwork'
          description: 'Allow traffic from VNet to private endpoints'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 1000
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          description: 'Deny all other inbound traffic'
        }
      }
    ]
  }
}

// Database NSG
resource databaseNsg 'Microsoft.Network/networkSecurityGroups@2023-05-01' = {
  name: '${applicationName}-db-nsg-${environment}'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowSqlFromAppService'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '1433'
          sourceAddressPrefix: spokeAppServiceSubnetPrefix
          destinationAddressPrefix: '*'
          description: 'Allow SQL traffic from App Service subnet'
        }
      }
      {
        name: 'DenyAllInbound'
        properties: {
          priority: 1000
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          description: 'Deny all other inbound traffic'
        }
      }
    ]
  }
}

// ============================================================================
// VNET PEERING (Hub-to-Spoke and Spoke-to-Hub)
// ============================================================================

resource hubToSpokePeering 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2023-05-01' = {
  parent: hubVnet
  name: 'hub-to-spoke-peering'
  properties: {
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
    allowGatewayTransit: false
    useRemoteGateways: false
    remoteVirtualNetwork: {
      id: spokeVnet.id
    }
  }
}

resource spokeToHubPeering 'Microsoft.Network/virtualNetworks/virtualNetworkPeerings@2023-05-01' = {
  parent: spokeVnet
  name: 'spoke-to-hub-peering'
  properties: {
    allowVirtualNetworkAccess: true
    allowForwardedTraffic: true
    allowGatewayTransit: false
    useRemoteGateways: false
    remoteVirtualNetwork: {
      id: hubVnet.id
    }
  }
}

// ============================================================================
// PRIVATE DNS ZONES
// ============================================================================

// Private DNS Zone for Azure Storage (Blob)
resource blobPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.blob.${az.environment().suffixes.storage}'
  location: 'global'
  tags: tags
}

resource blobPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: blobPrivateDnsZone
  name: '${applicationName}-blob-dns-link-${environment}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: spokeVnet.id
    }
  }
}

// Private DNS Zone for Azure SQL Database
resource sqlPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink${az.environment().suffixes.sqlServerHostname}'
  location: 'global'
  tags: tags
}

resource sqlPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: sqlPrivateDnsZone
  name: '${applicationName}-sql-dns-link-${environment}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: spokeVnet.id
    }
  }
}

// Private DNS Zone for App Service
resource appServicePrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.azurewebsites.net'
  location: 'global'
  tags: tags
}

resource appServicePrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: appServicePrivateDnsZone
  name: '${applicationName}-appservice-dns-link-${environment}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: spokeVnet.id
    }
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output hubVnetId string = hubVnet.id
output hubVnetName string = hubVnet.name

output spokeVnetId string = spokeVnet.id
output spokeVnetName string = spokeVnet.name

output appServiceSubnetId string = spokeVnet.properties.subnets[0].id
output privateEndpointSubnetId string = spokeVnet.properties.subnets[1].id
output databaseSubnetId string = spokeVnet.properties.subnets[2].id

output appServiceNsgId string = appServiceNsg.id
output privateEndpointNsgId string = privateEndpointNsg.id
output databaseNsgId string = databaseNsg.id

output blobPrivateDnsZoneId string = blobPrivateDnsZone.id
output sqlPrivateDnsZoneId string = sqlPrivateDnsZone.id
output appServicePrivateDnsZoneId string = appServicePrivateDnsZone.id

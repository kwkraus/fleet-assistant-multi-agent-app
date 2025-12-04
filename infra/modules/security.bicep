// ========================================
// Security Module - Front Door, WAF, Firewall
// ========================================
// This module implements enterprise-grade security for the Reliable Web App pattern
// including Azure Front Door Premium, WAF, Azure Firewall, and managed identities

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environmentName string

@description('Base name for resources')
param baseName string

@description('Tags to apply to all resources')
param tags object = {}

@description('Hub VNet ID for firewall deployment')
param hubVNetId string

@description('Backend App Service hostname')
param backendHostname string

@description('Static Web App hostname')
param frontendHostname string

@description('Enable WAF (recommended for production)')
param enableWaf bool = true

// ========================================
// Azure Front Door Premium
// ========================================

resource frontDoorProfile 'Microsoft.Cdn/profiles@2023-05-01' = {
  name: '${baseName}-fd-${environmentName}'
  location: 'global'
  tags: tags
  sku: {
    name: 'Premium_AzureFrontDoor'
  }
  properties: {
    originResponseTimeoutSeconds: 60
  }
}

// WAF Policy for Front Door
resource wafPolicy 'Microsoft.Network/FrontDoorWebApplicationFirewallPolicies@2022-05-01' = if (enableWaf) {
  name: '${baseName}wafpolicy${environmentName}'
  location: 'global'
  tags: tags
  sku: {
    name: 'Premium_AzureFrontDoor'
  }
  properties: {
    policySettings: {
      enabledState: 'Enabled'
      mode: 'Prevention'
      requestBodyCheck: 'Enabled'
    }
    managedRules: {
      managedRuleSets: [
        {
          ruleSetType: 'Microsoft_DefaultRuleSet'
          ruleSetVersion: '2.1'
          ruleSetAction: 'Block'
        }
        {
          ruleSetType: 'Microsoft_BotManagerRuleSet'
          ruleSetVersion: '1.0'
        }
      ]
    }
    customRules: {
      rules: [
        {
          name: 'RateLimitRule'
          priority: 1
          ruleType: 'RateLimitRule'
          rateLimitThreshold: 100
          rateLimitDurationInMinutes: 1
          matchConditions: [
            {
              matchVariable: 'RequestUri'
              operator: 'Contains'
              matchValue: [
                '/api/'
              ]
            }
          ]
          action: 'Block'
        }
        {
          name: 'GeoFilterRule'
          priority: 2
          ruleType: 'MatchRule'
          matchConditions: [
            {
              matchVariable: 'RemoteAddr'
              operator: 'GeoMatch'
              negateCondition: true
              matchValue: [
                'US'
                'CA'
                'GB'
                'DE'
                'FR'
              ]
            }
          ]
          action: 'Block'
        }
      ]
    }
  }
}

// Front Door Endpoint
resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2023-05-01' = {
  parent: frontDoorProfile
  name: '${baseName}-endpoint-${environmentName}'
  location: 'global'
  properties: {
    enabledState: 'Enabled'
  }
}

// Origin Group for Backend API
resource backendOriginGroup 'Microsoft.Cdn/profiles/originGroups@2023-05-01' = {
  parent: frontDoorProfile
  name: 'backend-origin-group'
  properties: {
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 50
    }
    healthProbeSettings: {
      probePath: '/healthz'
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 30
    }
    sessionAffinityState: 'Enabled'
  }
}

// Origin for Backend App Service
resource backendOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2023-05-01' = {
  parent: backendOriginGroup
  name: 'backend-app-service'
  properties: {
    hostName: backendHostname
    httpPort: 80
    httpsPort: 443
    originHostHeader: backendHostname
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}

// Origin Group for Frontend Static Web App
resource frontendOriginGroup 'Microsoft.Cdn/profiles/originGroups@2023-05-01' = {
  parent: frontDoorProfile
  name: 'frontend-origin-group'
  properties: {
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 50
    }
    healthProbeSettings: {
      probePath: '/'
      probeRequestType: 'HEAD'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 60
    }
    sessionAffinityState: 'Disabled'
  }
}

// Origin for Frontend Static Web App
resource frontendOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2023-05-01' = {
  parent: frontendOriginGroup
  name: 'frontend-static-web-app'
  properties: {
    hostName: frontendHostname
    httpPort: 80
    httpsPort: 443
    originHostHeader: frontendHostname
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}

// Route for API traffic
resource apiRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2023-05-01' = {
  parent: frontDoorEndpoint
  name: 'api-route'
  properties: {
    customDomains: []
    originGroup: {
      id: backendOriginGroup.id
    }
    ruleSets: []
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/api/*'
      '/healthz'
      '/swagger/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
  }
  dependsOn: [
    backendOrigin
  ]
}

// Route for frontend traffic
resource frontendRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2023-05-01' = {
  parent: frontDoorEndpoint
  name: 'frontend-route'
  properties: {
    customDomains: []
    originGroup: {
      id: frontendOriginGroup.id
    }
    ruleSets: []
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
  }
  dependsOn: [
    frontendOrigin
    apiRoute // Ensure API route is created first for proper priority
  ]
}

// Security Policy linking WAF to endpoint
resource securityPolicy 'Microsoft.Cdn/profiles/securityPolicies@2023-05-01' = if (enableWaf) {
  parent: frontDoorProfile
  name: 'security-policy'
  properties: {
    parameters: {
      type: 'WebApplicationFirewall'
      wafPolicy: {
        id: wafPolicy.id
      }
      associations: [
        {
          domains: [
            {
              id: frontDoorEndpoint.id
            }
          ]
          patternsToMatch: [
            '/*'
          ]
        }
      ]
    }
  }
}

// ========================================
// Azure Firewall (Hub Network)
// ========================================

resource firewallPublicIp 'Microsoft.Network/publicIPAddresses@2023-11-01' = {
  name: '${baseName}-fw-pip-${environmentName}'
  location: location
  tags: tags
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
}

resource azureFirewall 'Microsoft.Network/azureFirewalls@2023-11-01' = {
  name: '${baseName}-fw-${environmentName}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'AZFW_VNet'
      tier: 'Standard'
    }
    ipConfigurations: [
      {
        name: 'firewallIpConfig'
        properties: {
          publicIPAddress: {
            id: firewallPublicIp.id
          }
          subnet: {
            id: '${hubVNetId}/subnets/AzureFirewallSubnet'
          }
        }
      }
    ]
    networkRuleCollections: [
      {
        name: 'AllowAzureServices'
        properties: {
          priority: 100
          action: {
            type: 'Allow'
          }
          rules: [
            {
              name: 'AllowAzureMonitor'
              protocols: [
                'TCP'
              ]
              sourceAddresses: [
                '10.1.0.0/16' // Spoke VNet
              ]
              destinationAddresses: [
                'AzureMonitor'
              ]
              destinationPorts: [
                '443'
              ]
            }
            {
              name: 'AllowAzureStorage'
              protocols: [
                'TCP'
              ]
              sourceAddresses: [
                '10.1.0.0/16'
              ]
              destinationAddresses: [
                'Storage'
              ]
              destinationPorts: [
                '443'
              ]
            }
            {
              name: 'AllowAzureKeyVault'
              protocols: [
                'TCP'
              ]
              sourceAddresses: [
                '10.1.0.0/16'
              ]
              destinationAddresses: [
                'AzureKeyVault'
              ]
              destinationPorts: [
                '443'
              ]
            }
          ]
        }
      }
    ]
    applicationRuleCollections: [
      {
        name: 'AllowAzureServices'
        properties: {
          priority: 100
          action: {
            type: 'Allow'
          }
          rules: [
            {
              name: 'AllowAzureAPIs'
              protocols: [
                {
                  protocolType: 'Https'
                  port: 443
                }
              ]
              targetFqdns: [
                '*.azure.com'
                '*.microsoft.com'
                '*.windows.net'
                '*.ai.azure.com'
              ]
              sourceAddresses: [
                '10.1.0.0/16'
              ]
            }
          ]
        }
      }
    ]
  }
}

// ========================================
// Managed Identities
// ========================================

// User-assigned managed identity for App Service
resource appServiceManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${baseName}-app-identity-${environmentName}'
  location: location
  tags: tags
}

// User-assigned managed identity for AI Foundry
resource aiFoundryManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${baseName}-ai-identity-${environmentName}'
  location: location
  tags: tags
}

// ========================================
// Outputs
// ========================================

output frontDoorEndpointHostname string = frontDoorEndpoint.properties.hostName
output frontDoorId string = frontDoorProfile.id
output frontDoorName string = frontDoorProfile.name

output wafPolicyId string = enableWaf ? wafPolicy.id : ''

output azureFirewallPrivateIp string = azureFirewall.properties.ipConfigurations[0].properties.privateIPAddress
output azureFirewallId string = azureFirewall.id

output appServiceManagedIdentityId string = appServiceManagedIdentity.id
output appServiceManagedIdentityPrincipalId string = appServiceManagedIdentity.properties.principalId
output appServiceManagedIdentityClientId string = appServiceManagedIdentity.properties.clientId

output aiFoundryManagedIdentityId string = aiFoundryManagedIdentity.id
output aiFoundryManagedIdentityPrincipalId string = aiFoundryManagedIdentity.properties.principalId
output aiFoundryManagedIdentityClientId string = aiFoundryManagedIdentity.properties.clientId

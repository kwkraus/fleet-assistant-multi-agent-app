// ============================================================================
// Security Module - Azure Front Door and WAF
// ============================================================================

@description('Front Door profile name')
param frontDoorName string

@description('WAF policy name')
param wafPolicyName string

@description('Location (Front Door is global)')
param location string

@description('Tags to apply to security resources')
param tags object

@description('Backend App Service FQDN')
param backendAppServiceFqdn string

@description('Frontend Static Web App FQDN')
param frontendStaticWebAppFqdn string

@description('Front Door SKU')
@allowed(['Standard_AzureFrontDoor', 'Premium_AzureFrontDoor'])
param frontDoorSku string = 'Standard_AzureFrontDoor'

// ============================================================================
// WAF POLICY
// ============================================================================

resource wafPolicy 'Microsoft.Network/FrontDoorWebApplicationFirewallPolicies@2022-05-01' = {
  name: wafPolicyName
  location: location
  tags: tags
  sku: {
    name: frontDoorSku
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
          exclusions: []
          ruleGroupOverrides: []
        }
        {
          ruleSetType: 'Microsoft_BotManagerRuleSet'
          ruleSetVersion: '1.0'
          ruleSetAction: 'Block'
          exclusions: []
          ruleGroupOverrides: []
        }
      ]
    }
    customRules: {
      rules: [
        {
          name: 'RateLimitRule'
          priority: 100
          ruleType: 'RateLimitRule'
          rateLimitDurationInMinutes: 1
          rateLimitThreshold: 100
          matchConditions: [
            {
              matchVariable: 'RequestUri'
              operator: 'Contains'
              matchValue: [
                '/api/'
              ]
              transforms: []
            }
          ]
          action: 'Block'
        }
      ]
    }
  }
}

// ============================================================================
// FRONT DOOR PROFILE
// ============================================================================

resource frontDoorProfile 'Microsoft.Cdn/profiles@2023-05-01' = {
  name: frontDoorName
  location: location
  tags: tags
  sku: {
    name: frontDoorSku
  }
  properties: {
    originResponseTimeoutSeconds: 60
  }
}

// ============================================================================
// FRONT DOOR ENDPOINT
// ============================================================================

resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2023-05-01' = {
  parent: frontDoorProfile
  name: '${frontDoorName}-endpoint'
  location: location
  properties: {
    enabledState: 'Enabled'
  }
}

// ============================================================================
// ORIGIN GROUPS
// ============================================================================

// Backend API Origin Group
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
    sessionAffinityState: 'Disabled'
  }
}

// Frontend Origin Group
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
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 30
    }
    sessionAffinityState: 'Disabled'
  }
}

// ============================================================================
// ORIGINS
// ============================================================================

// Backend API Origin
resource backendOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2023-05-01' = {
  parent: backendOriginGroup
  name: 'backend-origin'
  properties: {
    hostName: backendAppServiceFqdn
    httpPort: 80
    httpsPort: 443
    originHostHeader: backendAppServiceFqdn
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}

// Frontend Origin
resource frontendOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2023-05-01' = {
  parent: frontendOriginGroup
  name: 'frontend-origin'
  properties: {
    hostName: frontendStaticWebAppFqdn
    httpPort: 80
    httpsPort: 443
    originHostHeader: frontendStaticWebAppFqdn
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}

// ============================================================================
// ROUTES
// ============================================================================

// Backend API Route
resource backendRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2023-05-01' = {
  parent: frontDoorEndpoint
  name: 'backend-route'
  properties: {
    customDomains: []
    originGroup: {
      id: backendOriginGroup.id
    }
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

// Frontend Route
resource frontendRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2023-05-01' = {
  parent: frontDoorEndpoint
  name: 'frontend-route'
  properties: {
    customDomains: []
    originGroup: {
      id: frontendOriginGroup.id
    }
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
    backendRoute // Ensure backend route is created first for priority
  ]
}

// ============================================================================
// SECURITY POLICY (Associate WAF with Front Door)
// ============================================================================

resource securityPolicy 'Microsoft.Cdn/profiles/securityPolicies@2023-05-01' = {
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

// ============================================================================
// OUTPUTS
// ============================================================================

output frontDoorId string = frontDoorProfile.id
output frontDoorName string = frontDoorProfile.name
output frontDoorEndpoint string = 'https://${frontDoorEndpoint.properties.hostName}'
output frontDoorEndpointHostName string = frontDoorEndpoint.properties.hostName
output wafPolicyId string = wafPolicy.id
output wafPolicyName string = wafPolicy.name

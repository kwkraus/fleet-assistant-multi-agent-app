// ============================================================================
// Static Web App Module - Next.js 15 Frontend
// ============================================================================

@description('Static Web App name')
param staticWebAppName string

@description('Azure region for Static Web App')
param location string

@description('Tags to apply to Static Web App')
param tags object

@description('Backend API URL')
param backendApiUrl string

@description('Static Web App SKU')
@allowed(['Free', 'Standard'])
param skuName string = 'Free'

@description('Repository URL (optional - for GitHub Actions integration)')
param repositoryUrl string = ''

@description('Repository branch (optional)')
param repositoryBranch string = 'main'

// ============================================================================
// STATIC WEB APP
// ============================================================================

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    repositoryUrl: repositoryUrl != '' ? repositoryUrl : null
    branch: repositoryUrl != '' ? repositoryBranch : null
    buildProperties: {
      appLocation: 'src/frontend/ai-chatbot'
      apiLocation: ''
      outputLocation: '.next'
      appBuildCommand: 'npm run build'
      apiBuildCommand: ''
    }
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: repositoryUrl != '' ? 'GitHub' : 'None'
  }
}

// ============================================================================
// APP SETTINGS (Environment Variables for Next.js)
// ============================================================================

resource appSettings 'Microsoft.Web/staticSites/config@2023-01-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    NEXT_PUBLIC_API_URL: backendApiUrl
  }
}

// ============================================================================
// CUSTOM DOMAIN (Optional - can be configured post-deployment)
// ============================================================================

// Note: Custom domains require additional configuration and verification
// They are typically set up after initial deployment
// Example configuration (commented out):
// resource customDomain 'Microsoft.Web/staticSites/customDomains@2023-01-01' = {
//   parent: staticWebApp
//   name: 'www.example.com'
//   properties: {}
// }

// ============================================================================
// OUTPUTS
// ============================================================================

output staticWebAppId string = staticWebApp.id
output staticWebAppName string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname
output deploymentToken string = staticWebApp.listSecrets().properties.apiKey
output repositoryUrl string = repositoryUrl

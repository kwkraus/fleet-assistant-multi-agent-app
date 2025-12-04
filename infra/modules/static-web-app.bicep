// ========================================
// Static Web App Module
// ========================================
// This module provisions Azure Static Web App for the Next.js 15 frontend
// with custom domain support and environment variable configuration

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environmentName string

@description('Base name for resources')
param baseName string

@description('Tags to apply to all resources')
param tags object = {}

// ========================================
// Static Web App Configuration
// ========================================

@description('Static Web App SKU')
@allowed([
  'Free'
  'Standard'
])
param staticWebAppSku string = 'Standard'

@description('Backend API URL for Next.js frontend')
param backendApiUrl string

@description('GitHub repository URL (optional, for CI/CD)')
param repositoryUrl string = ''

@description('GitHub repository branch (optional)')
param repositoryBranch string = 'main'

@description('GitHub repository token (optional, for CI/CD setup)')
@secure()
param repositoryToken string = ''

// ========================================
// Azure Static Web App
// ========================================

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: '${baseName}-swa-${environmentName}'
  location: location
  tags: tags
  sku: {
    name: staticWebAppSku
    tier: staticWebAppSku
  }
  properties: {
    repositoryUrl: !empty(repositoryUrl) ? repositoryUrl : null
    branch: !empty(repositoryUrl) ? repositoryBranch : null
    repositoryToken: !empty(repositoryToken) ? repositoryToken : null
    buildProperties: !empty(repositoryUrl) ? {
      appLocation: 'src/frontend/ai-chatbot'
      apiLocation: ''
      outputLocation: '.next'
      appBuildCommand: 'npm run build'
      apiBuildCommand: ''
      skipGithubActionWorkflowGeneration: false
    } : null
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: !empty(repositoryUrl) ? 'GitHub' : 'None'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

// ========================================
// Environment Variables / App Settings
// ========================================

resource staticWebAppConfig 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    // Frontend environment variables
    NEXT_PUBLIC_API_URL: backendApiUrl
  }
}

// ========================================
// Custom Domain (Optional - commented out)
// ========================================

// Uncomment and configure when you have a custom domain
/*
resource customDomain 'Microsoft.Web/staticSites/customDomains@2023-12-01' = {
  parent: staticWebApp
  name: 'www.example.com'
  properties: {
    validationMethod: 'cname-delegation'
  }
}
*/

// ========================================
// Basic Authentication (Optional - for non-prod)
// ========================================

// Enable basic auth for dev/staging environments to restrict access
resource staticWebAppBasicAuth 'Microsoft.Web/staticSites/config@2023-12-01' = if (environmentName != 'prod') {
  parent: staticWebApp
  name: 'functionappsettings'
  properties: {
    SWA_RUNTIME_SKIP_FUNCTION: 'false'
    // Add basic auth credentials if needed
  }
}

// ========================================
// Build Configuration File
// ========================================

// Note: Azure Static Web Apps expects a staticwebapp.config.json file in the root
// of your frontend application for routing, auth, and other settings.
// Example content:
/*
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    },
    {
      "route": "/*",
      "allowedRoles": ["anonymous"]
    }
  ],
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/images/*.{png,jpg,gif}", "/css/*"]
  },
  "responseOverrides": {
    "404": {
      "rewrite": "/index.html",
      "statusCode": 200
    }
  },
  "globalHeaders": {
    "content-security-policy": "default-src https: 'unsafe-eval' 'unsafe-inline'; object-src 'none'",
    "X-Frame-Options": "DENY",
    "X-Content-Type-Options": "nosniff",
    "Referrer-Policy": "strict-origin-when-cross-origin"
  },
  "mimeTypes": {
    ".json": "application/json"
  }
}
*/

// ========================================
// Diagnostic Settings
// ========================================

// Note: Static Web Apps have limited diagnostic settings support
// Most logging is available through the Azure portal's monitoring blade

// ========================================
// Outputs
// ========================================

output staticWebAppId string = staticWebApp.id
output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'

// Note: API key for deployment should be retrieved separately for security
// Use: az staticwebapp secrets list --name <app-name>

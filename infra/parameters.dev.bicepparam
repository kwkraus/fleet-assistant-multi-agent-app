// ========================================
// Development Environment Parameters
// ========================================
// This file contains parameter values optimized for development/testing

using './main.bicep'

// ========================================
// Core Parameters
// ========================================

param environmentName = 'dev'
param baseName = 'fleet'
param location = 'eastus'

param tags = {
  Environment: 'dev'
  Application: 'FleetAssistant'
  ManagedBy: 'Bicep'
  Pattern: 'ReliableWebApp'
  CostCenter: 'Engineering'
  Owner: 'DevTeam'
}

// ========================================
// Networking Parameters
// ========================================

param hubVNetAddressPrefix = '10.0.0.0/16'
param spokeVNetAddressPrefix = '10.1.0.0/16'

// ========================================
// Security Parameters
// ========================================

// WAF disabled for dev to reduce costs
param enableWaf = false

// ========================================
// App Service Parameters
// ========================================

// Basic tier for development
param appServicePlanSku = 'B1'
param appServiceInstanceCount = 1
param enableAutoscaling = false
param minInstanceCount = 1
param maxInstanceCount = 2

// ========================================
// AI Foundry Parameters
// ========================================

// Standard tier even for dev (Free tier has limitations)
param aiServicesSku = 'S0'

// Agent ID - update this after creating an agent in AI Foundry portal
param foundryAgentId = ''

// ========================================
// Static Web App Parameters
// ========================================

// Free tier for development
param staticWebAppSku = 'Free'

// Optional: GitHub integration for CI/CD
// Uncomment and fill in when ready to enable automated deployments
param repositoryUrl = ''
param repositoryBranch = 'main'
// param repositoryToken = '' // Pass this as a secure parameter during deployment

// ========================================
// Monitoring Parameters
// ========================================

// Enable daily cap to control costs in dev
param enableDailyCap = true
param dailyCapGb = 1
param logRetentionDays = 7 // Shorter retention for dev

// ========================================
// Debugging Parameters
// ========================================

// Enable snapshot debugger for dev troubleshooting
param enableSnapshotDebugger = false

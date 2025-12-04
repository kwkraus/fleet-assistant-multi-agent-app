# Fleet Assistant - Azure Infrastructure Deployment Guide

This guide provides step-by-step instructions for deploying the Fleet Assistant infrastructure to Azure using Bicep templates following the Microsoft Reliable Web App (RWA) pattern.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Architecture Overview](#architecture-overview)
- [Pre-Deployment Steps](#pre-deployment-steps)
- [Deployment Process](#deployment-process)
- [Post-Deployment Configuration](#post-deployment-configuration)
- [Verification and Testing](#verification-and-testing)
- [Troubleshooting](#troubleshooting)
- [Cost Management](#cost-management)

## Prerequisites

### Required Tools

- **Azure CLI** (version 2.50.0 or later)
  ```bash
  az --version
  az upgrade
  ```

- **Bicep CLI** (comes with Azure CLI 2.20.0+)
  ```bash
  az bicep version
  az bicep upgrade
  ```

- **Azure Subscription** with appropriate permissions:
  - Owner or Contributor role at subscription or resource group level
  - Ability to create service principals and managed identities
  - Permissions to assign RBAC roles

### Azure Service Quotas

Verify you have sufficient quota for:
- Virtual Networks
- App Service Plans (Premium v3 tier for production)
- Static Web Apps (Standard SKU for production)
- Azure Front Door Premium
- Azure Firewall
- AI Services / Cognitive Services

Check quotas:
```bash
az vm list-usage --location eastus --output table
```

## Architecture Overview

The infrastructure follows the Microsoft Reliable Web App pattern with:

- **Hub-Spoke Network Topology**: Centralized security and traffic management
- **Azure Front Door Premium**: Global load balancing with WAF protection
- **Private Endpoints**: Secure PaaS service access without public exposure
- **Managed Identities**: Secure service-to-service authentication
- **Multi-tier Security**: NSGs, Azure Firewall, WAF, private networking
- **Comprehensive Monitoring**: Application Insights, Log Analytics, custom alerts

### Resource Naming Convention

Resources follow this pattern: `{baseName}-{resourceType}-{environmentName}`

Example: `fleet-api-dev` for the development App Service

## Pre-Deployment Steps

### 1. Login to Azure

```bash
# Login to Azure
az login

# Set your subscription (if you have multiple)
az account set --subscription "Your-Subscription-Name-or-ID"

# Verify current subscription
az account show --output table
```

### 2. Choose Deployment Region

Select an Azure region that:
- Supports all required services (App Service P1v3, Front Door Premium, AI Services)
- Meets your latency requirements
- Aligns with data residency requirements

```bash
# List available regions
az account list-locations --output table

# Verify service availability in region
az provider show --namespace Microsoft.CognitiveServices --query "resourceTypes[?resourceType=='accounts'].locations" --output table
```

### 3. Create Resource Group

```bash
# For development
az group create \
  --name fleet-assistant-dev-rg \
  --location eastus \
  --tags Environment=dev Application=FleetAssistant

# For staging
az group create \
  --name fleet-assistant-staging-rg \
  --location eastus \
  --tags Environment=staging Application=FleetAssistant

# For production
az group create \
  --name fleet-assistant-prod-rg \
  --location eastus \
  --tags Environment=prod Application=FleetAssistant
```

### 4. Configure Parameters

Copy the example parameter file for your environment:

```bash
# For staging
cp infra/parameters.staging.bicepparam.example infra/parameters.staging.bicepparam

# For production
cp infra/parameters.prod.bicepparam.example infra/parameters.prod.bicepparam
```

Edit the parameter file and update:
- `baseName`: Unique identifier for your deployment (3-10 characters)
- `location`: Your chosen Azure region
- `tags`: Organization-specific tags
- `repositoryUrl`: Your GitHub repository URL (for Static Web App CI/CD)
- `foundryAgentId`: Leave empty initially, update after creating the agent

## Deployment Process

### Step 1: Validate the Template

Always validate before deploying:

```bash
# Development environment
az deployment group validate \
  --resource-group fleet-assistant-dev-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.dev.bicepparam

# Production environment
az deployment group validate \
  --resource-group fleet-assistant-prod-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.prod.bicepparam
```

### Step 2: Preview Changes (What-If)

Use What-If to preview changes without deploying:

```bash
# Development
az deployment group what-if \
  --resource-group fleet-assistant-dev-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.dev.bicepparam

# Production
az deployment group what-if \
  --resource-group fleet-assistant-prod-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.prod.bicepparam
```

### Step 3: Deploy Infrastructure

#### Development Deployment

```bash
az deployment group create \
  --resource-group fleet-assistant-dev-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.dev.bicepparam \
  --name fleet-assistant-dev-$(date +%Y%m%d-%H%M%S)
```

Expected deployment time: 15-25 minutes

#### Production Deployment

```bash
# With GitHub token for Static Web App CI/CD
az deployment group create \
  --resource-group fleet-assistant-prod-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.prod.bicepparam \
  --parameters repositoryToken="$GITHUB_TOKEN" \
  --name fleet-assistant-prod-$(date +%Y%m%d-%H%M%S)
```

Expected deployment time: 20-30 minutes

### Step 4: Capture Deployment Outputs

Save the deployment outputs for reference:

```bash
# Get deployment outputs
az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query properties.outputs

# Save to file
az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query properties.outputs > deployment-outputs.json
```

## Post-Deployment Configuration

### 1. Create AI Foundry Agent

The infrastructure creates the AI Hub and AI Project, but you need to create the agent:

```bash
# Get AI Project details from outputs
AI_PROJECT_NAME=$(az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query 'properties.outputs.aiProjectName.value' \
  --output tsv)

echo "AI Project Name: $AI_PROJECT_NAME"
```

**Using Azure Portal:**
1. Navigate to the Azure AI Foundry portal: https://ai.azure.com
2. Select your AI Project
3. Navigate to "Agents" section
4. Click "Create Agent"
5. Configure the agent with:
   - Name: `FleetAssistantAgent`
   - Model: `gpt-4` or `gpt-4o`
   - Instructions: Use the planning agent prompt from your codebase
6. Note the Agent ID (format: `asst_xxxxxxxxxxxxxxxxx`)

**Update the deployment with Agent ID:**

```bash
# Update dev environment
az deployment group create \
  --resource-group fleet-assistant-dev-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.dev.bicepparam \
  --parameters foundryAgentId="asst_xxxxxxxxxxxxxxxxx" \
  --name fleet-assistant-dev-update-$(date +%Y%m%d-%H%M%S)
```

### 2. Deploy Application Code

#### Backend App Service Deployment

**Option A: Using Azure CLI**

```bash
# Navigate to backend directory
cd src/backend/FleetAssistant.WebApi

# Publish the application
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to App Service
APP_SERVICE_NAME=$(az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query 'properties.outputs.appServiceName.value' \
  --output tsv)

az webapp deployment source config-zip \
  --resource-group fleet-assistant-dev-rg \
  --name $APP_SERVICE_NAME \
  --src deploy.zip
```

**Option B: Using GitHub Actions**

The repository should have a GitHub Actions workflow for automated deployments. Ensure you have:
1. Azure credentials stored as GitHub secrets
2. Workflow enabled in `.github/workflows/backend-deploy.yml`

#### Frontend Static Web App Deployment

If you provided `repositoryUrl` during deployment, the Static Web App automatically:
1. Creates a GitHub Actions workflow in your repository
2. Deploys on every push to the specified branch

**Manual deployment (if not using GitHub integration):**

```bash
# Get Static Web App details
SWA_NAME=$(az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query 'properties.outputs.staticWebAppName.value' \
  --output tsv)

# Get deployment token
SWA_TOKEN=$(az staticwebapp secrets list \
  --name $SWA_NAME \
  --resource-group fleet-assistant-dev-rg \
  --query 'properties.apiKey' \
  --output tsv)

# Deploy using SWA CLI
cd src/frontend/ai-chatbot
npm install -g @azure/static-web-apps-cli
swa deploy ./build --deployment-token $SWA_TOKEN
```

### 3. Configure Custom Domains (Production Only)

#### Front Door Custom Domain

```bash
# Add custom domain to Front Door
az afd custom-domain create \
  --resource-group fleet-assistant-prod-rg \
  --profile-name fleet-fd-prod \
  --custom-domain-name www-example-com \
  --host-name www.example.com \
  --certificate-type ManagedCertificate
```

Follow Azure's documentation for DNS validation and SSL certificate setup.

### 4. Review Security Configuration

#### Network Security

```bash
# Verify private endpoints are connected
az network private-endpoint list \
  --resource-group fleet-assistant-prod-rg \
  --output table

# Check NSG rules
az network nsg list \
  --resource-group fleet-assistant-prod-rg \
  --output table
```

#### WAF Configuration

```bash
# Get WAF policy details
az network front-door waf-policy list \
  --resource-group fleet-assistant-prod-rg \
  --output table

# Review WAF rules
az network front-door waf-policy show \
  --resource-group fleet-assistant-prod-rg \
  --name fleetwafpolicyprod
```

#### Managed Identity Verification

```bash
# List managed identities
az identity list \
  --resource-group fleet-assistant-prod-rg \
  --output table

# Verify RBAC assignments
az role assignment list \
  --resource-group fleet-assistant-prod-rg \
  --output table
```

## Verification and Testing

### 1. Health Check Endpoints

```bash
# Get App Service URL
APP_SERVICE_URL=$(az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query 'properties.outputs.appServiceUrl.value' \
  --output tsv)

# Test health endpoint
curl -I $APP_SERVICE_URL/healthz

# Expected: HTTP/1.1 200 OK
```

### 2. Front Door Connectivity

```bash
# Get Front Door URL
FRONT_DOOR_URL=$(az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query 'properties.outputs.frontDoorUrl.value' \
  --output tsv)

# Test through Front Door
curl -I $FRONT_DOOR_URL/healthz
```

### 3. Static Web App

```bash
# Get Static Web App URL
SWA_URL=$(az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query 'properties.outputs.staticWebAppUrl.value' \
  --output tsv)

# Open in browser
echo "Frontend URL: $SWA_URL"
```

### 4. Application Insights

```bash
# Get Application Insights details
APP_INSIGHTS_NAME=$(az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query 'properties.outputs.applicationInsightsName.value' \
  --output tsv)

# View recent traces
az monitor app-insights metrics show \
  --app $APP_INSIGHTS_NAME \
  --resource-group fleet-assistant-dev-rg \
  --metric requests/count
```

### 5. Integration Testing

Run the integration test script:

```bash
# From repository root
cd testing
node test-webapi-chat.js
```

## Troubleshooting

### Common Issues

#### 1. Deployment Fails with Quota Exceeded

**Error:** "Operation could not be completed as it results in exceeding quota"

**Solution:**
```bash
# Request quota increase
az support tickets create \
  --title "Increase quota for Premium App Service Plan" \
  --severity minimal \
  --contact-first-name "Your Name" \
  --contact-last-name "Your Last Name" \
  --contact-email "your@email.com" \
  --description "Need quota increase for P1v3 App Service Plan in East US"
```

#### 2. AI Foundry Connection Fails

**Symptoms:** Backend health check shows AI Foundry connectivity issues

**Solution:**
1. Verify managed identity has correct RBAC roles
2. Check private endpoint DNS resolution
3. Ensure firewall rules allow traffic

```bash
# Test from App Service
az webapp ssh --resource-group fleet-assistant-dev-rg --name fleet-api-dev

# Inside App Service shell
nslookup <ai-services-endpoint>
```

#### 3. Front Door Not Routing Traffic

**Solution:**
1. Verify origin health in Azure Portal
2. Check WAF policies aren't blocking traffic
3. Review Front Door routing rules

```bash
# Check Front Door health
az afd endpoint list \
  --profile-name fleet-fd-dev \
  --resource-group fleet-assistant-dev-rg \
  --query '[].deploymentStatus'
```

#### 4. Static Web App Build Fails

**Solution:**
1. Check GitHub Actions workflow logs
2. Verify Node.js version compatibility
3. Ensure build command is correct in workflow

### Debug Commands

```bash
# View deployment operation details
az deployment group show \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query 'properties.error'

# List all deployment operations
az deployment operation group list \
  --resource-group fleet-assistant-dev-rg \
  --name <deployment-name> \
  --query '[?properties.provisioningState!=`Succeeded`]'

# View activity log
az monitor activity-log list \
  --resource-group fleet-assistant-dev-rg \
  --start-time 2024-01-01T00:00:00Z \
  --query '[?level==`Error`]'
```

## Cost Management

### Development Environment

Estimated monthly cost: **$100-$200**
- Basic App Service Plan: ~$55/month
- AI Services S0: ~$0 (pay per use)
- Storage: ~$5/month
- Minimal Front Door usage: ~$35/month
- Log Analytics: ~$5/month (with daily cap)

### Production Environment

Estimated monthly cost: **$500-$1,500**
- Premium App Service Plan (P1v3): ~$210/month
- AI Services usage: Variable based on usage
- Front Door Premium: ~$35/month + $0.03/GB
- Azure Firewall: ~$180/month
- Log Analytics: ~$50-100/month
- Static Web App Standard: ~$10/month

### Cost Optimization Tips

1. **Use autoscaling wisely**: Configure scale-down during off-hours
2. **Enable daily cap on Log Analytics**: For non-production environments
3. **Review AI Services usage**: Monitor token consumption
4. **Use Azure Cost Management**: Set up budgets and alerts

```bash
# Set up cost alert
az consumption budget create \
  --resource-group fleet-assistant-dev-rg \
  --budget-name dev-budget \
  --amount 200 \
  --time-grain monthly \
  --threshold 80 \
  --notification-enabled true \
  --contact-emails your@email.com
```

## Next Steps

1. **Configure CI/CD**: Set up automated deployments via GitHub Actions
2. **Set up monitoring alerts**: Configure Application Insights alerts for critical metrics
3. **Implement backup strategy**: Configure backup for databases (when added)
4. **Document runbooks**: Create operational runbooks for common tasks
5. **Conduct security review**: Perform security assessment and penetration testing
6. **Plan disaster recovery**: Document and test DR procedures

## Additional Resources

- [Azure Reliable Web App Pattern](https://learn.microsoft.com/en-us/azure/architecture/web-apps/guides/enterprise-app-patterns/reliable-web-app/dotnet/guidance)
- [Azure Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure Front Door Documentation](https://learn.microsoft.com/en-us/azure/frontdoor/)
- [Azure AI Foundry Documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/)
- [Fleet Assistant Architecture Guide](../docs/MULTI_AGENT_INTEGRATION_GUIDE.md)

## Support

For issues or questions:
1. Check the [Troubleshooting](#troubleshooting) section
2. Review application logs in Application Insights
3. Consult Azure documentation
4. Open a GitHub issue with:
   - Environment (dev/staging/prod)
   - Deployment name and timestamp
   - Error messages and logs
   - Steps to reproduce

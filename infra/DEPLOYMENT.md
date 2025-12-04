# Fleet Assistant - Azure Deployment Guide

This guide provides step-by-step instructions for deploying the Fleet Assistant multi-agent application to Azure using Bicep Infrastructure-as-Code templates following the Microsoft Reliable Web App (RWA) pattern.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Architecture Overview](#architecture-overview)
- [Pre-Deployment Steps](#pre-deployment-steps)
- [Deployment Steps](#deployment-steps)
- [Post-Deployment Configuration](#post-deployment-configuration)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)
- [Cleanup](#cleanup)

## Prerequisites

### Required Tools

- **Azure CLI** (version 2.50.0 or later)
  ```bash
  az --version
  # Install or upgrade: https://docs.microsoft.com/cli/azure/install-azure-cli
  ```

- **Bicep CLI** (version 0.20.0 or later)
  ```bash
  az bicep version
  # Install: az bicep install
  # Upgrade: az bicep upgrade
  ```

- **Azure Subscription** with appropriate permissions:
  - Contributor or Owner role on the subscription
  - Ability to create resource groups and resources
  - Ability to assign RBAC roles

### Azure Account Setup

1. **Log in to Azure**:
   ```bash
   az login
   ```

2. **Set the subscription**:
   ```bash
   # List subscriptions
   az account list --output table
   
   # Set active subscription
   az account set --subscription "YOUR_SUBSCRIPTION_ID"
   ```

3. **Verify your account**:
   ```bash
   az account show
   ```

## Architecture Overview

The Fleet Assistant infrastructure includes:

- **Networking**: Hub-and-spoke VNet topology with private endpoints
- **Compute**: App Service (backend) and Static Web App (frontend)
- **AI**: Azure AI Foundry with AI Hub, Project, and Services
- **Data**: Azure SQL Database and Blob Storage
- **Security**: Azure Front Door with WAF, managed identities, NSGs
- **Monitoring**: Application Insights and Log Analytics

See [NETWORK_ARCHITECTURE.md](./NETWORK_ARCHITECTURE.md) for detailed network diagrams.

## Pre-Deployment Steps

### 1. Clone the Repository

```bash
git clone https://github.com/kwkraus/fleet-assistant-multi-agent-app.git
cd fleet-assistant-multi-agent-app
```

### 2. Choose Your Environment

Select the appropriate parameter file for your deployment:

- **Development**: `infra/parameters.dev.bicepparam` (existing)
- **Staging**: Copy `infra/parameters.staging.bicepparam.example` → `parameters.staging.bicepparam`
- **Production**: Copy `infra/parameters.prod.bicepparam.example` → `parameters.prod.bicepparam`

### 3. Generate SQL Credentials

Create secure SQL administrator credentials:

```bash
# Generate a secure password (Linux/macOS)
SQL_PASSWORD=$(openssl rand -base64 32)
echo "SQL Password: $SQL_PASSWORD"

# Or use PowerShell (Windows)
$SQL_PASSWORD = -join ((65..90) + (97..122) + (48..57) + (33, 35, 36, 37, 42, 43) | Get-Random -Count 20 | ForEach-Object {[char]$_})
Write-Host "SQL Password: $SQL_PASSWORD"
```

**Important**: Store this password securely (e.g., Azure Key Vault, password manager).

### 4. Validate Bicep Templates

```bash
cd infra

# Validate the main template
az bicep build --file main.bicep

# Check for errors
echo $?  # Should return 0 if successful
```

## Deployment Steps

### Option 1: Deploy to Development Environment

```bash
cd infra

# Deploy with parameter file
az deployment sub create \
  --location eastus \
  --template-file main.bicep \
  --parameters parameters.dev.bicepparam \
  --parameters sqlAdminPassword='YOUR_SECURE_PASSWORD_HERE' \
  --name fleet-dev-deployment-$(date +%Y%m%d-%H%M%S)
```

### Option 2: Deploy to Staging/Production

```bash
cd infra

# For staging
az deployment sub create \
  --location eastus \
  --template-file main.bicep \
  --parameters parameters.staging.bicepparam \
  --parameters sqlAdminPassword='YOUR_SECURE_PASSWORD_HERE' \
  --name fleet-staging-deployment-$(date +%Y%m%d-%H%M%S)

# For production
az deployment sub create \
  --location eastus \
  --template-file main.bicep \
  --parameters parameters.prod.bicepparam \
  --parameters sqlAdminPassword='YOUR_SECURE_PASSWORD_HERE' \
  --name fleet-prod-deployment-$(date +%Y%m%d-%H%M%S)
```

### Option 3: What-If Deployment (Preview Changes)

Preview changes before deploying:

```bash
az deployment sub what-if \
  --location eastus \
  --template-file main.bicep \
  --parameters parameters.dev.bicepparam \
  --parameters sqlAdminPassword='YOUR_SECURE_PASSWORD_HERE'
```

### Deployment Duration

- **Development**: ~15-20 minutes
- **Staging/Production**: ~20-30 minutes (depends on SKUs and autoscaling configuration)

### Monitoring Deployment Progress

```bash
# Watch deployment in the Azure Portal
az deployment sub show \
  --name fleet-dev-deployment-TIMESTAMP \
  --query properties.provisioningState

# View deployment outputs
az deployment sub show \
  --name fleet-dev-deployment-TIMESTAMP \
  --query properties.outputs
```

## Post-Deployment Configuration

### 1. Retrieve Deployment Outputs

```bash
# Save all outputs to a file
az deployment sub show \
  --name fleet-dev-deployment-TIMESTAMP \
  --query properties.outputs > deployment-outputs.json

# View specific outputs
az deployment sub show \
  --name fleet-dev-deployment-TIMESTAMP \
  --query properties.outputs.appServiceUrl.value

az deployment sub show \
  --name fleet-dev-deployment-TIMESTAMP \
  --query properties.outputs.frontDoorEndpoint.value
```

### 2. Configure AI Foundry Agents

After deployment, configure the AI Foundry agents in the Azure Portal:

1. Navigate to the AI Foundry Project resource
2. Create or configure your fleet management agents:
   - **Fuel Agent**: Fuel efficiency monitoring
   - **Maintenance Agent**: Predictive maintenance
   - **Safety Agent**: Safety event tracking
   - **Planning Agent**: Multi-agent orchestration
3. Note the **Agent ID** for each agent
4. Update the App Service app settings if the agent IDs differ from defaults

### 3. Deploy Application Code

#### Backend Deployment

```bash
cd src/backend/FleetAssistant.WebApi

# Build the application
dotnet publish -c Release -o ./publish

# Deploy to App Service
az webapp deployment source config-zip \
  --resource-group fleet-rg-dev \
  --name fleet-api-dev-SUFFIX \
  --src ./publish.zip
```

#### Frontend Deployment

The Static Web App can be deployed via GitHub Actions:

1. Get the deployment token:
   ```bash
   az staticwebapp secrets list \
     --name fleet-frontend-dev \
     --resource-group fleet-rg-dev \
     --query properties.apiKey -o tsv
   ```

2. Add the token as a GitHub secret: `AZURE_STATIC_WEB_APP_API_TOKEN`

3. GitHub Actions will automatically deploy on push to main branch

Or deploy manually:

```bash
cd src/frontend/ai-chatbot

# Build the application
npm install
npm run build

# Deploy using Static Web Apps CLI
npx @azure/static-web-apps-cli deploy \
  --deployment-token YOUR_DEPLOYMENT_TOKEN \
  --app-location . \
  --output-location .next
```

### 4. Configure Custom Domains (Production)

1. Add custom domain to Front Door:
   ```bash
   az afd custom-domain create \
     --resource-group fleet-rg-prod \
     --profile-name fleet-fd-prod-SUFFIX \
     --custom-domain-name fleet-custom-domain \
     --host-name fleet.yourdomain.com \
     --minimum-tls-version TLS12
   ```

2. Update DNS records as instructed by Azure

3. Enable HTTPS:
   ```bash
   az afd custom-domain update \
     --resource-group fleet-rg-prod \
     --profile-name fleet-fd-prod-SUFFIX \
     --custom-domain-name fleet-custom-domain \
     --certificate-type ManagedCertificate
   ```

## Verification

### 1. Verify Infrastructure Deployment

```bash
# Check resource group
az group show --name fleet-rg-dev

# List all resources
az resource list --resource-group fleet-rg-dev --output table

# Check App Service status
az webapp show \
  --name fleet-api-dev-SUFFIX \
  --resource-group fleet-rg-dev \
  --query state
```

### 2. Verify Application Health

```bash
# Backend health check
curl https://fleet-api-dev-SUFFIX.azurewebsites.net/healthz

# Expected response:
# {
#   "status": "Healthy",
#   "checks": [
#     {"name": "EntityFramework", "status": "Healthy"},
#     {"name": "FoundryHealthCheck", "status": "Healthy"}
#   ]
# }
```

### 3. Test Front Door

```bash
# Test via Front Door endpoint
curl https://fleet-fd-dev-SUFFIX-randomstring.azurefd.net/healthz

# Test frontend
curl https://fleet-fd-dev-SUFFIX-randomstring.azurefd.net/
```

### 4. Verify Networking

```bash
# Check VNet peering
az network vnet peering list \
  --resource-group fleet-rg-dev \
  --vnet-name fleet-spoke-vnet-dev \
  --output table

# Check private endpoints
az network private-endpoint list \
  --resource-group fleet-rg-dev \
  --output table
```

### 5. Test SSE Streaming

Test the chat endpoint with SSE streaming:

```bash
curl -X POST https://fleet-fd-dev-SUFFIX.azurefd.net/api/chat \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"What is the fuel efficiency of vehicle ABC123?"}]}' \
  -N
```

## Troubleshooting

### Common Issues

#### 1. Deployment Fails with "InsufficientPermissions"

**Solution**: Ensure your Azure account has Contributor or Owner role:

```bash
az role assignment list --assignee YOUR_USER_EMAIL --output table
```

#### 2. Private Endpoint Connection Fails

**Solution**: Verify private DNS zones are linked to the VNet:

```bash
az network private-dns link vnet list \
  --resource-group fleet-rg-dev \
  --zone-name privatelink.blob.core.windows.net \
  --output table
```

#### 3. App Service Can't Connect to SQL Database

**Solution**: Check firewall rules and VNet integration:

```bash
# Allow Azure services
az sql server firewall-rule create \
  --resource-group fleet-rg-dev \
  --server fleet-sql-dev-SUFFIX \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

#### 4. Foundry Agent Endpoint Not Accessible

**Solution**: Verify managed identity has correct RBAC role:

```bash
# Assign Cognitive Services User role
az role assignment create \
  --assignee YOUR_MANAGED_IDENTITY_PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope /subscriptions/SUBSCRIPTION_ID/resourceGroups/fleet-rg-dev/providers/Microsoft.CognitiveServices/accounts/fleet-aiservices-dev
```

#### 5. Front Door Health Probe Failing

**Solution**: Verify App Service health check endpoint:

```bash
# Test health endpoint directly
curl https://fleet-api-dev-SUFFIX.azurewebsites.net/healthz

# Check Front Door health probe logs
az monitor activity-log list \
  --resource-group fleet-rg-dev \
  --offset 1h
```

### Get Deployment Logs

```bash
# View deployment operation details
az deployment sub operation list \
  --name fleet-dev-deployment-TIMESTAMP \
  --query "[?properties.provisioningState=='Failed']"

# View resource-specific logs
az monitor activity-log list \
  --resource-group fleet-rg-dev \
  --offset 1h \
  --max-events 50
```

## Cleanup

### Delete Resources

**Warning**: This will permanently delete all resources and data.

```bash
# Delete the entire resource group
az group delete --name fleet-rg-dev --yes --no-wait

# Verify deletion
az group exists --name fleet-rg-dev
```

### Delete Specific Resources

```bash
# Delete Front Door (global resource)
az afd profile delete \
  --profile-name fleet-fd-dev-SUFFIX \
  --resource-group fleet-rg-dev

# Delete Static Web App
az staticwebapp delete \
  --name fleet-frontend-dev \
  --resource-group fleet-rg-dev
```

## Next Steps

- Review [SECURITY_CONFIGURATION.md](./SECURITY_CONFIGURATION.md) for security best practices
- Set up monitoring and alerts: [MONITORING_SETUP.md](./MONITORING_SETUP.md)
- Configure CI/CD pipelines for automated deployments
- Set up Azure Key Vault for secrets management
- Configure backup and disaster recovery policies

## Additional Resources

- [Microsoft Reliable Web App Pattern for .NET](https://learn.microsoft.com/azure/architecture/web-apps/guides/enterprise-app-patterns/reliable-web-app/dotnet/guidance)
- [Azure Front Door Documentation](https://docs.microsoft.com/azure/frontdoor/)
- [Azure AI Foundry Documentation](https://docs.microsoft.com/azure/ai-services/)
- [Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)

## Support

For issues or questions:
1. Check the [Troubleshooting](#troubleshooting) section
2. Review Azure Portal logs and metrics
3. Open an issue on GitHub with deployment logs and error messages

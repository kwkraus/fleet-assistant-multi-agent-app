# Fleet Assistant Infrastructure

This directory contains Bicep Infrastructure-as-Code (IaC) templates for deploying the Fleet Assistant multi-agent application to Azure following the **Microsoft Reliable Web App (RWA) pattern for .NET**.

## 📁 Directory Structure

```
infra/
├── main.bicep                              # Main orchestration template
├── modules/                                # Bicep modules
│   ├── managed-identity.bicep             # User-assigned managed identity
│   ├── monitoring.bicep                    # Application Insights, Log Analytics
│   ├── networking.bicep                    # Hub-spoke VNets, NSGs, private DNS
│   ├── security.bicep                      # Front Door, WAF policy
│   ├── ai-foundry.bicep                   # Azure AI Foundry resources
│   ├── app-service.bicep                   # App Service Plan and App Service
│   ├── static-web-app.bicep               # Static Web App for frontend
│   ├── database.bicep                      # SQL Server and Database
│   └── storage.bicep                       # Storage Account and Blob
├── parameters.dev.bicepparam               # Development environment
├── parameters.staging.bicepparam.example   # Staging template
├── parameters.prod.bicepparam.example      # Production template
├── DEPLOYMENT.md                           # Step-by-step deployment guide
├── NETWORK_ARCHITECTURE.md                 # Network topology and diagrams
├── SECURITY_CONFIGURATION.md               # Security best practices
├── MONITORING_SETUP.md                     # Monitoring and alerting guide
└── README.md                               # This file
```

## 🚀 Quick Start

### Prerequisites

- Azure CLI (v2.50.0+): `az --version`
- Bicep CLI (v0.20.0+): `az bicep version`
- Azure subscription with Contributor or Owner role
- Authenticated Azure CLI session: `az login`

### Deploy Development Environment

```bash
# Set your subscription
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# Deploy to development
cd infra
az deployment sub create \
  --location eastus \
  --template-file main.bicep \
  --parameters parameters.dev.bicepparam \
  --parameters sqlAdminPassword='YourSecurePassword123!' \
  --name fleet-dev-deployment-$(date +%Y%m%d-%H%M%S)
```

### Deploy Production Environment

```bash
# Copy template and customize
cp parameters.prod.bicepparam.example parameters.prod.bicepparam
# Edit parameters.prod.bicepparam with your settings

# Deploy to production
az deployment sub create \
  --location eastus \
  --template-file main.bicep \
  --parameters parameters.prod.bicepparam \
  --parameters sqlAdminPassword='YourSecurePassword123!' \
  --name fleet-prod-deployment-$(date +%Y%m%d-%H%M%S)
```

## 🏗️ Architecture

### Components Deployed

| Component | Purpose | RWA Pattern Alignment |
|-----------|---------|----------------------|
| **Azure Front Door** | Global load balancer, Layer 7 routing | ✅ Reliability - Multi-region ready |
| **Web Application Firewall** | OWASP Top 10, DDoS protection | ✅ Security - Perimeter defense |
| **Hub VNet** | Centralized security, Azure Firewall | ✅ Security - Network isolation |
| **Spoke VNet** | Application resources isolation | ✅ Security - Segmentation |
| **Private Endpoints** | Secure PaaS connectivity | ✅ Security - Zero trust |
| **App Service** | Backend ASP.NET Core API | ✅ Performance - Autoscaling |
| **Static Web App** | Frontend Next.js 15 | ✅ Performance - Global CDN |
| **Azure AI Foundry** | Multi-agent AI system | ✅ Innovation - AI integration |
| **SQL Database** | Persistent data storage | ✅ Reliability - Backups, HA |
| **Blob Storage** | Document management | ✅ Cost - Efficient storage |
| **Managed Identity** | Credential-free auth | ✅ Security - No secrets |
| **Application Insights** | Telemetry and monitoring | ✅ Operations - Observability |

### Network Topology

```
Internet → Front Door (WAF) → Static Web App (Frontend)
                            → App Service (Backend)
                                │
                                ├─→ SQL Database (Private Endpoint)
                                ├─→ Blob Storage (Private Endpoint)
                                ├─→ AI Foundry (Managed Identity)
                                └─→ App Insights (Public)

Hub VNet (10.0.0.0/16)
├── AzureFirewallSubnet (Future)
└── AzureBastionSubnet (Future)
    │
    │ VNet Peering
    ▼
Spoke VNet (10.1.0.0/16)
├── AppServiceSubnet (VNet integration)
├── PrivateEndpointSubnet (Private endpoints)
└── DatabaseSubnet (Service endpoints)
```

## 📦 Modules

### [managed-identity.bicep](modules/managed-identity.bicep)

Creates a user-assigned managed identity for shared permissions across resources.

**Parameters:**
- `identityName`: Name of the managed identity
- `location`: Azure region
- `tags`: Resource tags

**Outputs:**
- `identityId`: Full resource ID
- `clientId`: Application ID for RBAC
- `principalId`: Principal ID for role assignments

### [monitoring.bicep](modules/monitoring.bicep)

Provisions Log Analytics workspace and Application Insights for telemetry.

**Parameters:**
- `workspaceName`: Log Analytics workspace name
- `appInsightsName`: Application Insights instance name
- `retentionInDays`: Data retention (30-730 days)

**Outputs:**
- `appInsightsConnectionString`: Connection string for apps
- `appInsightsInstrumentationKey`: Legacy key
- `workspaceId`: Workspace resource ID

### [networking.bicep](modules/networking.bicep)

Deploys hub-and-spoke VNet topology with NSGs and private DNS zones.

**Key Features:**
- Hub VNet with firewall and bastion subnets
- Spoke VNet with app, private endpoint, and database subnets
- VNet peering (hub ↔ spoke)
- NSG rules (least privilege)
- Private DNS zones for blob, SQL, and App Service

**Outputs:**
- VNet IDs, subnet IDs, NSG IDs, private DNS zone IDs

### [security.bicep](modules/security.bicep)

Configures Azure Front Door with WAF policy.

**Key Features:**
- Front Door Standard/Premium SKU
- WAF with Microsoft managed rules (OWASP, Bot Protection)
- Rate limiting custom rules
- Origin groups for backend and frontend
- Health probes and routing rules

**Outputs:**
- `frontDoorEndpoint`: Public HTTPS endpoint
- `frontDoorId`: Resource ID

### [ai-foundry.bicep](modules/ai-foundry.bicep)

Provisions Azure AI Foundry resources for multi-agent system.

**Key Features:**
- AI Hub (Machine Learning workspace)
- AI Project (Project workspace)
- AI Services (Cognitive Services account)
- RBAC role assignment (Cognitive Services User)

**Outputs:**
- `agentEndpoint`: Foundry API endpoint
- `agentId`: Default agent ID (placeholder)

### [app-service.bicep](modules/app-service.bicep)

Deploys App Service Plan and App Service with SSE streaming support.

**Key Features:**
- Linux-based App Service Plan
- HTTP/2 enabled for SSE
- VNet integration
- Private endpoint
- Autoscaling rules (CPU, memory)
- App settings (Foundry, SQL, Storage, App Insights)

**Outputs:**
- `appServiceUrl`: Public URL
- `appServiceFqdn`: Fully qualified domain name

### [static-web-app.bicep](modules/static-web-app.bicep)

Provisions Static Web App for Next.js 15 frontend.

**Key Features:**
- Free or Standard SKU
- Automatic backend URL configuration
- GitHub Actions integration (optional)

**Outputs:**
- `defaultHostname`: Static Web App URL
- `deploymentToken`: For GitHub Actions

### [database.bicep](modules/database.bicep)

Creates SQL Server and SQL Database with private endpoint.

**Key Features:**
- SQL Server 12.0 (latest)
- TLS 1.2 minimum
- Private endpoint connectivity
- Public access disabled (production)

**Outputs:**
- `connectionString`: SQL connection string (includes credentials)
- `sqlServerFqdn`: Server FQDN

### [storage.bicep](modules/storage.bicep)

Provisions Storage Account with blob container and private endpoint.

**Key Features:**
- StorageV2 (general-purpose v2)
- HTTPS only, TLS 1.2 minimum
- Blob container with private access
- Private endpoint for blob service

**Outputs:**
- `connectionString`: Storage connection string
- `blobEndpoint`: Blob service endpoint

## 🔧 Configuration

### Environment-Specific Parameters

| Parameter | Development | Staging | Production |
|-----------|-------------|---------|------------|
| **App Service SKU** | B1 (Basic) | S1 (Standard) | P1v3 (Premium) |
| **SQL Database SKU** | Basic (5 DTU) | S0 (10 DTU) | S1 (20 DTU) |
| **Autoscaling** | Disabled | Enabled (1-3) | Enabled (2-10) |
| **Min Instances** | 1 | 1 | 2 |
| **Front Door SKU** | Standard | Standard | Standard/Premium |
| **VNet Address** | 10.0-1.x.x | 10.50-51.x.x | 10.100-101.x.x |

### Customization

Edit parameter files to customize:
- Resource names
- SKU sizes
- Autoscaling thresholds
- Network address spaces
- Feature flags (e.g., snapshot debugging)

## 🔐 Security

### Secrets Management

**Development**: App Settings (acceptable for testing)

**Production**: Azure Key Vault (recommended)

**Never commit:**
- SQL passwords
- Storage connection strings
- API keys
- Certificates

### Network Security

- All PaaS services use private endpoints
- NSG rules follow least-privilege
- Public access disabled for SQL Database and Storage
- WAF protects against common exploits
- HTTPS enforced on all services

### Identity & Access

- Managed identity for service-to-service auth
- RBAC role assignments (Cognitive Services User, SQL DB Contributor)
- No credentials in code or configuration

See [SECURITY_CONFIGURATION.md](SECURITY_CONFIGURATION.md) for details.

## 📊 Monitoring

### Built-in Monitoring

- Application Insights for telemetry
- Log Analytics for centralized logging
- Diagnostic settings enabled
- Health checks configured

### Custom Metrics

Track:
- Foundry Agent request latency
- Foundry Agent success/failure rates
- SSE streaming metrics
- Database query performance

See [MONITORING_SETUP.md](MONITORING_SETUP.md) for alert configuration.

## ✅ Validation

### Pre-Deployment

```bash
# Validate template syntax
bicep build main.bicep

# Preview changes (what-if)
az deployment sub what-if \
  --location eastus \
  --template-file main.bicep \
  --parameters parameters.dev.bicepparam \
  --parameters sqlAdminPassword='YourPassword123!'
```

### Post-Deployment

```bash
# Check resource group
az group show --name fleet-rg-dev

# List all resources
az resource list --resource-group fleet-rg-dev --output table

# Test health endpoint
curl https://$(az webapp show --name fleet-api-dev-SUFFIX --resource-group fleet-rg-dev --query defaultHostName -o tsv)/healthz
```

## 🐛 Troubleshooting

### Common Issues

1. **Storage account name too long**
   - Bicep uses unique suffixes for global uniqueness
   - Names are automatically shortened to fit 24-char limit

2. **Private endpoint connection fails**
   - Verify private DNS zone is linked to VNet
   - Check NSG rules allow traffic from source subnet

3. **Front Door health probe fails**
   - Ensure App Service `/healthz` returns 200 OK
   - Verify NSG allows inbound HTTPS

4. **SQL connection timeout**
   - Check private endpoint provisioning state
   - Verify connection string uses private FQDN

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed troubleshooting.

## 📚 Documentation

- **[DEPLOYMENT.md](DEPLOYMENT.md)** - Complete deployment walkthrough
- **[NETWORK_ARCHITECTURE.md](NETWORK_ARCHITECTURE.md)** - Network diagrams and traffic flow
- **[SECURITY_CONFIGURATION.md](SECURITY_CONFIGURATION.md)** - Security hardening guide
- **[MONITORING_SETUP.md](MONITORING_SETUP.md)** - Alerting and dashboards

## 🔄 CI/CD Integration

### GitHub Actions (Example)

```yaml
name: Deploy Infrastructure

on:
  push:
    branches: [main]
    paths:
      - 'infra/**'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy Bicep
        run: |
          az deployment sub create \
            --location eastus \
            --template-file infra/main.bicep \
            --parameters infra/parameters.prod.bicepparam \
            --parameters sqlAdminPassword=${{ secrets.SQL_ADMIN_PASSWORD }}
```

## 🧹 Cleanup

**Warning**: This permanently deletes all resources and data.

```bash
# Delete entire resource group
az group delete --name fleet-rg-dev --yes --no-wait

# Verify deletion
az group exists --name fleet-rg-dev
```

## 📝 Best Practices Implemented

✅ Modular design (separate modules for concerns)
✅ Parameterization for multiple environments
✅ Hub-and-spoke network topology
✅ Private endpoints for all PaaS services
✅ Managed identities (no secrets in code)
✅ WAF with OWASP protection
✅ Autoscaling based on metrics
✅ Health checks for reliability
✅ Comprehensive monitoring and logging
✅ Inline documentation in templates
✅ Outputs for verification

## 🤝 Contributing

When modifying templates:
1. Validate with `bicep build`
2. Test with `az deployment sub what-if`
3. Update documentation
4. Use semantic versioning for major changes

## 📖 References

- [Microsoft Reliable Web App Pattern](https://learn.microsoft.com/azure/architecture/web-apps/guides/enterprise-app-patterns/reliable-web-app/dotnet/guidance)
- [Azure Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure Front Door](https://docs.microsoft.com/azure/frontdoor/)
- [Azure AI Foundry](https://docs.microsoft.com/azure/ai-services/)
- [Private Link](https://docs.microsoft.com/azure/private-link/)

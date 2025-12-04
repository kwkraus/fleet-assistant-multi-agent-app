# Fleet Assistant - Security Configuration Guide

This guide provides detailed information about the security architecture and configuration of the Fleet Assistant infrastructure deployment.

## Table of Contents

- [Security Architecture Overview](#security-architecture-overview)
- [Network Security](#network-security)
- [Identity and Access Management](#identity-and-access-management)
- [Application Security](#application-security)
- [Data Protection](#data-protection)
- [Monitoring and Compliance](#monitoring-and-compliance)
- [Security Hardening Checklist](#security-hardening-checklist)

## Security Architecture Overview

The Fleet Assistant infrastructure implements defense-in-depth security following Microsoft's Security Best Practices:

### Security Layers

1. **Perimeter Security**: Azure Front Door with WAF
2. **Network Security**: Hub-spoke topology, NSGs, Azure Firewall
3. **Access Control**: Private endpoints, managed identities, RBAC
4. **Application Security**: HTTPS enforcement, secure headers, input validation
5. **Data Security**: Encryption at rest and in transit
6. **Monitoring**: Comprehensive logging and threat detection

## Network Security

### Hub-Spoke Network Topology

```
┌──────────────────────────────────────────────────────────┐
│                      Azure Front Door                     │
│              (Global Entry Point + WAF)                   │
└────────────────────────┬─────────────────────────────────┘
                         │
┌────────────────────────┼─────────────────────────────────┐
│                   Hub VNet (10.0.0.0/16)                  │
│  ┌──────────────────────────────────────────────────┐    │
│  │          Azure Firewall (10.0.1.0/24)            │    │
│  │  • Network traffic inspection                    │    │
│  │  • Application rules enforcement                 │    │
│  └──────────────────────────────────────────────────┘    │
└────────────────────────┬─────────────────────────────────┘
                         │ VNet Peering
┌────────────────────────┼─────────────────────────────────┐
│                 Spoke VNet (10.1.0.0/16)                  │
│  ┌──────────────────────────────────────────────────┐    │
│  │     App Service Subnet (10.1.1.0/24)             │    │
│  │  • Backend API (VNet integrated)                 │    │
│  │  • Outbound through VNet                         │    │
│  └──────────────────────────────────────────────────┘    │
│  ┌──────────────────────────────────────────────────┐    │
│  │   Private Endpoint Subnet (10.1.2.0/24)          │    │
│  │  • AI Services private endpoint                  │    │
│  │  • Storage private endpoint                      │    │
│  │  • Key Vault private endpoint (future)           │    │
│  └──────────────────────────────────────────────────┘    │
│  ┌──────────────────────────────────────────────────┐    │
│  │       Data Subnet (10.1.3.0/24)                  │    │
│  │  • SQL Database (future)                         │    │
│  │  • Restricted network access                     │    │
│  └──────────────────────────────────────────────────┘    │
└───────────────────────────────────────────────────────────┘
```

### Network Security Groups (NSGs)

#### App Service Subnet NSG

**Inbound Rules:**
- Allow HTTPS from Azure Front Door (`AzureFrontDoor.Backend` service tag)
- Allow HTTP from Azure Front Door (redirects to HTTPS)
- Allow VNet internal traffic
- Deny all other inbound traffic

**Verification:**
```bash
az network nsg show \
  --resource-group fleet-assistant-prod-rg \
  --name fleet-app-nsg-prod \
  --query 'securityRules[].{Name:name, Priority:priority, Access:access, Direction:direction}'
```

#### Private Endpoint Subnet NSG

**Inbound Rules:**
- Allow all VNet internal traffic
- Deny all Internet inbound

**Purpose:** Enable private endpoint connectivity while blocking public access.

#### Data Subnet NSG

**Inbound Rules:**
- Allow SQL traffic (1433) from App Service subnet only
- Allow HTTPS (443) from App Service subnet only
- Deny all Internet inbound

**Purpose:** Isolate data tier and restrict access to application tier only.

### Azure Firewall Rules

#### Network Rules

1. **Allow Azure Monitor**
   - Source: Spoke VNet (10.1.0.0/16)
   - Destination: AzureMonitor service tag
   - Port: 443
   - Purpose: Application Insights telemetry

2. **Allow Azure Storage**
   - Source: Spoke VNet
   - Destination: Storage service tag
   - Port: 443
   - Purpose: Blob storage access

3. **Allow Azure Key Vault**
   - Source: Spoke VNet
   - Destination: AzureKeyVault service tag
   - Port: 443
   - Purpose: Secrets retrieval

#### Application Rules

1. **Allow Azure APIs**
   - FQDN: *.azure.com, *.microsoft.com, *.windows.net, *.ai.azure.com
   - Protocol: HTTPS (443)
   - Purpose: Azure service API calls

**Review Firewall Logs:**
```bash
az monitor log-analytics query \
  --workspace <workspace-id> \
  --analytics-query "AzureDiagnostics | where Category == 'AzureFirewallNetworkRule' | take 100"
```

### Private Endpoints

All PaaS services use private endpoints to eliminate public exposure:

| Service | Private Endpoint | Private DNS Zone |
|---------|------------------|------------------|
| AI Services | Yes | privatelink.cognitiveservices.azure.com |
| Storage Account | Yes | privatelink.blob.core.windows.net |
| App Service | Yes | privatelink.azurewebsites.net |
| SQL Database (future) | Yes | privatelink.database.windows.net |
| Key Vault (future) | Yes | privatelink.vaultcore.azure.net |

**Verify Private Endpoint Connectivity:**
```bash
# List private endpoints
az network private-endpoint list \
  --resource-group fleet-assistant-prod-rg \
  --output table

# Test DNS resolution from App Service
az webapp ssh --resource-group fleet-assistant-prod-rg --name fleet-api-prod
# Inside shell:
nslookup <ai-services-endpoint>
# Should resolve to private IP (10.1.2.x)
```

## Identity and Access Management

### Managed Identities

The deployment uses **user-assigned managed identities** for secure service-to-service authentication:

#### App Service Managed Identity

**Assigned Roles:**
- **Cognitive Services OpenAI User** on AI Services
- **Cognitive Services User** on AI Services
- **Storage Blob Data Contributor** on Storage Account
- **Azure Machine Learning Workspace Connection Secrets Reader** on AI Project

**Purpose:** Enables backend API to:
- Call Azure AI Foundry agents without API keys
- Access blob storage for documents
- Read AI project connection strings

**Verify Role Assignments:**
```bash
APP_IDENTITY_ID=$(az identity show \
  --resource-group fleet-assistant-prod-rg \
  --name fleet-app-identity-prod \
  --query principalId --output tsv)

az role assignment list \
  --assignee $APP_IDENTITY_ID \
  --all \
  --output table
```

#### AI Foundry Managed Identity

**Assigned Roles:**
- System-assigned on AI Hub and AI Project
- Used internally by Azure AI services

### RBAC Best Practices

1. **Principle of Least Privilege**: Grant only necessary permissions
2. **Use Managed Identities**: Avoid storing credentials in app settings
3. **Regular Access Reviews**: Audit role assignments quarterly
4. **Conditional Access**: Implement for privileged accounts

**Audit RBAC Assignments:**
```bash
# Get all role assignments in resource group
az role assignment list \
  --resource-group fleet-assistant-prod-rg \
  --include-inherited \
  --output table

# Find overly permissive roles
az role assignment list \
  --resource-group fleet-assistant-prod-rg \
  --query "[?roleDefinitionName=='Owner' || roleDefinitionName=='Contributor']"
```

## Application Security

### Azure Front Door + WAF

#### WAF Policy Configuration

**Managed Rule Sets:**
1. **Microsoft Default Rule Set 2.1**
   - OWASP Top 10 protection
   - Common Vulnerabilities and Exposures (CVE)
   - Protocol attacks prevention

2. **Bot Manager Rule Set 1.0**
   - Bad bot detection and blocking
   - Good bot allowlisting (search engines)

**Custom Rules:**

1. **Rate Limiting**
   - Threshold: 100 requests per minute per client IP
   - Applies to: `/api/*` endpoints
   - Action: Block for 1 minute

2. **Geo-Filtering**
   - Allowed regions: US, CA, GB, DE, FR (configurable)
   - Other regions: Blocked
   - Purpose: Reduce attack surface

**Review WAF Logs:**
```bash
# View blocked requests
az network front-door waf-log list \
  --resource-group fleet-assistant-prod-rg \
  --profile-name fleet-fd-prod \
  --query "[?action=='Block']"
```

**Customize WAF Rules:**
```bash
# Add IP allowlist rule
az network front-door waf-policy custom-rule create \
  --resource-group fleet-assistant-prod-rg \
  --policy-name fleetwafpolicyprod \
  --name AllowTrustedIPs \
  --priority 10 \
  --rule-type MatchRule \
  --action Allow \
  --match-variable RemoteAddr \
  --operator IPMatch \
  --match-value "1.2.3.4/32" "5.6.7.8/32"
```

### App Service Security Headers

The backend application should implement these security headers:

```csharp
// Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    await next();
});
```

### HTTPS Enforcement

- **App Service**: HTTPS-only mode enabled
- **Front Door**: Automatic HTTP to HTTPS redirect
- **Minimum TLS Version**: 1.2

**Verify HTTPS Configuration:**
```bash
# Check App Service HTTPS setting
az webapp config show \
  --resource-group fleet-assistant-prod-rg \
  --name fleet-api-prod \
  --query httpsOnly

# Check TLS version
az webapp config show \
  --resource-group fleet-assistant-prod-rg \
  --name fleet-api-prod \
  --query minTlsVersion
```

### CORS Configuration

Configured for SSE streaming support:

```json
{
  "allowedOrigins": ["*"],  // Restrict to specific origins in production
  "supportCredentials": false,
  "exposedHeaders": ["Content-Type", "Cache-Control"]
}
```

**Production Recommendation:**
```bash
# Update CORS to specific origin
az webapp cors add \
  --resource-group fleet-assistant-prod-rg \
  --name fleet-api-prod \
  --allowed-origins "https://yourdomain.com"
```

## Data Protection

### Encryption at Rest

All data is encrypted at rest using platform-managed keys:

- **Storage Account**: AES-256 encryption (automatically enabled)
- **AI Services**: Encrypted by default
- **App Service**: File system encryption
- **SQL Database (future)**: Transparent Data Encryption (TDE)

**Verify Encryption:**
```bash
# Storage account encryption
az storage account show \
  --resource-group fleet-assistant-prod-rg \
  --name fleetaihubstprod \
  --query encryption.services
```

### Encryption in Transit

- All traffic uses HTTPS/TLS 1.2+
- Private endpoints use Azure backbone network
- No data transmitted over public Internet (except through Front Door)

### Sensitive Data Handling

**Current State:**
- AI Services endpoint stored as app setting
- Managed identity for authentication (no keys in config)

**Future Enhancement (Key Vault):**
```bicep
// Example: Store sensitive config in Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${baseName}-kv-${environmentName}'
  properties: {
    enableRbacAuthorization: true
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
  }
}

// App Service references Key Vault secrets
appSettings: [
  {
    name: 'DatabaseConnectionString'
    value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=DbConnectionString)'
  }
]
```

## Monitoring and Compliance

### Security Monitoring

**Application Insights** tracks:
- Failed authentication attempts
- Unusual access patterns
- Error rates and exceptions
- Performance anomalies

**Azure Monitor** alerts on:
- NSG rule violations
- Firewall rule matches
- Private endpoint failures
- RBAC changes

**WAF Logging:**
- All WAF actions logged to Log Analytics
- Blocked requests analyzed for attack patterns

### Compliance Features

| Requirement | Implementation |
|-------------|----------------|
| Encryption at rest | Azure platform encryption |
| Encryption in transit | TLS 1.2+, HTTPS-only |
| Network isolation | Private endpoints, NSGs |
| Access control | RBAC, managed identities |
| Audit logging | All operations logged |
| Data residency | Single-region deployment |

### Security Scanning

**Recommendation:** Integrate with Azure Security Center / Microsoft Defender for Cloud

```bash
# Enable Defender for Cloud (subscription level)
az security pricing create \
  --name AppServices \
  --tier Standard

az security pricing create \
  --name StorageAccounts \
  --tier Standard

az security pricing create \
  --name SqlServers \
  --tier Standard
```

## Security Hardening Checklist

### Pre-Production

- [ ] Review and customize WAF rules for your traffic patterns
- [ ] Configure custom domain with valid SSL certificate
- [ ] Implement IP whitelisting if applicable
- [ ] Enable Azure DDoS Protection (Standard tier for critical workloads)
- [ ] Configure Key Vault for sensitive configuration
- [ ] Set up security alerts in Application Insights
- [ ] Enable diagnostic logging on all resources
- [ ] Configure log retention per compliance requirements

### Network Security

- [ ] Review NSG rules and remove unnecessary access
- [ ] Validate private endpoint connectivity
- [ ] Test firewall rules with legitimate traffic
- [ ] Ensure no public IP addresses on backend resources
- [ ] Configure VPN or ExpressRoute for admin access (optional)

### Identity & Access

- [ ] Audit RBAC role assignments
- [ ] Remove any over-privileged accounts
- [ ] Verify managed identity permissions are minimal
- [ ] Enable Azure AD authentication for App Service (future)
- [ ] Implement Just-In-Time (JIT) access for administration

### Application Security

- [ ] Review security headers implementation
- [ ] Test for common vulnerabilities (OWASP Top 10)
- [ ] Implement rate limiting in application code
- [ ] Validate input sanitization
- [ ] Enable Application Insights security features
- [ ] Configure Content Security Policy (CSP)

### Data Protection

- [ ] Classify data sensitivity levels
- [ ] Implement data retention policies
- [ ] Configure backup for databases (when added)
- [ ] Test encryption at rest
- [ ] Document data flow and storage locations

### Monitoring & Response

- [ ] Configure security alerts and action groups
- [ ] Set up incident response procedures
- [ ] Define escalation paths
- [ ] Schedule security review meetings
- [ ] Implement automated remediation where possible

### Compliance Documentation

- [ ] Document security architecture
- [ ] Maintain RBAC assignment records
- [ ] Keep audit logs for required retention period
- [ ] Document incident response procedures
- [ ] Prepare for compliance audits (SOC 2, ISO 27001, etc.)

## Security Incident Response

### Detection

Monitor these indicators:
1. Unusual spike in 5xx errors
2. WAF blocking spike
3. Failed authentication attempts
4. Changes to RBAC assignments
5. Private endpoint connection failures

### Response Procedure

1. **Identify**: Use Application Insights and Log Analytics to understand scope
2. **Contain**: Block malicious IPs in WAF, revoke compromised credentials
3. **Eradicate**: Remove malicious code, patch vulnerabilities
4. **Recover**: Restore from known-good backups
5. **Lessons Learned**: Document incident and update procedures

### Emergency Contacts

Maintain an incident response contact list with:
- Cloud platform team
- Application development team
- Security team
- Management escalation path

## Additional Resources

- [Azure Security Best Practices](https://learn.microsoft.com/en-us/azure/security/fundamentals/best-practices-and-patterns)
- [Azure Front Door Security](https://learn.microsoft.com/en-us/azure/frontdoor/front-door-waf)
- [Azure Network Security](https://learn.microsoft.com/en-us/azure/security/fundamentals/network-overview)
- [Managed Identities Best Practices](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/managed-identity-best-practice-recommendations)
- [Azure Compliance](https://learn.microsoft.com/en-us/azure/compliance/)

## Support

For security concerns:
1. Review security logs in Application Insights and Log Analytics
2. Check Azure Advisor security recommendations
3. Consult Microsoft Security Response Center (MSRC)
4. Engage Azure support for critical incidents

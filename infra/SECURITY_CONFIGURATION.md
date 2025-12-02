# Security Configuration - Fleet Assistant

This document provides comprehensive security configuration guidance for the Fleet Assistant multi-agent application on Azure.

## Security Architecture Overview

The Fleet Assistant implements defense-in-depth security with multiple layers:

1. **Perimeter Security**: Azure Front Door with WAF
2. **Network Security**: Private endpoints, NSGs, VNet isolation
3. **Identity & Access**: Managed identities, RBAC
4. **Data Security**: Encryption at rest and in transit
5. **Application Security**: HTTPS only, CORS policies, secure configuration

## 1. Azure Front Door and WAF

### WAF Policy Configuration

The Web Application Firewall (WAF) protects against common web exploits:

```bicep
Managed Rule Sets:
├── Microsoft_DefaultRuleSet 2.1
│   ├── OWASP Top 10 Protection
│   ├── SQL Injection Prevention
│   ├── Cross-Site Scripting (XSS) Protection
│   └── Command Injection Prevention
│
└── Microsoft_BotManagerRuleSet 1.0
    ├── Bad Bot Detection
    ├── Known Bot Blocking
    └── Bot Signature Matching
```

### Custom WAF Rules

#### Rate Limiting Rule

```json
{
  "name": "RateLimitRule",
  "priority": 100,
  "ruleType": "RateLimitRule",
  "rateLimitDurationInMinutes": 1,
  "rateLimitThreshold": 100,
  "matchConditions": [{
    "matchVariable": "RequestUri",
    "operator": "Contains",
    "matchValue": ["/api/"]
  }],
  "action": "Block"
}
```

**Purpose**: Limit API requests to 100 per minute per client IP

#### Geo-Blocking (Optional)

Add custom rules to block traffic from specific countries:

```bash
az network front-door waf-policy custom-rule create \
  --resource-group fleet-rg-prod \
  --policy-name fleetwafprod \
  --name GeoBlock \
  --priority 200 \
  --rule-type MatchRule \
  --action Block \
  --match-variable RemoteAddr \
  --operator GeoMatch \
  --match-values "CN" "RU"
```

### WAF Modes

- **Detection Mode**: Log only, no blocking (testing/staging)
- **Prevention Mode**: Active blocking (production)

Switch modes via Azure Portal or CLI:

```bash
az network front-door waf-policy update \
  --resource-group fleet-rg-prod \
  --name fleetwafprod \
  --mode Prevention
```

## 2. Network Security

### Private Endpoints

All PaaS services use private endpoints to eliminate public internet exposure:

| Service | Private Endpoint IP | Private DNS Zone |
|---------|---------------------|------------------|
| SQL Database | 10.1.2.4 | privatelink.database.windows.net |
| Blob Storage | 10.1.2.5 | privatelink.blob.core.windows.net |
| App Service | 10.1.2.6 | privatelink.azurewebsites.net |

### Verify Private Endpoint Configuration

```bash
# List private endpoints
az network private-endpoint list \
  --resource-group fleet-rg-prod \
  --output table

# Test DNS resolution (should return 10.x.x.x)
nslookup fleet-sql-prod-abc123.database.windows.net

# Verify private connection
az network private-endpoint-connection show \
  --resource-group fleet-rg-prod \
  --resource-name fleet-sql-prod-abc123 \
  --name connection-name \
  --type Microsoft.Sql/servers
```

### Network Security Group (NSG) Best Practices

#### 1. Least Privilege Principle

Each NSG rule follows the principle of least privilege:

```
AppServiceSubnet NSG:
✓ Allow only HTTPS (443) inbound from Internet
✓ Allow HTTP (80) for redirection to HTTPS
✗ Deny all other inbound traffic (implicit)
✓ Allow all outbound (App Service needs access to dependencies)

PrivateEndpointSubnet NSG:
✓ Allow traffic from VNet (10.1.0.0/16)
✗ Deny all other sources
```

#### 2. Audit NSG Rules

Regularly review NSG flow logs:

```bash
# Enable NSG flow logs
az network watcher flow-log create \
  --resource-group fleet-rg-prod \
  --nsg fleet-appservice-nsg-prod \
  --storage-account fleetnsglogsprod \
  --enabled true \
  --retention 90

# Query flow logs
az network watcher flow-log show \
  --resource-group fleet-rg-prod \
  --nsg fleet-appservice-nsg-prod
```

### Service Endpoints vs Private Endpoints

**Fleet Assistant Default**: Private Endpoints

**Fallback Option**: Service Endpoints (cost-saving for dev/test)

```bicep
// Enable service endpoint on subnet
serviceEndpoints: [
  {
    service: 'Microsoft.Sql'
    locations: [location]
  }
  {
    service: 'Microsoft.Storage'
    locations: [location]
  }
]
```

## 3. Identity and Access Management

### Managed Identities

Fleet Assistant uses **user-assigned managed identity** for:
- App Service → SQL Database
- App Service → Blob Storage
- App Service → AI Foundry
- App Service → Application Insights

#### Benefits

✅ No credentials in code or configuration
✅ Automatic credential rotation
✅ Centralized identity management
✅ Audit trail in Azure AD logs

#### RBAC Role Assignments

| Service | Role | Scope |
|---------|------|-------|
| SQL Database | SQL DB Contributor | Database |
| Blob Storage | Storage Blob Data Contributor | Storage Account |
| AI Services | Cognitive Services User | AI Services Resource |
| Application Insights | Monitoring Metrics Publisher | App Insights |

### Assign Roles

```bash
# Get managed identity principal ID
PRINCIPAL_ID=$(az identity show \
  --resource-group fleet-rg-prod \
  --name fleet-identity-prod \
  --query principalId -o tsv)

# Assign SQL DB Contributor role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "SQL DB Contributor" \
  --scope /subscriptions/SUBSCRIPTION_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Sql/servers/fleet-sql-prod/databases/fleetdbprod

# Assign Storage Blob Data Contributor role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Storage Blob Data Contributor" \
  --scope /subscriptions/SUBSCRIPTION_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Storage/storageAccounts/fleetstprod

# Assign Cognitive Services User role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "a97b65f3-24c7-4388-baec-2e87135dc908" \
  --scope /subscriptions/SUBSCRIPTION_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.CognitiveServices/accounts/fleet-aiservices-prod
```

### SQL Authentication with Managed Identity

Update connection string to use managed identity:

```csharp
// In App Service app settings
ConnectionStrings__DefaultConnection = 
  "Server=tcp:fleet-sql-prod.database.windows.net,1433;
   Initial Catalog=fleetdbprod;
   Authentication=Active Directory Managed Identity;
   Encrypt=True;
   TrustServerCertificate=False;
   Connection Timeout=30;"
```

Or in code:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var credential = new DefaultAzureCredential();
var connection = new SqlConnection(connectionString);
connection.AccessToken = await credential.GetTokenAsync(
    new TokenRequestContext(new[] { "https://database.windows.net/.default" })
).ConfigureAwait(false);
```

## 4. Data Security

### Encryption at Rest

| Service | Encryption | Key Management |
|---------|-----------|----------------|
| SQL Database | Transparent Data Encryption (TDE) | Microsoft-managed keys |
| Blob Storage | Storage Service Encryption (SSE) | Microsoft-managed keys |
| App Service | Disk encryption | Platform-managed |

#### Enable Customer-Managed Keys (Optional)

For enhanced security, use Azure Key Vault for key management:

```bash
# Create Key Vault
az keyvault create \
  --resource-group fleet-rg-prod \
  --name fleet-kv-prod \
  --location eastus \
  --enable-purge-protection true \
  --enable-soft-delete true

# Create encryption key
az keyvault key create \
  --vault-name fleet-kv-prod \
  --name storage-encryption-key \
  --kty RSA \
  --size 2048

# Configure storage account to use customer-managed key
az storage account update \
  --resource-group fleet-rg-prod \
  --name fleetstprod \
  --encryption-key-name storage-encryption-key \
  --encryption-key-source Microsoft.Keyvault \
  --encryption-key-vault https://fleet-kv-prod.vault.azure.net/
```

### Encryption in Transit

All services enforce TLS 1.2 minimum:

- SQL Database: `Encrypt=True` in connection string
- Blob Storage: HTTPS only (`supportsHttpsTrafficOnly: true`)
- App Service: HTTPS only (`httpsOnly: true`)
- Front Door: HTTPS redirect enabled

### Data Classification

| Data Type | Sensitivity | Encryption | Access Control |
|-----------|-------------|-----------|----------------|
| User credentials | High | TDE, HTTPS | RBAC, MFA required |
| Fleet telemetry | Medium | SSE, HTTPS | RBAC, Network isolation |
| Application logs | Low-Medium | SSE | RBAC, Retention policy |

## 5. Application Security

### CORS Configuration

App Service CORS allows frontend to make cross-origin requests:

```json
{
  "cors": {
    "allowedOrigins": [
      "https://fleet-frontend-prod.azurestaticapps.net",
      "https://fleet-fd-prod.azurefd.net"
    ],
    "supportCredentials": false
  }
}
```

**Production**: Restrict to specific origins (not `*`)

### Secure Headers

Add security headers in App Service:

```bash
az webapp config set \
  --resource-group fleet-rg-prod \
  --name fleet-api-prod \
  --http20-enabled true \
  --ftps-state FtpsOnly \
  --min-tls-version 1.2

# Add custom headers via web.config or code
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# X-XSS-Protection: 1; mode=block
# Strict-Transport-Security: max-age=31536000; includeSubDomains
```

In ASP.NET Core middleware:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", 
        "max-age=31536000; includeSubDomains");
    await next();
});
```

### Secrets Management

**Development**: App Settings (acceptable for non-production)

**Production**: Azure Key Vault

#### Migrate to Key Vault

1. Create Key Vault:
   ```bash
   az keyvault create \
     --resource-group fleet-rg-prod \
     --name fleet-kv-prod \
     --location eastus
   ```

2. Add secrets:
   ```bash
   az keyvault secret set \
     --vault-name fleet-kv-prod \
     --name SqlAdminPassword \
     --value "YourSecurePassword123!"
   ```

3. Grant App Service access:
   ```bash
   az keyvault set-policy \
     --name fleet-kv-prod \
     --object-id $MANAGED_IDENTITY_PRINCIPAL_ID \
     --secret-permissions get list
   ```

4. Reference in App Service:
   ```bash
   az webapp config appsettings set \
     --resource-group fleet-rg-prod \
     --name fleet-api-prod \
     --settings SqlAdminPassword="@Microsoft.KeyVault(SecretUri=https://fleet-kv-prod.vault.azure.net/secrets/SqlAdminPassword/)"
   ```

## 6. Monitoring and Alerting

### Security Monitoring

Enable Azure Defender for:
- App Service
- SQL Database
- Storage Account
- Key Vault

```bash
az security pricing create \
  --name AppServices \
  --tier Standard

az security pricing create \
  --name SqlServers \
  --tier Standard

az security pricing create \
  --name StorageAccounts \
  --tier Standard
```

### Security Alerts

Configure alerts for suspicious activity:

```bash
# Failed authentication attempts
az monitor metrics alert create \
  --resource-group fleet-rg-prod \
  --name FailedAuthAttempts \
  --scopes /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Sql/servers/fleet-sql-prod \
  --condition "total failed_connections_system_errors > 10" \
  --window-size 5m \
  --evaluation-frequency 1m

# Unusual data egress
az monitor metrics alert create \
  --resource-group fleet-rg-prod \
  --name UnusualDataEgress \
  --scopes /subscriptions/SUB_ID/resourceGroups/fleet-rg-prod/providers/Microsoft.Storage/storageAccounts/fleetstprod \
  --condition "total Egress > 10000000000" \
  --window-size 15m \
  --evaluation-frequency 5m
```

## 7. Compliance and Audit

### Enable Audit Logging

#### SQL Database Auditing

```bash
az sql server audit-policy update \
  --resource-group fleet-rg-prod \
  --server fleet-sql-prod \
  --state Enabled \
  --storage-account fleetauditprod \
  --retention-days 90
```

#### Storage Account Logging

```bash
az storage logging update \
  --account-name fleetstprod \
  --services b \
  --log rwd \
  --retention 90
```

### Azure Policy Compliance

Apply organizational policies:

```bash
# Require HTTPS for storage accounts
az policy assignment create \
  --name RequireHTTPSStorage \
  --scope /subscriptions/SUBSCRIPTION_ID/resourceGroups/fleet-rg-prod \
  --policy /providers/Microsoft.Authorization/policyDefinitions/404c3081-a854-4457-ae30-26a93ef643f9

# Require TLS 1.2 minimum
az policy assignment create \
  --name RequireTLS12 \
  --scope /subscriptions/SUBSCRIPTION_ID/resourceGroups/fleet-rg-prod \
  --policy /providers/Microsoft.Authorization/policyDefinitions/8f01c9b9-a4ee-4bc6-8f7f-b8b5c0c5e0a8
```

## 8. Security Checklist

### Pre-Production

- [ ] WAF policy set to Prevention mode
- [ ] All PaaS services use private endpoints
- [ ] NSG rules reviewed and tightened
- [ ] Managed identity configured for all service-to-service communication
- [ ] Secrets migrated to Key Vault
- [ ] TLS 1.2 enforced on all services
- [ ] CORS restricted to specific origins
- [ ] Security headers configured
- [ ] Audit logging enabled
- [ ] Azure Defender enabled

### Post-Production

- [ ] Regular security scans scheduled
- [ ] Security alert recipients configured
- [ ] Incident response plan documented
- [ ] Penetration testing completed
- [ ] Security awareness training for team
- [ ] Third-party security assessment (if required)

## 9. Incident Response

### Security Event Response Workflow

1. **Detection**: Alert triggered (Azure Defender, WAF, Application Insights)
2. **Assessment**: Review logs, determine severity
3. **Containment**: Isolate affected resources (NSG rules, disable accounts)
4. **Remediation**: Patch vulnerabilities, update configurations
5. **Recovery**: Restore services, verify integrity
6. **Post-Incident**: Document lessons learned, update runbooks

### Emergency Contacts

Maintain a list of security contacts:
- Security team lead
- Azure subscription owner
- Compliance officer
- Legal counsel (for breach notification)

## 10. References

- [Azure Security Best Practices](https://docs.microsoft.com/azure/security/fundamentals/best-practices-and-patterns)
- [Azure Front Door Security](https://docs.microsoft.com/azure/frontdoor/front-door-security-headers)
- [Azure Private Link](https://docs.microsoft.com/azure/private-link/)
- [Managed Identities](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault/)
- [Azure Defender](https://docs.microsoft.com/azure/security-center/azure-defender)

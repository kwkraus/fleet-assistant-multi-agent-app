# Fleet Assistant - Network Architecture

## Overview

The Fleet Assistant infrastructure implements a **hub-spoke network topology** following Microsoft's Azure architecture best practices for enterprise applications. This design provides centralized security, traffic management, and cost-effective scaling.

## High-Level Architecture Diagram

```
                                     ┌─────────────────────────────────────────────────────────┐
                                     │                    Internet / Users                      │
                                     └──────────────────────────┬──────────────────────────────┘
                                                                │
                                                                │ HTTPS
                                                                ▼
┌────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                  Azure Front Door Premium (Global)                                         │
│  ┌───────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                         Web Application Firewall (WAF)                                            │   │
│  │  • OWASP Rule Set 2.1 (SQL injection, XSS, etc.)                                                 │   │
│  │  • Bot Manager (Bad bot blocking)                                                                │   │
│  │  • Rate Limiting (100 req/min per IP)                                                            │   │
│  │  • Geo-filtering (US, CA, GB, DE, FR allowed)                                                    │   │
│  └───────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                            │
│  Routing:                                                                                                  │
│   • /api/* → Backend App Service                                                                          │
│   • /* → Frontend Static Web App                                                                          │
└────────────────┬───────────────────────────────────────────────────────────┬───────────────────────────────┘
                 │                                                             │
                 │ HTTPS                                                       │ HTTPS
                 ▼                                                             ▼
   ┌─────────────────────────────┐                           ┌─────────────────────────────┐
   │  Azure Static Web App       │                           │  App Service (Backend API)   │
   │  (Next.js Frontend)         │                           │  (ASP.NET Core)              │
   │  • Global CDN distribution  │                           │  • Private VNet integration  │
   │  • Automatic SSL/TLS        │                           │  • Managed identity auth     │
   └─────────────────────────────┘                           └──────────────┬───────────────┘
                                                                              │
                                                                              │ VNet Integration
                                                                              ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                         Azure Region (e.g., East US)                                         │
│                                                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                                    HUB VNET (10.0.0.0/16)                                           │   │
│  │                                                                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐  │   │
│  │  │  AzureFirewallSubnet (10.0.1.0/24)                                                           │  │   │
│  │  │  ┌──────────────────────────────────────────────────────────────────────────────────────┐   │  │   │
│  │  │  │                        Azure Firewall                                                 │   │  │   │
│  │  │  │  • Network rules (Allow Azure Monitor, Storage, Key Vault)                           │   │  │   │
│  │  │  │  • Application rules (Allow *.azure.com, *.microsoft.com)                            │   │  │   │
│  │  │  │  • Central egress point for spoke VNet                                                │   │  │   │
│  │  │  └──────────────────────────────────────────────────────────────────────────────────────┘   │  │   │
│  │  └─────────────────────────────────────────────────────────────────────────────────────────────┘  │   │
│  │                                                                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐  │   │
│  │  │  GatewaySubnet (10.0.2.0/24)                                                                 │  │   │
│  │  │  • Reserved for VPN Gateway / ExpressRoute (future)                                          │  │   │
│  │  └─────────────────────────────────────────────────────────────────────────────────────────────┘  │   │
│  │                                                                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐  │   │
│  │  │  AzureBastionSubnet (10.0.3.0/24)                                                            │  │   │
│  │  │  • Reserved for Azure Bastion (future secure RDP/SSH access)                                │  │   │
│  │  └─────────────────────────────────────────────────────────────────────────────────────────────┘  │   │
│  │                                                                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐  │   │
│  │  │  ManagementSubnet (10.0.4.0/24)                                                              │  │   │
│  │  │  • Management VMs and jump boxes                                                             │  │   │
│  │  │  • NSG: Allow HTTPS from Internet, deny all other inbound                                   │  │   │
│  │  └─────────────────────────────────────────────────────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────┬───────────────────────────────────────────────────────┘   │
│                                                  │                                                           │
│                                                  │ VNet Peering                                              │
│                                                  │ (Bi-directional, gateway transit)                         │
│                                                  ▼                                                           │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                                  SPOKE VNET (10.1.0.0/16)                                           │   │
│  │                                                                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐  │   │
│  │  │  AppServiceSubnet (10.1.1.0/24)                                                              │  │   │
│  │  │  • Delegated to Microsoft.Web/serverFarms                                                    │  │   │
│  │  │  • NSG: Allow HTTPS from Azure Front Door, allow VNet internal                              │  │   │
│  │  │  • Service Endpoints: Storage, Key Vault, SQL, CognitiveServices                            │  │   │
│  │  │  ┌─────────────────────────────────────────────────────────────────────────────────────┐   │  │   │
│  │  │  │                    App Service (Backend API)                                         │   │  │   │
│  │  │  │  • VNet integrated for outbound traffic                                              │   │  │   │
│  │  │  │  • All egress routed through hub firewall                                            │   │  │   │
│  │  │  │  • Managed identity for Azure service auth                                           │   │  │   │
│  │  │  └─────────────────────────────────────────────────────────────────────────────────────┘   │  │   │
│  │  └─────────────────────────────────────────────────────────────────────────────────────────────┘  │   │
│  │                                                                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐  │   │
│  │  │  PrivateEndpointSubnet (10.1.2.0/24)                                                         │  │   │
│  │  │  • NSG: Allow VNet internal traffic, deny Internet                                           │  │   │
│  │  │  • Private endpoint network policies disabled                                                │  │   │
│  │  │  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────────┐              │  │   │
│  │  │  │  Private Endpoint    │  │  Private Endpoint    │  │  Private Endpoint    │              │  │   │
│  │  │  │  (AI Services)       │  │  (Storage Account)   │  │  (App Service)       │              │  │   │
│  │  │  │  10.1.2.10           │  │  10.1.2.11           │  │  10.1.2.12           │              │  │   │
│  │  │  └──────────────────────┘  └──────────────────────┘  └──────────────────────┘              │  │   │
│  │  │       ▲                          ▲                          ▲                                │  │   │
│  │  │       │                          │                          │                                │  │   │
│  │  │       │ Private Link            │ Private Link            │ Private Link                    │  │   │
│  │  │       ▼                          ▼                          ▼                                │  │   │
│  │  │  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────────┐              │  │   │
│  │  │  │  Private DNS Zone    │  │  Private DNS Zone    │  │  Private DNS Zone    │              │  │   │
│  │  │  │  cognitiveservices   │  │  blob.core.windows   │  │  azurewebsites.net   │              │  │   │
│  │  │  └──────────────────────┘  └──────────────────────┘  └──────────────────────┘              │  │   │
│  │  └─────────────────────────────────────────────────────────────────────────────────────────────┘  │   │
│  │                                                                                                      │   │
│  │  ┌─────────────────────────────────────────────────────────────────────────────────────────────┐  │   │
│  │  │  DataSubnet (10.1.3.0/24)                                                                    │  │   │
│  │  │  • NSG: Allow 1433 & 443 from AppServiceSubnet only, deny Internet                          │  │   │
│  │  │  • Service Endpoints: SQL, Storage                                                          │  │   │
│  │  │  • Reserved for Azure SQL Database (future)                                                 │  │   │
│  │  └─────────────────────────────────────────────────────────────────────────────────────────────┘  │   │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                              Azure AI Foundry Resources (PaaS)                                      │   │
│  │  ┌────────────────────┐  ┌────────────────────┐  ┌────────────────────┐                          │   │
│  │  │   AI Hub           │  │   AI Project       │  │   AI Services      │                          │   │
│  │  │   (ML Workspace)   │  │   (Project)        │  │   (Cognitive)      │                          │   │
│  │  │                    │  │                    │  │                    │                          │   │
│  │  │  • Orchestration   │─▶│  • Agent config   │─▶│  • GPT-4 models   │                          │   │
│  │  │  • Private access  │  │  • Private access  │  │  • Private endpoint│◀─Connected to Subnet    │   │
│  │  └────────────────────┘  └────────────────────┘  └────────────────────┘                          │   │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
│                                                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────────────┐   │
│  │                                   Monitoring & Logging                                              │   │
│  │  ┌────────────────────┐  ┌────────────────────┐  ┌────────────────────┐                          │   │
│  │  │  App Insights      │  │  Log Analytics     │  │  Azure Monitor     │                          │   │
│  │  │                    │  │  Workspace         │  │  Alerts            │                          │   │
│  │  │  • App telemetry   │─▶│  • Centralized logs│─▶│  • Email/SMS       │                          │   │
│  │  │  • Custom metrics  │  │  • KQL queries     │  │  • Action groups   │                          │   │
│  │  └────────────────────┘  └────────────────────┘  └────────────────────┘                          │   │
│  └────────────────────────────────────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Traffic Flow Scenarios

### 1. User Request to Frontend (Static Web App)

```
User Browser
    │
    ├─ HTTPS request to Front Door endpoint
    │
    ▼
Azure Front Door
    │
    ├─ WAF inspection (check rules, rate limits, geo-location)
    │
    ├─ Route: /* matches frontend origin
    │
    ▼
Azure Static Web App (Next.js)
    │
    ├─ Serve static content from global CDN
    │
    └─ Return HTML/JS/CSS to user
```

### 2. API Request from Frontend to Backend

```
Frontend (Browser)
    │
    ├─ HTTPS API request (e.g., POST /api/chat)
    │
    ▼
Azure Front Door
    │
    ├─ WAF inspection
    │
    ├─ Route: /api/* matches backend origin
    │
    ▼
App Service (Backend API)
    │
    ├─ Process request in VNet-integrated environment
    │
    ├─ Call AI Foundry agent via private endpoint
    │     │
    │     ▼
    │  Private Endpoint (10.1.2.10)
    │     │
    │     ▼
    │  AI Services (Private access only)
    │     │
    │     └─ Return agent response
    │
    ├─ SSE stream response back through Front Door
    │
    └─ User receives streamed response
```

### 3. Backend Outbound to Azure Services

```
App Service (10.1.1.x)
    │
    ├─ Outbound call to Azure service (e.g., Storage, Key Vault)
    │
    ▼
VNet Integration → Spoke VNet
    │
    ├─ Use service endpoint OR private endpoint
    │
    ├─ If no service endpoint: route through Hub VNet
    │     │
    │     ▼
    │  Azure Firewall (10.0.1.x)
    │     │
    │     ├─ Check network/application rules
    │     │
    │     └─ Allow/Deny based on rules
    │
    ▼
Azure PaaS Service (Private endpoint or Service endpoint)
    │
    └─ Return response to App Service
```

## Network Security Layers

### Layer 1: Perimeter Security
- **Azure Front Door WAF**: Blocks malicious traffic at the edge
- **DDoS Protection**: Automatic protection for Front Door
- **Geo-filtering**: Only allow traffic from approved countries

### Layer 2: Network Access Control
- **NSGs**: Subnet-level traffic filtering
- **Azure Firewall**: Network and application-level inspection
- **Private Endpoints**: Eliminate public exposure of PaaS services

### Layer 3: Authentication & Authorization
- **Managed Identities**: Secure service-to-service authentication
- **Azure RBAC**: Least-privilege access control
- **No Credentials in Code**: All auth via Azure AD tokens

### Layer 4: Data Encryption
- **TLS 1.2+**: All traffic encrypted in transit
- **Private Network**: Backend traffic never leaves Azure backbone
- **Encryption at Rest**: All data encrypted with platform keys

## Key Design Decisions

### Why Hub-Spoke Topology?

✅ **Centralized Security**: Single point of control (Azure Firewall)  
✅ **Cost Efficiency**: Shared network resources across applications  
✅ **Scalability**: Easy to add new spoke VNets for additional apps  
✅ **Isolation**: Each spoke can be isolated with separate NSGs  
✅ **Traffic Inspection**: All north-south traffic goes through hub

### Why Private Endpoints?

✅ **Zero Public Exposure**: PaaS services have no public IPs  
✅ **Network Isolation**: Traffic stays on Azure backbone  
✅ **Compliance**: Meets data residency and privacy requirements  
✅ **Security**: No Internet-facing attack surface

### Why Azure Front Door?

✅ **Global Performance**: Anycast network, edge caching  
✅ **Built-in DDoS**: Automatic protection at global scale  
✅ **WAF Integration**: Security without separate appliance  
✅ **Health Probes**: Automatic failover for availability  
✅ **SSL Offload**: Centralized certificate management

## Network Flow Matrix

| Source | Destination | Protocol | Port | Route | Security |
|--------|-------------|----------|------|-------|----------|
| Internet | Front Door | HTTPS | 443 | Direct | WAF inspection |
| Front Door | App Service | HTTPS | 443 | Direct (service tag) | NSG allow |
| Front Door | Static Web App | HTTPS | 443 | Direct (CDN) | Built-in |
| App Service | AI Services | HTTPS | 443 | Private endpoint | Managed identity |
| App Service | Storage | HTTPS | 443 | Private endpoint | Managed identity |
| App Service | Internet | HTTPS | 443 | Hub Firewall | Firewall rules |
| Management | Any | HTTPS | 443 | Hub Firewall | NSG + Firewall |

## Monitoring Points

1. **Front Door**: Request count, latency, WAF blocks
2. **Azure Firewall**: Network rule hits, allowed/denied traffic
3. **NSGs**: Flow logs for security analysis
4. **Private Endpoints**: Connection status, health
5. **App Service**: Request metrics, dependency calls
6. **AI Services**: Token usage, API latency

## Disaster Recovery Considerations

### Single-Region Deployment (Current)
- **RTO**: ~1 hour (Azure service recovery)
- **RPO**: Near-zero (no data loss for stateless services)

### Multi-Region Enhancement (Future)
- **Active-Passive**: Primary region + failover region
- **Traffic Manager**: DNS-based failover
- **Geo-Replication**: Storage, SQL, AI models
- **RTO**: ~15 minutes (DNS propagation + failover)

## Cost Optimization

**Network Costs:**
- VNet peering: ~$0.01/GB
- Private endpoints: ~$7/month per endpoint
- Azure Firewall: ~$180/month (Standard tier)
- Front Door Premium: ~$35/month + data transfer

**Optimization Strategies:**
- Use service endpoints where possible (free vs. private endpoints)
- Disable Azure Firewall in dev (use NSGs only)
- Use Free tier Front Door in dev
- Enable diagnostic settings only in prod

## Compliance & Governance

### Network Policies Enforced
- ✅ No public IPs on backend resources
- ✅ All traffic encrypted (TLS 1.2+)
- ✅ Network segmentation (subnets, NSGs)
- ✅ Centralized egress (Azure Firewall)
- ✅ Audit logs enabled (NSG flow logs)

### Industry Standards Alignment
- **NIST Cybersecurity Framework**: Network segmentation, access control
- **ISO 27001**: Information security management
- **SOC 2**: Security monitoring and controls
- **GDPR**: Data residency and encryption

## Troubleshooting Common Network Issues

### Issue: App Service Can't Reach AI Services

**Check:**
1. Private endpoint connection status
2. Private DNS zone linked to VNet
3. NSG rules on private endpoint subnet
4. Managed identity has RBAC permissions

**Diagnose:**
```bash
# From App Service SSH console
nslookup <ai-services-endpoint>
# Should resolve to 10.1.2.x (private IP)
```

### Issue: High Latency from Frontend to Backend

**Check:**
1. Front Door origin health status
2. App Service response times in Application Insights
3. Azure Firewall performance metrics
4. Network latency between regions

**Diagnose:**
```bash
# Test Front Door routing
curl -I https://<frontdoor-endpoint>/api/healthz

# Check App Service directly (should be blocked if private-only)
curl -I https://<appservice-name>.azurewebsites.net/healthz
```

## Additional Resources

- [Hub-Spoke Network Topology](https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/hybrid-networking/hub-spoke)
- [Azure Private Link](https://learn.microsoft.com/en-us/azure/private-link/private-link-overview)
- [Azure Front Door Best Practices](https://learn.microsoft.com/en-us/azure/frontdoor/front-door-best-practices)
- [Azure Network Security](https://learn.microsoft.com/en-us/azure/security/fundamentals/network-best-practices)

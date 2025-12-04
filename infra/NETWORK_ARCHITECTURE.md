# Network Architecture - Fleet Assistant

This document describes the network architecture for the Fleet Assistant multi-agent application, implementing a hub-and-spoke topology following the Microsoft Reliable Web App pattern.

## Architecture Overview

The Fleet Assistant network architecture implements enterprise-grade security and isolation using Azure's networking capabilities. The design follows a hub-and-spoke topology to centralize security controls while providing application isolation.

## Network Topology

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                              Azure Front Door (Global)                      │
│                      ┌────────────────────────────────────┐                 │
│                      │  WAF Policy                        │                 │
│                      │  - OWASP Top 10 Protection         │                 │
│                      │  - Bot Protection                   │                 │
│                      │  - Rate Limiting                    │                 │
│                      │  - DDoS Protection                  │                 │
│                      └────────────────────────────────────┘                 │
│                                    │                                        │
│                                    │ HTTPS                                  │
│                                    ▼                                        │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                 ┌───────────────────┴────────────────────┐
                 │                                        │
                 ▼                                        ▼
    ┌────────────────────────┐              ┌────────────────────────┐
    │   Static Web App        │              │   App Service          │
    │   (Frontend)            │              │   (Backend API)        │
    │                         │              │                        │
    │   Next.js 15            │              │   ASP.NET Core 8.0     │
    │   React 19              │              │   SSE Streaming        │
    └────────────────────────┘              └────────────────────────┘
                                                       │
                                     ┌─────────────────┴─────────────────┐
                                     │       Regional VNet (Spoke)       │
                                     └───────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           HUB VIRTUAL NETWORK                               │
│                           10.0.0.0/16                                       │
│                                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐   │
│  │  AzureFirewallSubnet                                                │   │
│  │  10.0.1.0/24                                                         │   │
│  │  ┌──────────────────────┐                                           │   │
│  │  │  Azure Firewall      │  Network-level traffic inspection         │   │
│  │  │  (Future)            │  Outbound internet control                │   │
│  │  └──────────────────────┘                                           │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐   │
│  │  AzureBastionSubnet                                                 │   │
│  │  10.0.2.0/24                                                         │   │
│  │  ┌──────────────────────┐                                           │   │
│  │  │  Azure Bastion       │  Secure VM management                     │   │
│  │  │  (Future)            │  RDP/SSH over SSL                         │   │
│  │  └──────────────────────┘                                           │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     │ VNet Peering
                                     │ (Full Mesh Connectivity)
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          SPOKE VIRTUAL NETWORK                              │
│                          10.1.0.0/16                                        │
│                                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐   │
│  │  AppServiceSubnet                                                   │   │
│  │  10.1.1.0/24                                                         │   │
│  │  Delegated to: Microsoft.Web/serverFarms                            │   │
│  │  ┌──────────────────────┐                                           │   │
│  │  │  App Service VNet    │  Backend API integration                  │   │
│  │  │  Integration         │  Outbound traffic through VNet            │   │
│  │  └──────────────────────┘                                           │   │
│  │  NSG: Allow HTTPS (80, 443) inbound from Internet                   │   │
│  │       Allow all outbound                                             │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐   │
│  │  PrivateEndpointSubnet                                              │   │
│  │  10.1.2.0/24                                                         │   │
│  │  Private Endpoint Network Policies: Disabled                        │   │
│  │  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐              │   │
│  │  │ SQL DB PE   │  │ Blob Storage │  │ App Service  │              │   │
│  │  │             │  │ PE           │  │ PE           │              │   │
│  │  └─────────────┘  └──────────────┘  └──────────────┘              │   │
│  │  NSG: Allow VNet inbound, Deny all other                            │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌────────────────────────────────────────────────────────────────────┐   │
│  │  DatabaseSubnet                                                     │   │
│  │  10.1.3.0/24                                                         │   │
│  │  Service Endpoints: Microsoft.Sql                                   │   │
│  │  NSG: Allow SQL (1433) from AppServiceSubnet                        │   │
│  │       Deny all other inbound                                        │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Address Space Allocation

### Hub VNet (10.0.0.0/16)
- **Purpose**: Centralized security and management
- **Total Addresses**: 65,536
- **Subnets**:
  - `AzureFirewallSubnet` - 10.0.1.0/24 (256 addresses)
  - `AzureBastionSubnet` - 10.0.2.0/24 (256 addresses)
  - Reserved for future expansion: 10.0.3.0/24 - 10.0.255.0/24

### Spoke VNet (10.1.0.0/16)
- **Purpose**: Application resources isolation
- **Total Addresses**: 65,536
- **Subnets**:
  - `AppServiceSubnet` - 10.1.1.0/24 (256 addresses)
  - `PrivateEndpointSubnet` - 10.1.2.0/24 (256 addresses)
  - `DatabaseSubnet` - 10.1.3.0/24 (256 addresses)
  - Reserved for future services: 10.1.4.0/24 - 10.1.255.0/24

### Environment-Specific Address Spaces

- **Development**: 10.0.0.0/16 (Hub), 10.1.0.0/16 (Spoke)
- **Staging**: 10.50.0.0/16 (Hub), 10.51.0.0/16 (Spoke)
- **Production**: 10.100.0.0/16 (Hub), 10.101.0.0/16 (Spoke)

## Traffic Flow Diagrams

### Inbound User Request Flow

```
┌─────────┐
│  User   │
└────┬────┘
     │
     │ HTTPS (443)
     │
     ▼
┌─────────────────────────┐
│   Azure Front Door      │
│   - DNS Resolution      │
│   - SSL Termination     │
│   - WAF Inspection      │
│   - Routing Decision    │
└────────┬────────────────┘
         │
         ├─────────────────────────────┬───────────────────────────────┐
         │                             │                               │
         │ Route: /*                   │ Route: /api/*                 │
         │                             │                               │
         ▼                             ▼                               ▼
┌─────────────────┐          ┌──────────────────┐         ┌─────────────────┐
│  Static Web App │          │   App Service    │         │ Health Probe:   │
│  (Frontend)     │          │   (Backend API)  │         │ /healthz        │
│                 │          │                  │         │                 │
│  Serves:        │          │  Serves:         │         │  Returns:       │
│  - HTML/CSS/JS  │          │  - REST APIs     │         │  - 200 OK       │
│  - React App    │          │  - SSE Streaming │         │  - Health Status│
└─────────────────┘          └──────────────────┘         └─────────────────┘
```

### Backend to PaaS Services Flow

```
┌────────────────────────────────────────────────────────────────────┐
│                     App Service (Backend API)                      │
│                                                                    │
│  - VNet Integration enabled                                        │
│  - Managed Identity assigned                                       │
│  - Outbound traffic routes through VNet                           │
└────────────────────────────────────────────────────────────────────┘
                           │
                           │ Private Network
                           │ (No Internet Exposure)
                           │
        ┌──────────────────┼──────────────────┬───────────────────┐
        │                  │                  │                   │
        ▼                  ▼                  ▼                   ▼
┌────────────────┐  ┌────────────────┐  ┌──────────────┐  ┌────────────────┐
│  SQL Database  │  │ Blob Storage   │  │  AI Foundry  │  │ App Insights   │
│                │  │                │  │              │  │                │
│  Private EP:   │  │  Private EP:   │  │  Public EP   │  │  Public EP     │
│  10.1.2.4      │  │  10.1.2.5      │  │  (Managed ID)│  │  (Conn String) │
└────────────────┘  └────────────────┘  └──────────────┘  └────────────────┘
        │                  │
        │ Port 1433        │ Port 443
        │ Protocol: TCP    │ Protocol: HTTPS
        │                  │
        └──────────────────┴───────────────────────────────────────────────┐
                                                                            │
                           Private DNS Resolution                          │
                           - privatelink.database.windows.net              │
                           - privatelink.blob.core.windows.net             │
                           - privatelink.azurewebsites.net                 │
                                                                            │
└───────────────────────────────────────────────────────────────────────────┘
```

## Network Security Groups (NSG) Rules

### AppServiceSubnet NSG

| Priority | Name | Direction | Access | Protocol | Source | Dest Port | Destination | Description |
|----------|------|-----------|--------|----------|--------|-----------|-------------|-------------|
| 100 | AllowHttpsInbound | Inbound | Allow | TCP | Internet | 443 | * | HTTPS from Internet |
| 110 | AllowHttpInbound | Inbound | Allow | TCP | Internet | 80 | * | HTTP from Internet (redirect to HTTPS) |
| 100 | AllowAllOutbound | Outbound | Allow | * | * | * | * | All outbound traffic |

### PrivateEndpointSubnet NSG

| Priority | Name | Direction | Access | Protocol | Source | Dest Port | Destination | Description |
|----------|------|-----------|--------|----------|--------|-----------|-------------|-------------|
| 100 | AllowVNetInbound | Inbound | Allow | * | VirtualNetwork | * | VirtualNetwork | VNet to private endpoints |
| 1000 | DenyAllInbound | Inbound | Deny | * | * | * | * | Block all other inbound |

### DatabaseSubnet NSG

| Priority | Name | Direction | Access | Protocol | Source | Dest Port | Destination | Description |
|----------|------|-----------|--------|----------|--------|-----------|-------------|-------------|
| 100 | AllowSqlFromAppService | Inbound | Allow | TCP | 10.1.1.0/24 | 1433 | * | SQL from App Service |
| 1000 | DenyAllInbound | Inbound | Deny | * | * | * | * | Block all other inbound |

## Private DNS Zones

### Configured Zones

1. **Blob Storage**: `privatelink.blob.core.windows.net`
   - Purpose: Resolve storage account private endpoints
   - Linked to: Spoke VNet

2. **SQL Database**: `privatelink.database.windows.net`
   - Purpose: Resolve SQL server private endpoints
   - Linked to: Spoke VNet

3. **App Service**: `privatelink.azurewebsites.net`
   - Purpose: Resolve App Service private endpoints
   - Linked to: Spoke VNet

### DNS Resolution Flow

```
App Service (10.1.1.x) wants to connect to SQL Database
    │
    ▼
1. DNS Query: fleet-sql-dev-abc123.database.windows.net
    │
    ▼
2. Azure DNS intercepts (Private DNS Zone linked to VNet)
    │
    ▼
3. Private DNS Zone: privatelink.database.windows.net
    │
    ▼
4. Returns: 10.1.2.4 (Private Endpoint IP)
    │
    ▼
5. Traffic flows through VNet (never leaves Azure backbone)
```

## VNet Peering Configuration

### Hub-to-Spoke Peering

- **Name**: hub-to-spoke-peering
- **Allow Virtual Network Access**: Yes
- **Allow Forwarded Traffic**: Yes
- **Allow Gateway Transit**: No (reserved for future VPN/ExpressRoute)
- **Use Remote Gateways**: No

### Spoke-to-Hub Peering

- **Name**: spoke-to-hub-peering
- **Allow Virtual Network Access**: Yes
- **Allow Forwarded Traffic**: Yes
- **Allow Gateway Transit**: No
- **Use Remote Gateways**: No (reserved for future VPN/ExpressRoute)

## Service Endpoints vs Private Endpoints

### When to Use Each

| Feature | Service Endpoint | Private Endpoint |
|---------|-----------------|------------------|
| Cost | Free | ~$7/month per endpoint |
| IP Address | Public (filtered by firewall) | Private (10.x.x.x) |
| DNS Required | No | Yes (Private DNS Zone) |
| Cross-Region | No | Yes |
| On-Premises Access | No | Yes (via VPN/ExpressRoute) |

**Fleet Assistant Choice**: Private Endpoints for production security

## Security Layers

### Defense in Depth

1. **Layer 1 - Perimeter**
   - Azure Front Door with WAF
   - DDoS Protection
   - Rate limiting

2. **Layer 2 - Network**
   - VNet isolation
   - NSG rules
   - Private endpoints

3. **Layer 3 - Application**
   - Managed identities
   - HTTPS only
   - CORS policies

4. **Layer 4 - Data**
   - Encryption at rest
   - Encryption in transit
   - Private connectivity

## High Availability Considerations

### Current Implementation

- **App Service**: Zone redundancy disabled (dev), enabled for prod
- **SQL Database**: Locally redundant storage
- **Storage Account**: Locally redundant storage
- **Front Door**: Multi-region by default (global service)

### Future Multi-Region Setup

```
┌──────────────────────────────────────────────────────────────────┐
│                    Azure Front Door (Global)                     │
│                    Traffic Manager / Routing                     │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                 ┌───────────┴───────────┐
                 │                       │
                 ▼                       ▼
        ┌─────────────────┐     ┌─────────────────┐
        │  Primary Region │     │ Secondary Region│
        │  (East US)      │     │  (West US 2)    │
        │                 │     │                 │
        │  Active         │     │  Standby        │
        │  Traffic: 100%  │     │  Traffic: 0%    │
        └─────────────────┘     └─────────────────┘
                 │                       │
                 └───────────┬───────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │   SQL Geo-      │
                    │   Replication   │
                    └─────────────────┘
```

## Monitoring and Observability

### Network Monitoring

- **NSG Flow Logs**: Track allowed/denied traffic
- **Front Door Logs**: Request/response metrics
- **VNet Flow Logs**: Inter-subnet communication
- **Azure Monitor**: Network topology visualization

### Key Metrics

- Private endpoint connection status
- NSG hit count (allow/deny rules)
- Front Door response time
- VNet peering status

## Troubleshooting

### Common Issues

1. **Cannot connect to private endpoint**
   - Check: Private DNS zone is linked to VNet
   - Check: NSG allows traffic from source subnet
   - Check: Private endpoint provisioning state is "Succeeded"

2. **Front Door health probe failing**
   - Check: App Service `/healthz` endpoint returns 200
   - Check: NSG allows inbound HTTPS from Internet
   - Check: App Service is running

3. **SQL connection timeout**
   - Check: Firewall allows App Service subnet
   - Check: Connection string uses private endpoint FQDN
   - Check: Private DNS resolves to 10.x.x.x address

## Best Practices Implemented

✅ Hub-and-spoke topology for centralized management
✅ Private endpoints for all PaaS services
✅ NSG rules follow least-privilege principle
✅ Separate address spaces per environment
✅ Private DNS zones for name resolution
✅ VNet integration for App Service
✅ Service endpoints as fallback option
✅ Front Door for global load balancing
✅ WAF for application-layer security

## References

- [Azure VNet Documentation](https://docs.microsoft.com/azure/virtual-network/)
- [Hub-Spoke Topology](https://docs.microsoft.com/azure/architecture/reference-architectures/hybrid-networking/hub-spoke)
- [Private Endpoints](https://docs.microsoft.com/azure/private-link/private-endpoint-overview)
- [Azure Front Door](https://docs.microsoft.com/azure/frontdoor/)

# Fleet Assistant Multi-Agent Application

A sophisticated multi-agent fleet management system built with ASP.NET Core WebAPI backend and Next.js 15 frontend, powered by Azure AI Foundry Agent Service. This application provides intelligent fleet management capabilities through specialized AI agents that coordinate to handle fuel tracking, maintenance scheduling, safety monitoring, and more.

<img width="1103" height="905" alt="image" src="https://github.com/user-attachments/assets/52f05cc8-d369-430f-85e2-2ff5f4651a43" />

## 🏗️ Architecture

The application consists of three main components:

- **Backend**: ASP.NET Core 8.0 WebAPI (`src/backend/FleetAssistant.WebApi`)
  - Azure AI Foundry Agent Service integration via `Azure.AI.Agents.Persistent` SDK
  - Entity Framework Core with SQL Server and In-Memory database support
  - Azure Blob Storage for document management
  - RESTful API with Server-Sent Events (SSE) streaming
  
- **Frontend**: Next.js 15 chat interface (`src/frontend/ai-chatbot`)
  - React 19 with App Router
  - Tailwind CSS v4 with Radix UI components
  - Custom SSE streaming implementation for real-time chat
  
- **Shared**: Common models and interfaces (`src/backend/FleetAssistant.Shared`)
  - DTOs and shared domain models
  - Common interfaces for cross-cutting concerns

### Key Features

- 🤖 **Multi-Agent Coordination**: Planning agent orchestrates specialized agents (Fuel, Maintenance, Safety)
- 🔌 **Integration Plugins**: Extensible plugin system for GeoTab, Fleetio, and Samsara
- 💬 **Real-time Streaming**: SSE-based streaming responses for interactive conversations
- 🔐 **Multi-Tenant Security**: API key authentication with tenant isolation
- 📊 **Fleet Analytics**: Intelligent fuel efficiency, maintenance, and safety monitoring
- 📁 **Document Management**: Azure Blob Storage integration for fleet documents

## 📋 Prerequisites

Before setting up the application, ensure you have the following tools installed:

### Required Tools

- **Node.js**: v20.19.5 or higher
  - Download from [nodejs.org](https://nodejs.org/)
  - Verify: `node --version`
  
- **npm**: v10.8.2 or higher
  - Comes with Node.js
  - Verify: `npm --version`
  
- **.NET SDK**: 8.0 or higher
  - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
  - Verify: `dotnet --version`
  
- **Git**: Latest version
  - Download from [git-scm.com](https://git-scm.com/)
  - Verify: `git --version`

### Optional Tools

- **Visual Studio 2022** or **Visual Studio Code** (recommended)
  - VS Code extensions: C# Dev Kit, C#, REST Client
- **SQL Server** (optional - application can use in-memory database for development)
- **Azure CLI** (for Azure deployment)
  - Download from [docs.microsoft.com/cli/azure/install-azure-cli](https://docs.microsoft.com/cli/azure/install-azure-cli)
  - Required for infrastructure deployment
- **Azurite** (for local Azure Storage emulation)
  - Install: `npm install -g azurite`

### Azure Services (Required for Production)

- **Azure AI Foundry Agent Service**
  - Create an AI Foundry project in Azure Portal
  - Note the Agent Endpoint and Agent ID
- **Azure Storage Account** (for blob storage)
- **Azure SQL Database** (optional - can use in-memory DB for dev)
- **Application Insights** (optional - for monitoring)

## 🚢 Azure Deployment (Production)

The Fleet Assistant includes comprehensive Bicep Infrastructure-as-Code templates following the **Microsoft Reliable Web App (RWA) pattern**. This provides enterprise-grade security, networking, and operational excellence.

### Quick Start - Azure Deployment

```bash
# 1. Login to Azure
az login

# 2. Create resource group
az group create \
  --name fleet-assistant-prod-rg \
  --location eastus

# 3. Deploy infrastructure
az deployment group create \
  --resource-group fleet-assistant-prod-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.dev.bicepparam

# 4. Deployment takes 15-25 minutes
# Outputs will include URLs for App Service, Static Web App, and Front Door
```

### Infrastructure Features

✅ **Enterprise Networking**
- Hub-spoke VNet topology with centralized security
- Private endpoints for all PaaS services (no public exposure)
- Azure Firewall for network-level traffic control
- Network Security Groups (NSGs) for subnet isolation

✅ **Global Security & Performance**
- Azure Front Door Premium with global load balancing
- Web Application Firewall (WAF) with OWASP rules
- DDoS protection and geo-filtering
- Managed identities for secure service authentication

✅ **AI Integration**
- Azure AI Foundry Hub and Project provisioning
- AI Services (Cognitive Services) with private connectivity
- Secure RBAC configuration for agent access
- Automatic endpoint configuration for backend

✅ **Operational Excellence**
- Application Insights for APM and custom telemetry
- Log Analytics for centralized logging
- Auto-configured alerts for errors, performance, availability
- Health checks and availability tests

✅ **Cost Optimization**
- Environment-specific SKU sizing (dev/staging/prod)
- Autoscaling rules based on CPU, memory, and queue depth
- Daily caps on Log Analytics for dev environments

### Detailed Deployment Documentation

For comprehensive deployment instructions, see:
- **[Deployment Guide](./docs/DEPLOYMENT.md)** - Step-by-step deployment instructions
- **[Security Guide](./docs/SECURITY.md)** - Security architecture and hardening
- **[Monitoring Guide](./docs/MONITORING.md)** - Observability and alerting setup

### Infrastructure Components

| Component | Purpose | SKU (Dev/Prod) |
|-----------|---------|----------------|
| Azure Front Door | Global CDN + WAF | Premium |
| App Service | Backend API | B1 / P1v3 |
| Static Web App | Next.js Frontend | Free / Standard |
| AI Foundry | Multi-agent system | S0 |
| Azure Firewall | Network security | Standard |
| Application Insights | Monitoring | Pay-per-use |

### Post-Deployment Steps

After infrastructure deployment:

1. **Create AI Agent** in Azure AI Foundry portal
2. **Update deployment** with agent ID
3. **Deploy application code** to App Service and Static Web App
4. **Configure custom domain** in Front Door (production)
5. **Review monitoring** dashboards and alerts

See the [Deployment Guide](./docs/DEPLOYMENT.md) for detailed instructions.

## 📋 Prerequisites

## 🚀 Local Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/kwkraus/fleet-assistant-multi-agent-app.git
cd fleet-assistant-multi-agent-app
```

### 2. Backend Setup

#### Step 1: Navigate to Backend Directory

```bash
cd src/backend/FleetAssistant.WebApi
```

#### Step 2: Restore Dependencies

```bash
dotnet restore
```

#### Step 3: Configure Application Settings

Create or update `appsettings.Development.json` (this file is gitignored):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "FleetAssistantDb": ""
  },
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "DefaultContainer": "fleet-documents",
    "MaxFileSizeBytes": 10485760,
    "AllowedExtensions": [".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif", ".xlsx", ".csv"]
  },
  "UseFoundryAgent": true,
  "FoundryAgentService": {
    "AgentId": "YOUR_AGENT_ID_HERE",
    "AgentEndpoint": "YOUR_FOUNDRY_AGENT_ENDPOINT_HERE",
    "RunPollingDelayMs": 100,
    "StreamingDelayMs": 50
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  }
}
```

**Configuration Notes:**

- **ConnectionStrings.FleetAssistantDb**: Leave empty to use in-memory database for development
- **BlobStorage.ConnectionString**: Use `UseDevelopmentStorage=true` for Azurite, or provide Azure Storage connection string
- **UseFoundryAgent**: Set to `true` to enable Azure AI Foundry, or `false` to use mock agent
- **FoundryAgentService.AgentEndpoint**: Your Azure AI Foundry service URL (e.g., `https://your-project.services.ai.azure.com/api/projects/YourProject`)
- **FoundryAgentService.AgentId**: Your hosted agent identifier from Azure AI Foundry

#### Step 4: Set Up Azure Authentication (Optional but Recommended)

The application uses `DefaultAzureCredential` for Azure services authentication. Set up one of these options:

**Option A: Azure CLI (Recommended for Local Development)**
```bash
az login
```

**Option B: Environment Variables**
```bash
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
export AZURE_TENANT_ID="your-tenant-id"
```

**Option C: User Secrets (for sensitive data)**
```bash
dotnet user-secrets init
dotnet user-secrets set "FoundryAgentService:AgentId" "your-agent-id"
dotnet user-secrets set "FoundryAgentService:AgentEndpoint" "your-endpoint"
```

#### Step 5: Run Database Migrations (Optional - if using SQL Server)

```bash
dotnet ef database update
```

#### Step 6: Start the Backend

```bash
dotnet run
```

The backend will be available at:
- HTTP: `http://localhost:5074`
- HTTPS: `https://localhost:7074`

**Verify the backend is running:**
- Open `https://localhost:7074/swagger` in your browser to see the API documentation
- Health check: `https://localhost:7074/health`

### 3. Frontend Setup

#### Step 1: Navigate to Frontend Directory

Open a new terminal and navigate to the frontend:

```bash
cd src/frontend/ai-chatbot
```

#### Step 2: Install Dependencies

```bash
npm install
```

#### Step 3: Configure Environment Variables

Create a `.env.local` file (this file is gitignored):

```env
# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:5074
```

**Note**: The frontend connects to the backend WebAPI endpoint for chat functionality.

#### Step 4: Start the Frontend Development Server

```bash
npm run dev
```

The frontend will be available at `http://localhost:3000`

**Verify the frontend is running:**
- Open `http://localhost:3000` in your browser
- You should see the Fleet Assistant chat interface

### 4. Testing the Integration

#### Option 1: Use the Web Interface

1. Open `http://localhost:3000` in your browser
2. Type a message in the chat interface (e.g., "Tell me about fleet maintenance")
3. You should receive a streaming response from the AI agent

#### Option 2: Use the Integration Test Script

```powershell
# From the root directory
.\testing\test-integration.ps1
```

#### Option 3: Use the Node.js Test Scripts

```bash
# Test WebAPI chat endpoint with streaming
node testing/test-webapi-chat.js

# Test conversation flow
node testing/test-conversation-flow.js
```

#### Option 4: Use REST Client (VS Code)

If using VS Code with REST Client extension:
1. Open `src/backend/FleetAssistant.WebApi/api-test.http`
2. Click "Send Request" on any of the test requests

## 🔧 Configuration Details

### Backend Configuration Options

The backend can be configured through `appsettings.json`, `appsettings.Development.json`, environment variables, or user secrets.

#### Database Configuration

**In-Memory Database (Development)**
```json
{
  "ConnectionStrings": {
    "FleetAssistantDb": ""
  }
}
```

**SQL Server**
```json
{
  "ConnectionStrings": {
    "FleetAssistantDb": "Server=(localdb)\\mssqllocaldb;Database=FleetAssistant;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

#### Azure Blob Storage

**Local Development (Azurite)**
```json
{
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "DefaultContainer": "fleet-documents"
  }
}
```

**Azure Storage Account**
```json
{
  "BlobStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=youraccountkey;EndpointSuffix=core.windows.net",
    "DefaultContainer": "fleet-documents"
  }
}
```

#### Azure AI Foundry Agent Configuration

```json
{
  "UseFoundryAgent": true,
  "FoundryAgentService": {
    "AgentId": "your-agent-id",
    "AgentEndpoint": "https://your-project.services.ai.azure.com/api/projects/YourProject",
    "RunPollingDelayMs": 100,
    "StreamingDelayMs": 50
  }
}
```

- **AgentId**: The unique identifier for your agent in Azure AI Foundry
- **AgentEndpoint**: The full URL to your Azure AI Foundry project
- **RunPollingDelayMs**: How often to poll for run status updates (milliseconds)
- **StreamingDelayMs**: Delay between streaming chunks for smooth UX (milliseconds)

### Frontend Configuration Options

The frontend uses environment variables prefixed with `NEXT_PUBLIC_` to expose them to the browser.

**`.env.local`**
```env
# Backend API URL
NEXT_PUBLIC_API_URL=http://localhost:5074

# Optional: API Base Path (if backend is not at root)
# NEXT_PUBLIC_API_BASE_PATH=/api
```

## 📦 Building for Production

### Backend Build

```bash
cd src/backend/FleetAssistant.WebApi
dotnet publish -c Release -o ./publish
```

The published files will be in the `publish` directory.

### Frontend Build

```bash
cd src/frontend/ai-chatbot
npm run build
npm run start
```

The optimized production build will be created in the `.next` directory.

## 🚢 Deployment

### Deploy to Azure

#### Prerequisites

- Azure subscription
- Azure CLI installed and authenticated (`az login`)
- Required Azure resources created:
  - Azure AI Foundry project with an agent
  - Azure App Service (or Azure Container Apps)
  - Azure Storage Account
  - Azure SQL Database (optional)
  - Application Insights (optional)

#### Backend Deployment to Azure App Service

**Option 1: Using Azure CLI**

```bash
# Create resource group
az group create --name fleet-assistant-rg --location eastus

# Create App Service plan
az appservice plan create --name fleet-assistant-plan --resource-group fleet-assistant-rg --sku B1 --is-linux

# Create Web App
az webapp create --name fleet-assistant-api --resource-group fleet-assistant-rg --plan fleet-assistant-plan --runtime "DOTNET|8.0"

# Configure App Settings
az webapp config appsettings set --name fleet-assistant-api --resource-group fleet-assistant-rg --settings \
  FoundryAgentService__AgentId="your-agent-id" \
  FoundryAgentService__AgentEndpoint="your-endpoint" \
  UseFoundryAgent="true"

# Deploy
cd src/backend/FleetAssistant.WebApi
dotnet publish -c Release
cd bin/Release/net8.0/publish
zip -r deploy.zip .
az webapp deployment source config-zip --name fleet-assistant-api --resource-group fleet-assistant-rg --src deploy.zip
```

**Option 2: Using Visual Studio**

1. Right-click on `FleetAssistant.WebApi` project
2. Select "Publish"
3. Choose "Azure" as target
4. Select "Azure App Service (Linux)"
5. Configure and publish

**Option 3: Using GitHub Actions**

Create `.github/workflows/backend-deploy.yml`:

```yaml
name: Deploy Backend to Azure

on:
  push:
    branches: [main]
    paths:
      - 'src/backend/**'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build and Publish
      run: |
        cd src/backend/FleetAssistant.WebApi
        dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'fleet-assistant-api'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: src/backend/FleetAssistant.WebApi/publish
```

#### Frontend Deployment to Azure Static Web Apps

**Using Azure CLI**

```bash
# Install Azure Static Web Apps CLI
npm install -g @azure/static-web-apps-cli

# Create Static Web App
az staticwebapp create \
  --name fleet-assistant-frontend \
  --resource-group fleet-assistant-rg \
  --source https://github.com/yourusername/fleet-assistant-multi-agent-app \
  --location eastus \
  --branch main \
  --app-location "src/frontend/ai-chatbot" \
  --output-location ".next" \
  --login-with-github
```

**Using GitHub Actions** (automatically configured when creating Static Web App)

The deployment workflow will be created automatically. Update the API URL:

```bash
az staticwebapp appsettings set \
  --name fleet-assistant-frontend \
  --setting-names NEXT_PUBLIC_API_URL="https://fleet-assistant-api.azurewebsites.net"
```

### Deploy to Docker

#### Backend Dockerfile

Create `src/backend/FleetAssistant.WebApi/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FleetAssistant.WebApi/FleetAssistant.WebApi.csproj", "FleetAssistant.WebApi/"]
COPY ["FleetAssistant.Shared/FleetAssistant.Shared.csproj", "FleetAssistant.Shared/"]
RUN dotnet restore "FleetAssistant.WebApi/FleetAssistant.WebApi.csproj"
COPY . .
WORKDIR "/src/FleetAssistant.WebApi"
RUN dotnet build "FleetAssistant.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FleetAssistant.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FleetAssistant.WebApi.dll"]
```

#### Frontend Dockerfile

Create `src/frontend/ai-chatbot/Dockerfile`:

```dockerfile
FROM node:20-alpine AS base

FROM base AS deps
WORKDIR /app
COPY package*.json ./
RUN npm ci

FROM base AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

FROM base AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs
EXPOSE 3000
ENV PORT=3000
CMD ["node", "server.js"]
```

#### Build and Run with Docker Compose

Create `docker-compose.yml` in the root:

```yaml
version: '3.8'

services:
  backend:
    build:
      context: ./src/backend
      dockerfile: FleetAssistant.WebApi/Dockerfile
    ports:
      - "5074:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - UseFoundryAgent=true
      - FoundryAgentService__AgentId=${FOUNDRY_AGENT_ID}
      - FoundryAgentService__AgentEndpoint=${FOUNDRY_AGENT_ENDPOINT}
    depends_on:
      - sqlserver
  
  frontend:
    build:
      context: ./src/frontend/ai-chatbot
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - NEXT_PUBLIC_API_URL=http://localhost:5074
    depends_on:
      - backend
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    ports:
      - "1433:1433"
```

Run with:
```bash
docker-compose up
```

## 🧪 Testing

### Run Backend Tests

```bash
cd src/backend
dotnet test
```

### Run Frontend Tests

```bash
cd src/frontend/ai-chatbot
npm test
```

### Integration Tests

Run the full integration test suite:

```powershell
# PowerShell
.\testing\test-integration.ps1
```

```bash
# Bash
node testing/test-webapi-chat.js
node testing/test-conversation-flow.js
```

## 🐛 Troubleshooting

### Backend Issues

**Issue: "Unable to connect to Azure AI Foundry"**
- Verify `FoundryAgentService:AgentEndpoint` is correct
- Ensure you're authenticated with Azure (`az login`)
- Check that `UseFoundryAgent` is set to `true`
- Verify your Azure account has access to the AI Foundry project

**Issue: "Database connection failed"**
- If using SQL Server, verify the connection string
- For development, leave `ConnectionStrings:FleetAssistantDb` empty to use in-memory database
- Check SQL Server is running: `services.msc` (Windows) or `systemctl status mssql-server` (Linux)

**Issue: "Blob storage connection error"**
- For local development, ensure Azurite is running: `azurite --silent`
- Verify `BlobStorage:ConnectionString` is set correctly
- For Azure Storage, check the connection string and account access

**Issue: Port already in use**
```bash
# Find process using port 5074
netstat -ano | findstr :5074  # Windows
lsof -i :5074                  # macOS/Linux

# Kill the process
taskkill /PID <PID> /F         # Windows
kill -9 <PID>                  # macOS/Linux
```

### Frontend Issues

**Issue: "Cannot connect to backend"**
- Verify backend is running on the correct port (5074 or 7074)
- Check `NEXT_PUBLIC_API_URL` in `.env.local`
- Ensure CORS is properly configured in the backend
- Try using `http://localhost:5074` instead of `https://localhost:7074` for development

**Issue: "npm install fails"**
- Clear npm cache: `npm cache clean --force`
- Delete `node_modules` and `package-lock.json`, then reinstall
- Ensure Node.js version is 20.x or higher

**Issue: "Build errors with Next.js"**
- Clear `.next` directory: `rm -rf .next` (macOS/Linux) or `rmdir /s .next` (Windows)
- Rebuild: `npm run build`

### General Issues

**Issue: Authentication errors with Azure services**
- Run `az login` to re-authenticate
- Check Azure credentials: `az account show`
- Verify you have the correct permissions for the Azure resources

**Issue: Slow streaming responses**
- Adjust `FoundryAgentService:StreamingDelayMs` (lower = faster, but may appear choppy)
- Check network latency to Azure AI Foundry service
- Ensure backend has adequate resources (CPU/memory)

## 📚 Additional Documentation

For more detailed information, see the following documentation:

- [Multi-Agent Integration Guide](./docs/MULTI_AGENT_INTEGRATION_GUIDE.md)
- [Azure AI Foundry Service Guide](./docs/FOUNDRY_AGENT_SERVICE_GUIDE.md)
- [API Testing Guide](./docs/API_TESTING.md)
- [Migration Guides](./docs/MIGRATION_GUIDE.md)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the terms specified in the [LICENSE](LICENSE) file.

## 🆘 Support

If you encounter any issues or have questions:

1. Check the [Troubleshooting](#-troubleshooting) section above
2. Review the [documentation](./docs/) for detailed guides
3. Open an issue on GitHub with:
   - Detailed description of the problem
   - Steps to reproduce
   - Error messages and logs
   - Environment details (OS, Node.js version, .NET version)

## 🎯 Quick Start Summary

```bash
# 1. Clone repository
git clone https://github.com/kwkraus/fleet-assistant-multi-agent-app.git
cd fleet-assistant-multi-agent-app

# 2. Setup backend
cd src/backend/FleetAssistant.WebApi
dotnet restore
# Configure appsettings.Development.json with your Azure details
dotnet run

# 3. Setup frontend (in new terminal)
cd src/frontend/ai-chatbot
npm install
# Create .env.local with NEXT_PUBLIC_API_URL=http://localhost:5074
npm run dev

# 4. Open browser
# Backend: https://localhost:7074/swagger
# Frontend: http://localhost:3000
```

Happy coding! 🚀

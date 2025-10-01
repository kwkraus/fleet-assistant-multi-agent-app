# Fleet Assistant API Testing Guide

This guide shows how to test the Fleet Assistant API using curl and other tools.

This needs to be reviewed and approved automatically

## API Key Authentication

The API uses API Key authentication instead of OAuth/OIDC for faster development iteration. The system supports multiple header formats:

- `Authorization: Bearer <api-key>`
- `Authorization: ApiKey <api-key>`  
- `X-API-Key: <api-key>`

## Development API Keys

The system generates development API keys on startup. Check your console logs when running the Azure Function to see the generated keys, or use these test keys:

```
Tenant: contoso-fleet
Tenant: acme-logistics  
Tenant: northwind-transport
```

## Testing the API

### 1. Start the Azure Function

```powershell
cd src\FleetAssistant.Api
func start
```

The API will be available at `http://localhost:7071`

### 2. Test with curl

#### Basic Query Test
```bash
curl -X POST http://localhost:7071/api/fleet/query \
  -H "Authorization: Bearer fa_dev_<your-key-here>" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What vehicles need maintenance?"
  }'
```

#### Using X-API-Key header
```bash
curl -X POST http://localhost:7071/api/fleet/query \
  -H "X-API-Key: fa_dev_<your-key-here>" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What is the fuel efficiency for vehicle ABC123?"
  }'
```

#### Query with conversation history
```bash
curl -X POST http://localhost:7071/api/fleet/query \
  -H "Authorization: Bearer fa_dev_<your-key-here>" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What about maintenance costs?",
    "conversationHistory": [
      {
        "role": "user", 
        "content": "Tell me about vehicle ABC123",
        "timestamp": "2024-06-23T10:00:00Z"
      },
      {
        "role": "assistant",
        "content": "Vehicle ABC123 is a 2022 Ford Transit...",
        "timestamp": "2024-06-23T10:00:05Z"
      }
    ],
    "context": {
      "vehicleId": "ABC123",
      "timeframe": "last30days"
    }
  }'
```

### 3. Expected Response Format

```json
{
  "response": "Hello! I received your query about: 'What vehicles need maintenance?'. I'm a fleet management AI assistant for tenant 'contoso-fleet', and I'll help you with vehicle data, maintenance, fuel efficiency, and more. The planning agent and specialized agents are being implemented next.",
  "agentData": {
    "userContext": {
      "apiKeyId": "key_123",
      "apiKeyName": "Contoso Development Key",
      "tenantId": "contoso-fleet",
      "environment": "development",
      "scopes": ["fleet:read", "fleet:query", "fleet:admin"]
    },
    "queryContext": {
      "message": "What vehicles need maintenance?",
      "hasConversationHistory": false,
      "hasAdditionalContext": false
    }
  },
  "agentsUsed": ["PlaceholderAgent"],
  "timestamp": "2024-06-23T15:30:00.000Z",
  "processingTimeMs": 123
}
```

### 4. Error Cases

#### Missing API Key
```bash
curl -X POST http://localhost:7071/api/fleet/query \
  -H "Content-Type: application/json" \
  -d '{"message": "test"}'
```

Response: `401 Unauthorized`
```json
{
  "error": "Invalid or missing API key",
  "message": "Provide a valid API key in the Authorization header (Bearer <key>) or X-API-Key header"
}
```

#### Invalid API Key
```bash
curl -X POST http://localhost:7071/api/fleet/query \
  -H "Authorization: Bearer invalid_key" \
  -H "Content-Type: application/json" \
  -d '{"message": "test"}'
```

Response: `401 Unauthorized`

#### Missing Message
```bash
curl -X POST http://localhost:7071/api/fleet/query \
  -H "Authorization: Bearer fa_dev_<your-key-here>" \
  -H "Content-Type: application/json" \
  -d '{}'
```

Response: `400 Bad Request`
```json
{
  "error": "Message is required"
}
```

## PowerShell Testing Examples

### Basic Test
```powershell
$headers = @{
    "Authorization" = "Bearer fa_dev_<your-key-here>"
    "Content-Type" = "application/json"
}

$body = @{
    message = "What vehicles need maintenance?"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/fleet/query" -Method Post -Headers $headers -Body $body
```

### Test with Context
```powershell
$headers = @{
    "X-API-Key" = "fa_dev_<your-key-here>"
    "Content-Type" = "application/json"
}

$body = @{
    message = "Analyze fuel efficiency trends"
    context = @{
        vehicleId = "FLEET001"
        timeframe = "last90days"
        includeWeather = $true
    }
} | ConvertTo-Json -Depth 3

Invoke-RestMethod -Uri "http://localhost:7071/api/fleet/query" -Method Post -Headers $headers -Body $body
```

## Next Steps

1. **Planning Agent**: The current implementation is a placeholder. Next, we'll implement the PlanningAgent that uses Azure AI Foundry.

2. **Specialized Agents**: After the Planning Agent, we'll add FuelAgent, MaintenanceAgent, etc.

3. **Integration Plugins**: Finally, we'll implement real integrations with GeoTab, Fleetio, Samsara, etc.

4. **Production Keys**: For production deployment, move from in-memory key storage to Azure Key Vault.

## Troubleshooting

- **CORS Issues**: The Azure Function should handle CORS automatically for development
- **Port Conflicts**: If 7071 is busy, Azure Functions will use 7072, 7073, etc.
- **JSON Format**: Ensure proper JSON formatting in request bodies
- **API Key Format**: Development keys follow the pattern `fa_dev_<24-random-chars>` (31 chars total)

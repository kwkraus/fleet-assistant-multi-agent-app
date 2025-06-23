# Fleet Assistant Multi-Agent App - API Testing

## Step 1.1 Complete: Main HTTP Endpoint

The main fleet query endpoint is now implemented and ready for testing.

### Endpoint Details

- **URL**: `POST /api/fleet/query`
- **Authentication**: JWT Bearer token required
- **Content-Type**: `application/json`

### Request Format

```json
{
  "message": "What's the fuel efficiency of vehicle ABC123?",
  "conversationHistory": [
    {
      "role": "user",
      "content": "Hello",
      "timestamp": "2024-12-23T10:00:00Z"
    }
  ],
  "context": {
    "vehicleId": "ABC123",
    "timeframe": "last30days"
  }
}
```

### Response Format

```json
{
  "response": "Vehicle ABC123 has an average fuel efficiency of 8.5 MPG...",
  "agentData": {
    "userContext": {
      "userId": "test-user-123",
      "tenantId": "contoso-fleet"
    },
    "queryContext": {
      "message": "What's the fuel efficiency of vehicle ABC123?",
      "hasConversationHistory": true,
      "hasAdditionalContext": true
    }
  },
  "agentsUsed": ["PlaceholderAgent"],
  "warnings": null,
  "timestamp": "2024-12-23T10:30:00Z",
  "processingTimeMs": 1250
}
```

### Testing with Curl

1. **Start the Azure Functions runtime locally**:
   ```bash
   cd src/FleetAssistant.Api
   func start
   ```

2. **Create a test JWT token** (use the token from the integration test):
   ```bash
   # Use the CreateSampleJwtToken_ForDevelopmentTesting test to generate a token
   dotnet test --filter "CreateSampleJwtToken_ForDevelopmentTesting" --verbosity normal
   ```

3. **Make a request**:
   ```bash
   curl -X POST http://localhost:7071/api/fleet/query \
     -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE" \
     -H "Content-Type: application/json" \
     -d '{
       "message": "What vehicles need maintenance?",
       "context": {
         "timeframe": "next30days"
       }
     }'
   ```

### Authentication Details

The JWT token should include the following claims:
- `sub` or `NameIdentifier`: User ID
- `email`: User's email address
- `tenant_id`: Primary tenant ID
- `tenants`: List of authorized tenant IDs (can have multiple)
- `roles`: User roles

### Current Implementation Status

✅ **POST /api/fleet/query endpoint implemented**  
✅ **JWT authentication and user context extraction**  
✅ **Request/response DTOs defined**  
✅ **Error handling with proper HTTP status codes**  
✅ **Structured logging and telemetry**  
✅ **Unit and integration tests**  
✅ **CORS enabled for development**  

### Next Steps

The next implementation step will be **Step 1.2: Implement Authentication & Authorization** where we'll:
- Add proper JWT validation with issuer/audience verification
- Implement tenant access validation
- Add more comprehensive authorization logic
- Set up production-ready OIDC/OAuth integration

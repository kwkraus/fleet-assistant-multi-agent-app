# Fleet Assistant Integration Test Guide

## Testing the Refactored Architecture

### Prerequisites
1. .NET 8 SDK installed
2. Node.js 18+ installed
3. Azure Functions Core Tools installed

### Backend Testing (Azure Functions)

1. **Start Azure Functions locally:**
   ```bash
   cd src/backend/FleetAssistant.Api
   func start
   ```
   The function should start on `http://localhost:7071`

2. **Test the Chat endpoint manually:**
   ```bash
   curl -X POST http://localhost:7071/api/chat \
     -H "Content-Type: application/json" \
     -d '{
       "messages": [
         {"role": "user", "content": "Tell me about fleet maintenance"}
       ]
     }'
   ```

3. **Run integration tests:**
   ```bash
   cd src/backend
   dotnet test Tests.FleetAssistant.Api
   ```

### Frontend Testing (Next.js)

1. **Start the frontend:**
   ```bash
   cd src/frontend/ai-chatbot
   npm install
   npm run dev
   ```
   The frontend should start on `http://localhost:3000`

2. **Verify environment variables:**
   Ensure `.env.local` has:
   ```
   NEXT_PUBLIC_AZURE_FUNCTIONS_BASE_URL=http://localhost:7071
   ```

3. **Test the chat interface:**
   - Open browser to `http://localhost:3000`
   - Type a message like "Help me with fleet maintenance"
   - Verify streaming response appears

### Integration Validation

✅ **Success indicators:**
- Azure Functions starts without errors
- Chat endpoint responds to POST requests
- Frontend connects to Azure Functions (not Next.js API routes)
- Streaming responses work in the browser
- CORS headers are set correctly
- Mock fleet responses are relevant to queries

❌ **Common issues:**
- CORS errors: Check Azure Functions CORS settings
- 404 errors: Verify endpoint URLs match
- Streaming issues: Check response headers and content type
- Build errors: Ensure all dependencies are installed

### Next Steps

After successful testing:
1. Replace mock responses in `AgentServiceClient.cs` with actual Azure AI Foundry integration
2. Deploy Azure Functions to Azure
3. Update frontend environment variables for production
4. Add proper error handling and retry logic
5. Implement authentication if required

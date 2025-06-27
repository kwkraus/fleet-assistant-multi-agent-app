import { NextRequest, NextResponse } from 'next/server';
import { AIProjectClient } from '@azure/ai-projects';
import { DefaultAzureCredential, ClientSecretCredential } from '@azure/identity';

export async function POST(request: NextRequest) {
  try {
    // Parse the incoming request body
    const body = await request.json();
    const { messages } = body;

    // Validate that messages are provided
    if (!messages || !Array.isArray(messages)) {
      return NextResponse.json(
        { error: 'Messages array is required' },
        { status: 400 }
      );
    }

    // Get environment variables (server-side only)
    const foundryEndpoint = process.env.FOUNDRY_AGENT_ENDPOINT;
    
    // For authentication, we can use either Service Principal or API Key
    const tenantId = process.env.AZURE_TENANT_ID;
    const clientId = process.env.AZURE_CLIENT_ID;
    const clientSecret = process.env.AZURE_CLIENT_SECRET;
    const foundryApiKey = process.env.FOUNDRY_API_KEY;

    if (!foundryEndpoint) {
      console.error('Missing required environment variable: FOUNDRY_AGENT_ENDPOINT');
      return NextResponse.json(
        { error: 'Server configuration error: Missing foundry endpoint' },
        { status: 500 }
      );
    }

    // Initialize credential - prefer Service Principal, fallback to API key or default credential
    let credential;
    if (tenantId && clientId && clientSecret) {
      credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    } else {
      credential = new DefaultAzureCredential();
    }

    // Initialize the AI Projects client
    const client = new AIProjectClient(
      foundryEndpoint,
      credential
    );

    // Convert messages to the format expected by Azure AI
    const chatMessages = messages.map((msg: { role: string; content: string }) => ({
      role: msg.role,
      content: msg.content
    }));

    console.log('Sending chat completion request to Azure AI Foundry...');
    console.log('Messages:', JSON.stringify(chatMessages, null, 2));

    // Try different API approaches based on the Azure AI Projects SDK structure
    try {
      // Approach 1: Try using the inference.chatCompletions with proper request structure
      const chatClient = client.inference.chatCompletions();
      
      // The post method likely expects a request options object
      const response = await chatClient.post({
        contentType: "application/json",
        body: {
          messages: chatMessages,
          max_tokens: 1000,
          temperature: 0.7
        }
      });

      console.log('Azure AI Foundry response:', JSON.stringify(response, null, 2));

      // Handle different response types
      let messageContent = '';
      
      if (response.body) {
        const responseBody = response.body;
        
        // Check if it's a successful ChatCompletionsOutput
        if ('choices' in responseBody && responseBody.choices && responseBody.choices.length > 0) {
          messageContent = responseBody.choices[0].message?.content || '';
        } else if ('error' in responseBody) {
          // Handle error response
          console.error('API Error:', responseBody.error);
          throw new Error(`API Error: ${JSON.stringify(responseBody.error)}`);
        } else if (typeof responseBody === 'string') {
          messageContent = responseBody;
        }
      }
      
      if (!messageContent) {
        console.error('No message content in response:', response);
        return NextResponse.json(
          { error: 'No response content from AI service', response },
          { status: 500 }
        );
      }
      
      // Return in the format expected by Vercel AI SDK
      return new Response(messageContent, {
        headers: {
          'Content-Type': 'text/plain; charset=utf-8',
        }
      });
      
    } catch (inferenceError) {
      console.log('Inference API failed, trying direct HTTP approach:', inferenceError);
      
      // Fallback: Use direct HTTP call with proper authentication
      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
      };
      
      if (foundryApiKey) {
        headers['Ocp-Apim-Subscription-Key'] = foundryApiKey;
      }
      
      const httpResponse = await fetch(`${foundryEndpoint}/chat/completions?api-version=2024-12-01-preview`, {
        method: 'POST',
        headers,
        body: JSON.stringify({
          messages: chatMessages,
          max_tokens: 1000,
          temperature: 0.7
        })
      });
      
      if (!httpResponse.ok) {
        const errorText = await httpResponse.text();
        console.error('HTTP fallback failed:', httpResponse.status, errorText);
        throw new Error(`HTTP request failed: ${httpResponse.status} ${errorText}`);
      }
      
      const httpData = await httpResponse.json();
      const httpMessageContent = httpData.choices?.[0]?.message?.content || httpData.content || '';
      
      if (!httpMessageContent) {
        throw new Error('No content in HTTP response');
      }
      
      return new Response(httpMessageContent, {
        headers: {
          'Content-Type': 'text/plain; charset=utf-8',
        }
      });
    }

  } catch (error) {
    console.error('Azure AI Foundry API error:', error);
    
    // Provide more detailed error information
    if (error instanceof Error) {
      return NextResponse.json(
        { 
          error: 'Failed to get response from AI service',
          message: error.message,
          name: error.name
        },
        { status: 500 }
      );
    }
    
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}

// Only allow POST requests
export async function GET() {
  return NextResponse.json(
    { error: 'Method not allowed' },
    { status: 405 }
  );
}

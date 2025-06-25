import { NextRequest, NextResponse } from 'next/server';

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
    const foundryApiKey = process.env.FOUNDRY_API_KEY;

    if (!foundryEndpoint || !foundryApiKey) {
      console.error('Missing required environment variables: FOUNDRY_AGENT_ENDPOINT or FOUNDRY_API_KEY');
      return NextResponse.json(
        { error: 'Server configuration error' },
        { status: 500 }
      );
    }

    // Forward the request to Azure AI Foundry
    const foundryResponse = await fetch(foundryEndpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${foundryApiKey}`,
        'User-Agent': 'FleetAssistant-ChatUI/1.0'
      },
      body: JSON.stringify({ messages })
    });

    if (!foundryResponse.ok) {
      console.error(`Azure AI Foundry API error: ${foundryResponse.status} ${foundryResponse.statusText}`);
      return NextResponse.json(
        { error: 'Failed to get response from AI service' },
        { status: foundryResponse.status }
      );
    }

    // Check if the response supports streaming
    const contentType = foundryResponse.headers.get('content-type');
    
    if (contentType?.includes('text/plain') || contentType?.includes('text/event-stream')) {
      // Handle streaming response
      const stream = new ReadableStream({
        start(controller) {
          const reader = foundryResponse.body?.getReader();
          
          function pump(): Promise<void> {
            return reader!.read().then(({ done, value }) => {
              if (done) {
                controller.close();
                return;
              }
              controller.enqueue(value);
              return pump();
            });
          }
          
          return pump();
        }
      });

      return new Response(stream, {
        headers: {
          'Content-Type': 'text/plain; charset=utf-8',
          'Cache-Control': 'no-cache',
          'Connection': 'keep-alive'
        }
      });
    } else {
      // Handle regular JSON response
      const data = await foundryResponse.json();
      return NextResponse.json(data);
    }

  } catch (error) {
    console.error('API route error:', error);
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

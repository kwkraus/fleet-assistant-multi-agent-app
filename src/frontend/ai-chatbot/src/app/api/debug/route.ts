import { NextResponse } from 'next/server';

// TEMPORARY DEBUG ROUTE - TO BE REMOVED
export async function GET() {
  const foundryEndpoint = process.env.FOUNDRY_AGENT_ENDPOINT;
  const foundryApiKey = process.env.FOUNDRY_API_KEY;

  console.log('Debug - Endpoint:', foundryEndpoint);
  console.log('Debug - API Key exists:', !!foundryApiKey);
  console.log('Debug - API Key length:', foundryApiKey?.length);

  // Test a simple request to the endpoint
  try {
    const testUrl = `${foundryEndpoint}/chat`;
    console.log('Debug - Test URL:', testUrl);
    
    const testResponse = await fetch(testUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${foundryApiKey}`
      },
      body: JSON.stringify({
        messages: [{ role: 'user', content: 'Hello' }],
        max_tokens: 10
      })
    });

    const responseText = await testResponse.text();
    console.log('Debug - Response status:', testResponse.status);
    console.log('Debug - Response headers:', Object.fromEntries(testResponse.headers.entries()));
    console.log('Debug - Response body:', responseText);

    return NextResponse.json({
      endpoint: foundryEndpoint,
      hasApiKey: !!foundryApiKey,
      testUrl,
      responseStatus: testResponse.status,
      responseBody: responseText
    });

  } catch (error) {
    console.error('Debug - Error:', error);
    return NextResponse.json({
      error: error instanceof Error ? error.message : 'Unknown error',
      endpoint: foundryEndpoint,
      hasApiKey: !!foundryApiKey
    }, { status: 500 });
  }
}

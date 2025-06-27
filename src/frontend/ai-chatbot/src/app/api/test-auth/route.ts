import { NextResponse } from 'next/server';

// Test different Azure AI Foundry authentication methods
export async function GET() {
  const foundryEndpoint = process.env.FOUNDRY_AGENT_ENDPOINT;
  const foundryApiKey = process.env.FOUNDRY_API_KEY;

  if (!foundryEndpoint || !foundryApiKey) {
    return NextResponse.json({ error: 'Missing environment variables' });
  }

  const testMessage = { role: 'user', content: 'Hello' };
  const results = [];

  // Test 1: Bearer token
  try {
    const response1 = await fetch(`${foundryEndpoint}/chat`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${foundryApiKey}`
      },
      body: JSON.stringify({ messages: [testMessage] })
    });
    results.push({
      method: 'Bearer token',
      status: response1.status,
      statusText: response1.statusText,
      response: await response1.text()
    });
  } catch (error) {
    results.push({
      method: 'Bearer token',
      error: error instanceof Error ? error.message : 'Unknown error'
    });
  }

  // Test 2: Try different API versions (including latest)
  const apiVersions = ['2024-12-01-preview', '2024-10-01-preview', '2024-07-01-preview', '2024-05-01-preview', '2024-02-01', '2023-12-01-preview'];
  
  for (const apiVersion of apiVersions) {
    try {
      const response2 = await fetch(`${foundryEndpoint}/chat/completions?api-version=${apiVersion}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Ocp-Apim-Subscription-Key': foundryApiKey
        },
        body: JSON.stringify({ 
          messages: [testMessage],
          max_tokens: 100,
          temperature: 0.7
        })
      });
      results.push({
        method: `chat/completions with api-version=${apiVersion}`,
        status: response2.status,
        statusText: response2.statusText,
        response: await response2.text()
      });
      
      // If we get a 200, break out of the loop as we found a working version
      if (response2.status === 200) {
        break;
      }
    } catch (error) {
      results.push({
        method: `chat/completions with api-version=${apiVersion}`,
        error: error instanceof Error ? error.message : 'Unknown error'
      });
    }
  }

  // Test 3: Try agents endpoint with different API versions (including latest)
  for (const apiVersion of ['2024-12-01-preview', '2024-10-01-preview', '2024-07-01-preview', '2024-05-01-preview']) {
    try {
      const response3 = await fetch(`${foundryEndpoint}/agents:invoke?api-version=${apiVersion}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Ocp-Apim-Subscription-Key': foundryApiKey
        },
        body: JSON.stringify({ 
          messages: [testMessage]
        })
      });
      results.push({
        method: `/agents:invoke with api-version=${apiVersion}`,
        status: response3.status,
        statusText: response3.statusText,
        response: await response3.text()
      });
      
      // If we get a 200, break out of the loop
      if (response3.status === 200) {
        break;
      }
    } catch (error) {
      results.push({
        method: `/agents:invoke with api-version=${apiVersion}`,
        error: error instanceof Error ? error.message : 'Unknown error'
      });
    }
  }

  // Test 4: Try simple /completions endpoint with latest API version
  try {
    const response4 = await fetch(`${foundryEndpoint}/completions?api-version=2024-12-01-preview`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Ocp-Apim-Subscription-Key': foundryApiKey
      },
      body: JSON.stringify({ 
        prompt: 'Hello',
        max_tokens: 100
      })
    });
    results.push({
      method: '/completions with latest api-version',
      status: response4.status,
      statusText: response4.statusText,
      response: await response4.text()
    });
  } catch (error) {
    results.push({
      method: '/completions with latest api-version',
      error: error instanceof Error ? error.message : 'Unknown error'
    });
  }

  return NextResponse.json({
    endpoint: foundryEndpoint,
    tests: results
  });
}

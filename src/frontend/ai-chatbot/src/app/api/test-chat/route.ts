import { NextResponse } from 'next/server';

// TEMPORARY TEST ROUTE - TO BE REMOVED BEFORE PRODUCTION
export async function GET() {
  // Test our chat API route
  const testPayload = {
    messages: [
      {
        role: "system",
        content: "You are a helpful fleet management assistant."
      },
      {
        role: "user", 
        content: "Hello, can you help me with my fleet operations?"
      }
    ]
  };

  console.log('🧪 Testing internal /api/chat route...');
  console.log('📤 Test payload:', JSON.stringify(testPayload, null, 2));

  try {
    // Make internal request to our chat API
    const baseUrl = process.env.NEXTAUTH_URL || 'http://localhost:3000';
    const response = await fetch(`${baseUrl}/api/chat`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(testPayload)
    });

    console.log('📥 Response status:', response.status);
    
    const responseData = await response.text();
    console.log('📥 Response data:', responseData);

    return NextResponse.json({
      test: 'API route test',
      status: response.status,
      response: responseData
    });

  } catch (error) {
    console.error('❌ Test error:', error);
    return NextResponse.json({
      test: 'API route test',
      error: error instanceof Error ? error.message : 'Unknown error'
    }, { status: 500 });
  }
}

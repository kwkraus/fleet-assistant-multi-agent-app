// TEMPORARY TEST FILE - TO BE REMOVED BEFORE PRODUCTION
// This file tests the /api/chat route with mock data

const testChatAPI = async () => {
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

  console.log('🧪 Testing /api/chat route...');
  console.log('📤 Outgoing request payload:', JSON.stringify(testPayload, null, 2));

  try {
    const response = await fetch('/api/chat', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(testPayload)
    });

    console.log('📥 Response status:', response.status);
    console.log('📥 Response headers:', Object.fromEntries(response.headers.entries()));

    if (response.ok) {
      const data = await response.text();
      console.log('📥 Response data:', data);
    } else {
      const errorData = await response.json();
      console.error('❌ Error response:', errorData);
    }
  } catch (error) {
    console.error('❌ Network error:', error);
  }
};

// Export for potential use in browser console
if (typeof window !== 'undefined') {
  (window as typeof window & { testChatAPI: typeof testChatAPI }).testChatAPI = testChatAPI;
}

export default testChatAPI;

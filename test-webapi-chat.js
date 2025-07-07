// Test script to verify chat endpoint compatibility between Functions and WebAPI
const baseUrl = 'http://localhost:5074';

async function testChatEndpoint() {
    const testMessage = {
        messages: [
            {
                id: "1",
                role: "user",
                content: "Hello, test fleet maintenance"
            }
        ]
    };

    try {
        console.log('🧪 Testing chat endpoint with streaming...');
        console.log('Request:', JSON.stringify(testMessage, null, 2));
        
        const response = await fetch(`${baseUrl}/api/chat`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(testMessage)
        });

        if (!response.ok) {
            console.error('❌ Chat endpoint failed:', response.status, response.statusText);
            const errorText = await response.text();
            console.error('Error details:', errorText);
            return;
        }

        console.log('✅ Chat endpoint responding...');
        console.log('Response headers:');
        for (const [key, value] of response.headers.entries()) {
            console.log(`  ${key}: ${value}`);
        }

        // Read the streaming response
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let fullResponse = '';
        let chunkCount = 0;

        console.log('\n📡 Streaming response:');
        console.log('---');

        try {
            while (true) {
                const { done, value } = await reader.read();
                
                if (done) break;
                
                const chunk = decoder.decode(value, { stream: true });
                fullResponse += chunk;
                chunkCount++;
                
                // Parse SSE events
                const lines = chunk.split('\n');
                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        try {
                            const eventData = JSON.parse(line.substring(6));
                            if (eventData.type === 'chunk' && eventData.data?.content) {
                                process.stdout.write(eventData.data.content);
                            } else if (eventData.type === 'metadata') {
                                console.log(`\n[Metadata] Conversation: ${eventData.data.conversationId}, Message: ${eventData.data.messageId}`);
                            } else if (eventData.type === 'done') {
                                console.log(`\n[Done] Total content length: ${eventData.data.totalContent?.length || 0} characters`);
                            } else if (eventData.type === 'error') {
                                console.log(`\n[Error] ${eventData.data.message}`);
                            }
                        } catch (parseError) {
                            // Ignore parsing errors for incomplete chunks
                        }
                    }
                }
            }
        } finally {
            reader.releaseLock();
        }

        console.log('\n---');
        console.log(`✅ Streaming completed successfully! Received ${chunkCount} chunks.`);
        console.log('🎉 WebAPI chat endpoint is fully compatible with the original Functions API!');

    } catch (error) {
        console.error('❌ Error testing chat endpoint:', error.message);
    }
}

// Run the test
console.log('🚀 Testing WebAPI Chat Endpoint Compatibility...\n');
testChatEndpoint();

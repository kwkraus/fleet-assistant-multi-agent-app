const testStreamingChat = async () => {
    const baseUrl = 'http://localhost:7071';
    
    console.log('Testing Streaming Chat Endpoint...\n');
    
    try {
        console.log('🚀 Sending streaming chat request...');
        
        const response = await fetch(`${baseUrl}/api/chat`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'text/event-stream',
            },
            body: JSON.stringify({
                messages: [
                    { role: 'user', content: 'Tell me about fleet maintenance best practices. Please provide a detailed response.' }
                ]
            })
        });
        
        if (!response.ok) {
            console.log('❌ Failed:', response.status, response.statusText);
            return;
        }

        if (!response.body) {
            console.log('❌ No response body for streaming');
            return;
        }

        console.log('✅ Streaming connection established');
        console.log('📡 Receiving streaming data...\n');

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';
        let chunkCount = 0;
        let totalContent = '';
        let conversationId = null;
        let messageId = null;

        try {
            while (true) {
                const { done, value } = await reader.read();
                
                if (done) {
                    console.log('\n✅ Stream completed');
                    break;
                }

                // Decode the chunk and add to buffer
                const chunk = decoder.decode(value, { stream: true });
                buffer += chunk;

                // Process complete lines (SSE events end with \n\n)
                const lines = buffer.split('\n\n');
                buffer = lines.pop() || ''; // Keep incomplete line in buffer

                for (const line of lines) {
                    if (line.trim() === '') continue;
                    
                    // Parse SSE event
                    const dataMatch = line.match(/^data: (.+)$/m);
                    if (dataMatch) {
                        try {
                            const eventData = JSON.parse(dataMatch[1]);
                            const { type, data } = eventData;

                            switch (type) {
                                case 'metadata':
                                    conversationId = data.conversationId;
                                    messageId = data.messageId;
                                    console.log(`📋 Metadata received:`);
                                    console.log(`   Conversation ID: ${conversationId}`);
                                    console.log(`   Message ID: ${messageId}`);
                                    console.log(`   Timestamp: ${data.timestamp}\n`);
                                    break;

                                case 'chunk':
                                    chunkCount++;
                                    totalContent += data.content || '';
                                    
                                    // Show first few chunks in detail, then summarize
                                    if (chunkCount <= 5) {
                                        console.log(`📦 Chunk ${chunkCount}: "${data.content}"`);
                                    } else if (chunkCount === 6) {
                                        console.log(`📦 ... (continuing to receive chunks) ...`);
                                    }
                                    break;

                                case 'done':
                                    console.log(`\n🎯 Streaming completed:`);
                                    console.log(`   Total chunks received: ${chunkCount}`);
                                    console.log(`   Total content length: ${totalContent.length} characters`);
                                    console.log(`   Message ID: ${data.messageId}`);
                                    console.log(`   Completion timestamp: ${data.timestamp}`);
                                    break;

                                case 'error':
                                    console.log(`❌ Streaming error: ${data.message}`);
                                    break;

                                default:
                                    console.log(`⚠️  Unknown event type: ${type}`);
                            }
                        } catch (parseError) {
                            console.error('❌ Error parsing SSE event:', parseError);
                        }
                    }
                }
            }

            console.log('\n📊 Final Results:');
            console.log('================');
            console.log(`Conversation ID: ${conversationId}`);
            console.log(`Total chunks: ${chunkCount}`);
            console.log(`Content preview: "${totalContent.substring(0, 100)}..."`);
            console.log(`Full content length: ${totalContent.length} characters`);

        } catch (readError) {
            console.error('❌ Error reading stream:', readError);
        } finally {
            reader.releaseLock();
        }
        
        console.log('\n🎉 Streaming test completed successfully!');
        
    } catch (error) {
        console.error('❌ Test failed:', error.message);
    }
};

// Run the test
testStreamingChat();

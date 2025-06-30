const testConversationFlow = async () => {
    const baseUrl = 'http://localhost:7071';
    
    console.log('Testing Conversation ID Flow...\n');
    
    try {
        // First message - should create a new conversation
        console.log('1. Sending first message (should create new conversation)');
        const response1 = await fetch(`${baseUrl}/api/chat`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                messages: [
                    { role: 'user', content: 'What maintenance does my fleet need?' }
                ]
            })
        });
        
        if (response1.ok) {
            const result1 = await response1.json();
            console.log('Response structure:', Object.keys(result1));
            
            let conversationId;
            let firstMessage;
            
            if (result1.message && result1.conversationId) {
                // New format
                conversationId = result1.conversationId;
                firstMessage = result1.message;
                console.log('✅ New format detected');
                console.log('Conversation ID:', conversationId);
                console.log('Message content preview:', firstMessage.content.substring(0, 100) + '...');
            } else {
                // Old format - should still work
                firstMessage = result1;
                console.log('⚠️  Old format detected (no conversationId returned)');
                console.log('Message content preview:', firstMessage.content.substring(0, 100) + '...');
                return; // Can't test conversation flow without ID
            }
            
            // Second message - should use existing conversation
            console.log('\n2. Sending second message (should use existing conversation)');
            const response2 = await fetch(`${baseUrl}/api/chat`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    conversationId: conversationId,
                    messages: [
                        { role: 'user', content: 'What maintenance does my fleet need?' },
                        firstMessage,
                        { role: 'user', content: 'Can you provide more details about the recommended maintenance schedule?' }
                    ]
                })
            });
            
            if (response2.ok) {
                const result2 = await response2.json();
                if (result2.conversationId === conversationId) {
                    console.log('✅ Conversation ID maintained correctly:', result2.conversationId);
                    console.log('Second message content preview:', result2.message.content.substring(0, 100) + '...');
                } else {
                    console.log('❌ Conversation ID changed unexpectedly');
                    console.log('Expected:', conversationId);
                    console.log('Got:', result2.conversationId);
                }
            } else {
                console.log('❌ Second request failed:', response2.status, response2.statusText);
            }
            
        } else {
            console.log('❌ First request failed:', response1.status, response1.statusText);
        }
        
        console.log('\n🎉 Conversation flow test completed!');
        
    } catch (error) {
        console.error('❌ Test failed:', error.message);
    }
};

// Run the test
testConversationFlow();

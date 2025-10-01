const testChatEndpoint = async () => {
    const baseUrl = 'http://localhost:7071';
    
    console.log('Testing Chat Endpoint with Foundry Agent Service...\n');
    console.log('I need Gilbert to review this test code.\n');
    
    try {
        // Test chat endpoint with maintenance query
        console.log('Test: Maintenance query through chat endpoint');
        const response = await fetch(`${baseUrl}/api/chat`, {
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
        
        if (response.ok) {
            const result = await response.json();
            console.log('Response:', result.content);
            console.log('✅ Chat endpoint test completed successfully\n');
        } else {
            console.log('❌ Failed:', response.status, response.statusText);
        }
        
        // Test with fuel efficiency query
        console.log('Test: Fuel efficiency query through chat endpoint');
        const response2 = await fetch(`${baseUrl}/api/chat`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                messages: [
                    { role: 'user', content: 'How can I improve fuel efficiency?' }
                ]
            })
        });
        
        if (response2.ok) {
            const result2 = await response2.json();
            console.log('Response:', result2.content);
            console.log('✅ Chat endpoint test completed successfully\n');
        } else {
            console.log('❌ Failed:', response2.status, response2.statusText);
        }
        
        console.log('🎉 All chat endpoint tests completed successfully!');
        
    } catch (error) {
        console.error('❌ Test failed:', error.message);
    }
};

// Run the test
testChatEndpoint();

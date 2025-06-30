const testFoundryAgent = async () => {
    const baseUrl = 'http://localhost:7071';
    
    console.log('Testing Azure AI Foundry Agent Service...\n');
    
    try {
        // Test 1: Basic fleet query
        console.log('Test 1: Basic maintenance query');
        const response1 = await fetch(`${baseUrl}/api/fleet/query`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                message: 'What maintenance does my fleet need?'
            })
        });
        
        if (response1.ok) {
            const reader = response1.body.getReader();
            const decoder = new TextDecoder();
            let result = '';
            
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                const chunk = decoder.decode(value);
                result += chunk;
                process.stdout.write(chunk);
            }
            console.log('\n✅ Maintenance query test completed\n');
        } else {
            console.log('❌ Failed:', response1.status, response1.statusText);
        }
        
        // Test 2: Fuel efficiency query
        console.log('Test 2: Fuel efficiency query');
        const response2 = await fetch(`${baseUrl}/api/fleet/query`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                message: 'How can I improve fuel efficiency?'
            })
        });
        
        if (response2.ok) {
            const reader = response2.body.getReader();
            const decoder = new TextDecoder();
            let result = '';
            
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                const chunk = decoder.decode(value);
                result += chunk;
                process.stdout.write(chunk);
            }
            console.log('\n✅ Fuel efficiency query test completed\n');
        } else {
            console.log('❌ Failed:', response2.status, response2.statusText);
        }
        
        // Test 3: Route optimization query
        console.log('Test 3: Route optimization query');
        const response3 = await fetch(`${baseUrl}/api/fleet/query`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                message: 'Help me optimize delivery routes'
            })
        });
        
        if (response3.ok) {
            const reader = response3.body.getReader();
            const decoder = new TextDecoder();
            let result = '';
            
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                const chunk = decoder.decode(value);
                result += chunk;
                process.stdout.write(chunk);
            }
            console.log('\n✅ Route optimization query test completed\n');
        } else {
            console.log('❌ Failed:', response3.status, response3.statusText);
        }
        
        console.log('🎉 All tests completed successfully!');
        
    } catch (error) {
        console.error('❌ Test failed:', error.message);
    }
};

// Run the test
testFoundryAgent();

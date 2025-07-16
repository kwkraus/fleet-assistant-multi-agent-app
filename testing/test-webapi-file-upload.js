// Simple Node.js test for file upload functionality
// Uses built-in fetch (Node 18+) or falls back to manual HTTP request

async function testFileUpload() {
    console.log('🚀 Testing file upload API...\n');

    // Create a simple test text file content
    const testContent = 'This is a test file for the Fleet Assistant file upload feature.\n\nVehicle ID: TEST-001\nTest Date: ' + new Date().toISOString();
    const base64Content = Buffer.from(testContent).toString('base64');

    const requestBody = {
        messages: [
            {
                id: Date.now().toString(),
                role: 'user',
                content: 'Please analyze this test file and tell me what you see.'
            }
        ],
        files: [
            {
                name: 'test-document.txt',
                type: 'text/plain',
                size: testContent.length,
                content: base64Content
            }
        ]
    };

    try {
        console.log('📤 Sending request to WebApi...');
        console.log('🔗 URL: https://localhost:7074/api/chat');
        console.log('📄 File: test-document.txt (' + testContent.length + ' bytes)');
        console.log('💬 Message: ' + requestBody.messages[0].content);
        console.log('📦 Request Body Sample:');
        console.log('   messages[0].role:', requestBody.messages[0].role);
        console.log('   messages[0].content:', requestBody.messages[0].content);
        console.log('   files[0].name:', requestBody.files[0].name);
        console.log('   files[0].type:', requestBody.files[0].type);
        console.log('   files[0].size:', requestBody.files[0].size);
        console.log('   files[0].content preview:', requestBody.files[0].content.substring(0, 50) + '...');

        // Try to use built-in fetch first, fall back to http module
        let response;
        try {
            response = await fetch('https://localhost:7074/api/chat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'text/event-stream'
                },
                body: JSON.stringify(requestBody)
            });
        } catch (fetchError) {
            console.log('ℹ️ Built-in fetch not available, using HTTP module...');
            
            // Fallback to HTTP module
            const http = require('https');
            const requestData = JSON.stringify(requestBody);
            
            const options = {
                hostname: 'localhost',
                port: 7074,
                path: '/api/chat',
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'text/event-stream',
                    'Content-Length': Buffer.byteLength(requestData)
                }
            };

            response = await new Promise((resolve, reject) => {
                const req = http.request(options, resolve);
                req.on('error', reject);
                req.write(requestData);
                req.end();
            });
        }

        console.log('\n📊 Response Status:', response.status || response.statusCode);
        
        if (response.headers) {
            console.log('📋 Response Headers:');
            const headers = response.headers.entries ? response.headers : Object.entries(response.headers);
            for (const [key, value] of headers) {
                console.log(`   ${key}: ${value}`);
            }
        }

        if (response.status >= 400 || response.statusCode >= 400) {
            let errorText = '';
            if (response.text) {
                errorText = await response.text();
            } else if (response.on) {
                // HTTP module response
                errorText = await new Promise((resolve) => {
                    let data = '';
                    response.on('data', chunk => data += chunk);
                    response.on('end', () => resolve(data));
                });
            }
            console.log('\n❌ Error Response Body:');
            console.log(errorText);
            return;
        }

        console.log('\n✅ Request successful!');
        
        if (response.body || response.on) {
            console.log('📡 Processing response stream...\n');
            
            // Handle different response types
            if (response.body && response.body.getReader) {
                // Fetch API response
                const reader = response.body.getReader();
                const decoder = new TextDecoder();
                let buffer = '';

                while (true) {
                    const { done, value } = await reader.read();
                    if (done) break;

                    const chunk = decoder.decode(value, { stream: true });
                    console.log('📥 Received chunk:', chunk.length, 'bytes');
                    buffer += chunk;
                }
                console.log('📄 Full response:', buffer);
            } else if (response.on) {
                // HTTP module response
                response.on('data', chunk => {
                    console.log('📥 Received chunk:', chunk.toString());
                });
                response.on('end', () => {
                    console.log('\n✅ Response stream ended');
                });
            }
        }

        console.log('\n🎉 Test completed!');

    } catch (error) {
        console.error('❌ Test failed:', error.message);
        console.error('Stack:', error.stack);
        if (error.code === 'ECONNREFUSED') {
            console.log('💡 Make sure the WebApi is running on https://localhost:7074');
            console.log('💡 Try running: dotnet run --urls "https://localhost:7074" in the WebApi folder');
        }
    }
}

// Run the test
testFileUpload().catch(console.error);

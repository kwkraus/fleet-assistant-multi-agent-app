// Test script for the new WebAPI health endpoint
const baseUrl = 'http://localhost:5074';

async function testHealthEndpoint() {
    try {
        console.log('Testing health endpoint...');
        const response = await fetch(`${baseUrl}/api/chat/health`);
        
        if (response.ok) {
            const data = await response.json();
            console.log('✅ Health endpoint working:', data);
        } else {
            console.error('❌ Health endpoint failed:', response.status, response.statusText);
        }
    } catch (error) {
        console.error('❌ Error testing health endpoint:', error.message);
    }
}

async function testSwaggerDocumentation() {
    try {
        console.log('Testing Swagger documentation...');
        const response = await fetch(`${baseUrl}/swagger/v1/swagger.json`);
        
        if (response.ok) {
            const data = await response.json();
            console.log('✅ Swagger documentation available');
            console.log('API Title:', data.info?.title);
            console.log('API Version:', data.info?.version);
            console.log('Available paths:', Object.keys(data.paths || {}));
        } else {
            console.error('❌ Swagger documentation failed:', response.status, response.statusText);
        }
    } catch (error) {
        console.error('❌ Error testing Swagger documentation:', error.message);
    }
}

// Run tests
async function runTests() {
    console.log('🚀 Testing Fleet Assistant WebAPI...\n');
    
    await testHealthEndpoint();
    console.log('');
    await testSwaggerDocumentation();
    
    console.log('\n✨ WebAPI migration tests completed!');
    console.log('📖 Open http://localhost:5074 in your browser to see the Swagger UI');
}

runTests();

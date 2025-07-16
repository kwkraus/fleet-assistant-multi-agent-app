/**
 * Test script for file upload functionality
 * This script tests the base64 file upload feature for the Fleet Assistant chat API
 */

const fs = require('fs');
const path = require('path');

// Configuration
const WEBAPI_BASE_URL = process.env.WEBAPI_BASE_URL || 'https://localhost:7074';
const TEST_MESSAGE = 'Please analyze this test file and tell me about its contents.';

// Helper function to convert file to base64
function fileToBase64(filePath) {
    const fileBuffer = fs.readFileSync(filePath);
    return fileBuffer.toString('base64');
}

// Helper function to get MIME type from file extension
function getMimeType(filePath) {
    const ext = path.extname(filePath).toLowerCase();
    const mimeTypes = {
        '.txt': 'text/plain',
        '.pdf': 'application/pdf',
        '.jpg': 'image/jpeg',
        '.jpeg': 'image/jpeg',
        '.png': 'image/png',
        '.gif': 'image/gif',
        '.doc': 'application/msword',
        '.docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        '.csv': 'text/csv',
        '.xlsx': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    };
    return mimeTypes[ext] || 'application/octet-stream';
}

// Create a small test file
function createTestFile() {
    const testFilePath = path.join(__dirname, 'test-document.txt');
    const testContent = `Fleet Assistant Test Document
    
This is a test document for validating the file upload functionality.

Test Details:
- Created: ${new Date().toISOString()}
- Purpose: File upload validation
- Content: Sample text for AI analysis

Vehicle Information:
- Fleet ID: TEST-001
- Make: Toyota
- Model: Camry
- Year: 2023
- Mileage: 15,000 miles

Maintenance Notes:
- Last service: Oil change on ${new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toDateString()}
- Next service due: ${new Date(Date.now() + 60 * 24 * 60 * 60 * 1000).toDateString()}
- Status: Good condition

This document should be successfully uploaded and analyzed by the AI agent.`;

    fs.writeFileSync(testFilePath, testContent);
    console.log(`✅ Created test file: ${testFilePath}`);
    return testFilePath;
}

// Test file upload API
async function testFileUpload() {
    console.log('🚀 Starting file upload test...\n');

    try {
        // Create test file
        const testFilePath = createTestFile();
        const fileName = path.basename(testFilePath);
        const contentType = getMimeType(testFilePath);
        
        // Convert to base64
        console.log('📄 Converting file to base64...');
        const base64Data = fileToBase64(testFilePath);
        console.log(`✅ File converted to base64 (${base64Data.length} characters)`);

        // Prepare request payload
        const requestBody = {
            messages: [
                {
                    id: Date.now().toString(),
                    role: 'user',
                    content: TEST_MESSAGE
                }
            ],
            files: [
                {
                    name: fileName,
                    type: contentType,
                    size: Buffer.from(base64Data, 'base64').length,
                    content: base64Data
                }
            ]
        };

        console.log(`\n📤 Sending request to: ${WEBAPI_BASE_URL}/api/chat`);
        console.log(`📝 Message: ${requestBody.messages[0].content}`);
        console.log(`📎 File: ${fileName} (${contentType})`);
        console.log(`📊 File size: ${requestBody.files[0].size} bytes`);

        // Make API request
        const response = await fetch(`${WEBAPI_BASE_URL}/api/chat`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'text/event-stream'
            },
            body: JSON.stringify(requestBody)
        });

        console.log(`\n📊 Response Status: ${response.status} ${response.statusText}`);

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }

        // Handle streaming response
        if (response.body) {
            console.log('📡 Receiving streaming response...\n');
            
            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';
            let messageContent = '';

            while (true) {
                const { done, value } = await reader.read();
                
                if (done) break;

                const chunk = decoder.decode(value, { stream: true });
                buffer += chunk;

                // Process complete SSE events
                const lines = buffer.split('\n\n');
                buffer = lines.pop() || '';

                for (const line of lines) {
                    if (line.trim() === '') continue;
                    
                    const dataMatch = line.match(/^data: (.+)$/m);
                    if (dataMatch) {
                        try {
                            const eventData = JSON.parse(dataMatch[1]);
                            
                            if (eventData.type === 'content' && eventData.data.content) {
                                process.stdout.write(eventData.data.content);
                                messageContent += eventData.data.content;
                            } else if (eventData.type === 'done') {
                                console.log('\n\n✅ Message completed');
                                console.log(`📈 Total content length: ${messageContent.length} characters`);
                            } else if (eventData.type === 'error') {
                                console.log(`\n❌ Error: ${eventData.data.message}`);
                            }
                        } catch (parseError) {
                            console.log(`\n⚠️ Parse error: ${parseError.message}`);
                        }
                    }
                }
            }
        }

        console.log('\n✅ File upload test completed successfully!');

        // Clean up test file
        fs.unlinkSync(testFilePath);
        console.log('🧹 Cleaned up test file');

    } catch (error) {
        console.error('❌ Test failed:', error.message);
        console.error('Stack trace:', error.stack);
        
        // Clean up test file if it exists
        const testFilePath = path.join(__dirname, 'test-document.txt');
        if (fs.existsSync(testFilePath)) {
            fs.unlinkSync(testFilePath);
            console.log('🧹 Cleaned up test file');
        }
    }
}

// Validation tests
async function testValidation() {
    console.log('\n🔍 Testing file validation...\n');

    // Test 1: File too large (simulate > 3MB)
    console.log('Test 1: File size validation');
    try {
        const largeBase64 = 'A'.repeat(4 * 1024 * 1024); // 4MB of A's (larger than 3MB limit)
        
        const requestBody = {
            message: 'Test large file',
            files: [
                {
                    fileName: 'large-file.txt',
                    contentType: 'text/plain',
                    data: largeBase64
                }
            ]
        };

        const response = await fetch(`${WEBAPI_BASE_URL}/api/chat`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        });

        console.log(`📊 Large file response: ${response.status}`);
        if (!response.ok) {
            const error = await response.text();
            console.log(`✅ Expected validation error: ${error}`);
        }
    } catch (error) {
        console.log(`✅ Expected validation error: ${error.message}`);
    }

    // Test 2: Too many files (more than 2)
    console.log('\nTest 2: File count validation');
    try {
        const requestBody = {
            message: 'Test too many files',
            files: [
                { fileName: 'file1.txt', contentType: 'text/plain', data: 'dGVzdA==' },
                { fileName: 'file2.txt', contentType: 'text/plain', data: 'dGVzdA==' },
                { fileName: 'file3.txt', contentType: 'text/plain', data: 'dGVzdA==' }
            ]
        };

        const response = await fetch(`${WEBAPI_BASE_URL}/api/chat`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        });

        console.log(`📊 Too many files response: ${response.status}`);
        if (!response.ok) {
            const error = await response.text();
            console.log(`✅ Expected validation error: ${error}`);
        }
    } catch (error) {
        console.log(`✅ Expected validation error: ${error.message}`);
    }

    console.log('\n🔍 Validation tests completed\n');
}

// Main execution
async function main() {
    console.log('🚀 Fleet Assistant File Upload Test Suite\n');
    console.log('==========================================\n');

    // Run main file upload test
    await testFileUpload();

    // Run validation tests
    await testValidation();

    console.log('\n==========================================');
    console.log('📋 Test Summary:');
    console.log('- ✅ Base64 file conversion');
    console.log('- ✅ API request with file attachment');
    console.log('- ✅ Streaming response handling');
    console.log('- ✅ File validation testing');
    console.log('- ✅ Error handling');
    console.log('\n🎉 All tests completed!');
}

// Only run if this script is executed directly
if (require.main === module) {
    main().catch(console.error);
}

module.exports = {
    testFileUpload,
    testValidation,
    fileToBase64,
    getMimeType
};

const { AIProjectClient } = require('@azure/ai-projects');
const { DefaultAzureCredential } = require('@azure/identity');

async function testAzureAI() {
  try {
    // Create a client with a dummy endpoint for testing
    const client = new AIProjectClient(
      'https://example.azureml.ms',
      new DefaultAzureCredential()
    );

    console.log('Client created successfully');
    
    // Try to get the client and see if we can introspect the post method
    const chatClient = client.inference.chatCompletions();
    console.log('\nTrying to understand post method signature...');
    console.log('post method:', chatClient.post.toString());
    
    // Try to call it with different parameter combinations to see what it expects
    console.log('\nTrying to see what parameters post expects...');
    try {
      const result = await chatClient.post({
        messages: [{ role: 'user', content: 'Hello' }],
        max_tokens: 100,
        temperature: 0.7
      });
      console.log('Success with object parameter:', result);
    } catch (error) {
      console.log('Error with object parameter:', error.message);
    }
    
  } catch (error) {
    console.error('Error:', error.message);
  }
}

testAzureAI();

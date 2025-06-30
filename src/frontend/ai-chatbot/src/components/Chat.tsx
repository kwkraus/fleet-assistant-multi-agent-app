'use client';

import { useState } from 'react';
import MessageList from './MessageList';
import MessageInput from './MessageInput';

interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  createdAt?: Date;
}

interface SSEEventData {
  type: string;
  data: {
    conversationId?: string;
    messageId?: string;
    content?: string;
    message?: string;
    timestamp?: string;
    totalContent?: string;
  };
}

export default function Chat() {
  const azureFunctionsBaseUrl = process.env.NEXT_PUBLIC_AZURE_FUNCTIONS_BASE_URL || 'http://localhost:7071';
  
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [conversationId, setConversationId] = useState<string | null>(null);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setInput(e.target.value);
  };

  const clearConversation = () => {
    setMessages([]);
    setConversationId(null);
    console.log('Conversation cleared');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!input.trim() || isLoading) {
      return;
    }

    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      role: 'user',
      content: input.trim(),
      createdAt: new Date()
    };

    // Add only the user message and set loading state
    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setIsLoading(true); // This will show the "Thinking" animation

    try {
      const requestBody: {
        messages: { id: string; role: string; content: string }[];
        conversationId?: string;
      } = {
        messages: [...messages, userMessage].map(msg => ({
          id: msg.id,
          role: msg.role,
          content: msg.content
        }))
      };

      // Include conversationId if we have one
      if (conversationId) {
        requestBody.conversationId = conversationId;
      }

      const response = await fetch(`${azureFunctionsBaseUrl}/api/chat`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'text/event-stream',
        },
        body: JSON.stringify(requestBody)
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      if (!response.body) {
        throw new Error('No response body for streaming');
      }

      // Handle streaming response
      await handleStreamingResponse(response.body);

    } catch (error) {
      console.error('Chat error:', error);
      
      // Add error message as assistant response
      const errorMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: 'Sorry, I encountered an error processing your request. Please try again.',
        createdAt: new Date()
      };
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleStreamingResponse = async (body: ReadableStream<Uint8Array>) => {
    const reader = body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let assistantMessageId: string | null = null;

    try {
      while (true) {
        const { done, value } = await reader.read();
        
        if (done) {
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
              assistantMessageId = await handleSSEEvent(eventData, assistantMessageId);
            } catch (parseError) {
              console.error('Error parsing SSE event:', parseError, 'Raw data:', dataMatch[1]);
            }
          }
        }
      }
    } catch (error) {
      console.error('Error reading stream:', error);
      throw error;
    } finally {
      reader.releaseLock();
    }
  };

  const handleSSEEvent = async (eventData: SSEEventData, messageId: string | null): Promise<string | null> => {
    const { type, data } = eventData;

    switch (type) {
      case 'metadata':
        // Handle metadata (conversationId, etc.)
        if (data.conversationId && !conversationId) {
          setConversationId(data.conversationId);
          console.log('New conversation started with ID:', data.conversationId);
        }
        return messageId;

      case 'chunk':
        // Create assistant message on first chunk if it doesn't exist
        if (!messageId) {
          const newAssistantMessageId = (Date.now() + 1).toString();
          const assistantMessage: ChatMessage = {
            id: newAssistantMessageId,
            role: 'assistant',
            content: data.content || '',
            createdAt: new Date()
          };
          setMessages(prev => [...prev, assistantMessage]);
          setIsLoading(false); // Hide the "Thinking" animation
          return newAssistantMessageId;
        } else {
          // Append chunk to existing message
          setMessages(prev => 
            prev.map(msg => 
              msg.id === messageId 
                ? { ...msg, content: msg.content + (data.content || '') }
                : msg
            )
          );
          return messageId;
        }

      case 'done':
        // Streaming completed
        console.log('Streaming completed for message:', messageId);
        setIsLoading(false); // Ensure loading is false
        return messageId;

      case 'error':
        // Handle streaming error
        console.error('Streaming error:', data.message);
        if (!messageId) {
          // Create error message if no assistant message exists yet
          const errorMessageId = (Date.now() + 1).toString();
          const errorMessage: ChatMessage = {
            id: errorMessageId,
            role: 'assistant',
            content: '⚠️ An error occurred while generating the response.',
            createdAt: new Date()
          };
          setMessages(prev => [...prev, errorMessage]);
          setIsLoading(false);
          return errorMessageId;
        } else {
          // Append error to existing message
          setMessages(prev => 
            prev.map(msg => 
              msg.id === messageId 
                ? { 
                    ...msg, 
                    content: msg.content + '\n\n⚠️ An error occurred while generating the response.'
                  }
                : msg
            )
          );
          setIsLoading(false);
          return messageId;
        }

      default:
        console.warn('Unknown SSE event type:', type);
        return messageId;
    }
  };

  return (
    <div className="flex flex-col h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b px-4 py-4 shadow-sm">
        <div className="max-w-4xl mx-auto">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-xl font-semibold text-gray-900">Fleet Assistant</h1>
              <p className="text-sm text-gray-600">Your AI-powered fleet management assistant</p>
            </div>
            {conversationId && (
              <div className="flex items-center gap-2">
                <div className="text-xs text-gray-500 bg-gray-100 px-2 py-1 rounded">
                  Session: {conversationId.substring(0, 8)}...
                </div>
                <button
                  onClick={clearConversation}
                  className="text-xs text-red-600 hover:text-red-800 px-2 py-1 border border-red-300 rounded hover:bg-red-50 transition-colors"
                >
                  Clear
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Messages Container */}
      <div className="flex-1 overflow-hidden">
        <div className="h-full max-w-4xl mx-auto px-4 py-4">
          <MessageList messages={messages} isLoading={isLoading} />
        </div>
      </div>

      {/* Input Area */}
      <MessageInput
        input={input}
        isLoading={isLoading}
        onInputChange={handleInputChange}
        onSend={handleSubmit}
      />
    </div>
  );
}

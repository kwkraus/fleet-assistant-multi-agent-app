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

    // Add user message immediately
    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setIsLoading(true);

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
        },
        body: JSON.stringify(requestBody)
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const responseData = await response.json();
      
      // Handle both old format (direct message) and new format (with conversationId)
      let assistantMessage: ChatMessage;
      let newConversationId: string | null = null;

      if (responseData.message && responseData.conversationId) {
        // New format with conversationId
        assistantMessage = responseData.message;
        newConversationId = responseData.conversationId;
      } else {
        // Fallback to old format (direct message)
        assistantMessage = responseData;
      }

      // Store the conversationId for future requests
      if (newConversationId && !conversationId) {
        setConversationId(newConversationId);
        console.log('New conversation started with ID:', newConversationId);
      }
      
      // Add assistant message
      setMessages(prev => [...prev, assistantMessage]);
    } catch (error) {
      console.error('Chat error:', error);
      
      // Add error message
      const errorMessage: ChatMessage = {
        id: Date.now().toString(),
        role: 'assistant',
        content: 'Sorry, I encountered an error processing your request. Please try again.',
        createdAt: new Date()
      };
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsLoading(false);
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

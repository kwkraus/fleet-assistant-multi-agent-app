'use client';

import { FormEvent, KeyboardEvent } from 'react';

interface MessageInputProps {
  input: string;
  isLoading: boolean;
  onInputChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => void;
  onSend: (e: FormEvent<HTMLFormElement>) => void;
}

export default function MessageInput({ 
  input, 
  isLoading, 
  onInputChange, 
  onSend 
}: MessageInputProps) {
  
  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      if (input.trim() && !isLoading) {
        const formEvent = new Event('submit', { bubbles: true, cancelable: true }) as unknown as FormEvent<HTMLFormElement>;
        onSend(formEvent);
      }
    }
  };

  return (
    <form onSubmit={onSend} className="border-t bg-white p-4">
      <div className="flex space-x-4 max-w-4xl mx-auto">
        <div className="flex-1 relative">
          <textarea
            value={input}
            onChange={onInputChange}
            onKeyDown={handleKeyDown}
            placeholder="Type your message... (Press Enter to send, Shift+Enter for new line)"
            className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
            disabled={isLoading}
            rows={1}
            style={{
              minHeight: '52px',
              maxHeight: '120px',
              height: 'auto',
            }}
            onInput={(e) => {
              const target = e.target as HTMLTextAreaElement;
              target.style.height = 'auto';
              target.style.height = Math.min(target.scrollHeight, 120) + 'px';
            }}
          />
          {input.trim() && (
            <div className="absolute bottom-2 right-2 text-xs text-gray-400">
              Press Enter to send
            </div>
          )}
        </div>
        <button
          type="submit"
          disabled={isLoading || !input.trim()}
          className="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
        >
          {isLoading ? (
            <div className="flex items-center space-x-2">
              <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              <span>Sending</span>
            </div>
          ) : (
            'Send'
          )}
        </button>
      </div>
      
      {/* Quick action buttons */}
      <div className="max-w-4xl mx-auto mt-3">
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => onInputChange({ target: { value: 'What are the current maintenance schedules for my fleet?' } } as React.ChangeEvent<HTMLTextAreaElement>)}
            disabled={isLoading}
            className="px-3 py-1 text-sm bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-full transition-colors disabled:opacity-50"
          >
            Maintenance Schedule
          </button>
          <button
            type="button"
            onClick={() => onInputChange({ target: { value: 'Show me fuel efficiency reports for this month' } } as React.ChangeEvent<HTMLTextAreaElement>)}
            disabled={isLoading}
            className="px-3 py-1 text-sm bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-full transition-colors disabled:opacity-50"
          >
            Fuel Reports
          </button>
          <button
            type="button"
            onClick={() => onInputChange({ target: { value: 'Help me plan optimal routes for my deliveries' } } as React.ChangeEvent<HTMLTextAreaElement>)}
            disabled={isLoading}
            className="px-3 py-1 text-sm bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-full transition-colors disabled:opacity-50"
          >
            Route Planning
          </button>
        </div>
      </div>
    </form>
  );
}

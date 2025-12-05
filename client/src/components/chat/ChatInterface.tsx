import { useEffect, useRef } from 'react';

import { Button } from '../ui/button';
import { ChatInput } from './ChatInput';
import { MessageList } from './MessageList';
import { ScrollArea } from '../ui/scroll-area';
import { Trash2 } from 'lucide-react';
import { UsageDisplay } from './UsageDisplay';
import { useSSEChat } from '../../hooks/useSSEChat';

export const ChatInterface = () => {
  const { messages, sendMessage, isLoading, clearMessages, lastUsage, totalUsage } = useSSEChat();
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  return (
    <div className="flex h-screen bg-background">
      {/* Chat Panel */}
      <div className="flex w-full flex-col">
        {/* Header */}
        <div className="flex items-center justify-between border-b p-4">
          <div>
            <h1 className="text-xl font-bold">Generative UI Chat</h1>
            <p className="text-sm text-muted-foreground">
              Ask questions about weather, sales, or performance data
            </p>
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="icon"
              onClick={clearMessages}
              title="Clear chat"
              disabled={messages.length === 0}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Messages */}
        <ScrollArea className="flex-1 p-4">
          {messages.length === 0 ? (
            <div className="flex h-full flex-col items-center justify-center space-y-4 text-center">
              <div className="rounded-full bg-muted p-6">
                <svg
                  className="h-12 w-12 text-muted-foreground"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
                  />
                </svg>
              </div>
              <div>
                <h2 className="text-lg font-semibold">Start a conversation</h2>
                <p className="text-sm text-muted-foreground">
                  Try asking:
                </p>
                <ul className="mt-2 space-y-1 text-sm text-muted-foreground">
                  <li>"Show me the weather in New York"</li>
                  <li>"Get sales data from last month"</li>
                  <li>"Who are the top 5 salespeople?"</li>
                </ul>
              </div>
            </div>
          ) : (
            <>
              <MessageList messages={messages} sendMessage={sendMessage} />
              <div ref={messagesEndRef} />
            </>
          )}
        </ScrollArea>

        {/* Input */}
        <div className="border-t p-4">
          <ChatInput onSend={sendMessage} isLoading={isLoading} />
        </div>
        
        {/* Usage Display */}
        <UsageDisplay lastUsage={lastUsage} totalUsage={totalUsage} />
      </div>
    </div>
  );
};

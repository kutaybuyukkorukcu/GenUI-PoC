import { PanelRight, PanelRightClose, Trash2 } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

import { Button } from '../ui/button';
import { ChatInput } from './ChatInput';
import type { ChatMessage } from '../../types';
import { ComponentRenderer } from '../renderers/ComponentRenderer';
import { MessageList } from './MessageList';
import { ScrollArea } from '../ui/scroll-area';
import { useSSEChat } from '../../hooks/useSSEChat';

export const ChatInterface = () => {
  const { messages, sendMessage, isLoading, clearMessages } = useSSEChat();
  const [selectedMessage, setSelectedMessage] = useState<ChatMessage | null>(null);
  const [showPreview, setShowPreview] = useState(true);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  useEffect(() => {
    // Auto-select the latest message with a tool result
    const latestWithResult = [...messages]
      .reverse()
      .find((m) => m.toolResult && m.toolResult.success);
    
    if (latestWithResult && latestWithResult.id !== selectedMessage?.id) {
      setSelectedMessage(latestWithResult);
      setShowPreview(true);
    }
  }, [messages, selectedMessage?.id]);

  return (
    <div className="flex h-screen bg-background">
      {/* Chat Panel */}
      <div className={`flex flex-col transition-all duration-300 ${showPreview ? 'w-1/2' : 'w-full'}`}>
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
              onClick={() => setShowPreview(!showPreview)}
              title={showPreview ? 'Hide preview' : 'Show preview'}
            >
              {showPreview ? (
                <PanelRightClose className="h-4 w-4" />
              ) : (
                <PanelRight className="h-4 w-4" />
              )}
            </Button>
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
              <MessageList
                messages={messages}
                onSelectMessage={setSelectedMessage}
                selectedMessageId={selectedMessage?.id}
              />
              <div ref={messagesEndRef} />
            </>
          )}
        </ScrollArea>

        {/* Input */}
        <div className="border-t p-4">
          <ChatInput onSend={sendMessage} isLoading={isLoading} />
        </div>
      </div>

      {/* Preview Panel */}
      {showPreview && (
        <div className="w-1/2 border-l bg-muted/30">
          <div className="flex h-full flex-col">
            <div className="border-b bg-background p-4">
              <h2 className="text-lg font-semibold">Preview</h2>
              <p className="text-sm text-muted-foreground">
                {selectedMessage
                  ? 'Viewing generated component'
                  : 'Component preview will appear here'}
              </p>
            </div>

            <ScrollArea className="flex-1 p-6">
              {selectedMessage?.toolResult ? (
                <div className="space-y-4">
                  <ComponentRenderer toolResult={selectedMessage.toolResult} />
                </div>
              ) : (
                <div className="flex h-full items-center justify-center">
                  <div className="text-center text-muted-foreground">
                    <p>No component to display</p>
                    <p className="mt-2 text-sm">
                      Ask a question to see generated UI components
                    </p>
                  </div>
                </div>
              )}
            </ScrollArea>
          </div>
        </div>
      )}
    </div>
  );
};

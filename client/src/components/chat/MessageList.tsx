import { Bot, Loader2, User } from 'lucide-react';

import type { ChatMessage } from '../../types';
import { GenerativeUIRenderer } from '../renderers/GenerativeUIRenderer';
import { cn } from '../../lib/utils';

interface MessageListProps {
  messages: ChatMessage[];
  onSelectMessage?: (message: ChatMessage) => void;
  selectedMessageId?: string;
}

export const MessageList = ({ messages, onSelectMessage, selectedMessageId }: MessageListProps) => {
  return (
    <div className="flex flex-col space-y-4">
      {messages.map((message) => (
        <div
          key={message.id}
          className={cn(
            'group flex gap-3 rounded-lg p-4 transition-colors',
            message.role === 'user' ? 'bg-muted/50' : 'bg-card',
            onSelectMessage && message.toolResult && 'cursor-pointer hover:bg-accent',
            selectedMessageId === message.id && 'ring-2 ring-primary'
          )}
          onClick={() => onSelectMessage && message.toolResult && onSelectMessage(message)}
        >
          <div className="flex-shrink-0">
            {message.role === 'user' ? (
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary text-primary-foreground">
                <User className="h-4 w-4" />
              </div>
            ) : (
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-secondary text-secondary-foreground">
                <Bot className="h-4 w-4" />
              </div>
            )}
          </div>

          <div className="flex-1 space-y-2">
            <div className="flex items-center gap-2">
              <span className="text-sm font-semibold">
                {message.role === 'user' ? 'You' : 'Assistant'}
              </span>
              <span className="text-xs text-muted-foreground">
                {message.timestamp.toLocaleTimeString()}
              </span>
              {message.isStreaming && (
                <Loader2 className="h-3 w-3 animate-spin text-muted-foreground" />
              )}
            </div>

            {/* Render Generative UI DSL format */}
            {message.isGenerativeUI && message.generativeUIResponse ? (
              <GenerativeUIRenderer 
                response={message.generativeUIResponse} 
                isStreaming={message.isStreaming}
              />
            ) : (
              <>
                {/* Render legacy format */}
                <div className="text-sm leading-relaxed">{message.content}</div>

                {message.analysis && (
                  <div className="mt-2 rounded-md border border-dashed p-3 text-xs">
                    <p className="font-semibold">Analysis:</p>
                    <p className="text-muted-foreground">{message.analysis.reasoning}</p>
                  </div>
                )}

                {message.toolResult && onSelectMessage && (
                  <div className="mt-2 text-xs text-muted-foreground">
                    Click to view results →
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      ))}
    </div>
  );
};

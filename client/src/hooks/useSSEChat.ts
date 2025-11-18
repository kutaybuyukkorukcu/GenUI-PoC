import { AgentAnalysis, ChatMessage, ToolCallResult } from '../types';
import { useCallback, useRef, useState } from 'react';

import { chatWithAgentSSE } from '../services/api';

export const useSSEChat = () => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const abortControllerRef = useRef<AbortController | null>(null);

  const sendMessage = useCallback(async (content: string) => {
    // Add user message
    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      role: 'user',
      content,
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setIsLoading(true);

    // Create assistant message placeholder
    const assistantMessageId = (Date.now() + 1).toString();
    const assistantMessage: ChatMessage = {
      id: assistantMessageId,
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      isStreaming: true,
    };

    setMessages((prev) => [...prev, assistantMessage]);

    await chatWithAgentSSE(
      content,
      (data) => {
        setMessages((prev) => {
          const updated = [...prev];
          const msgIndex = updated.findIndex((m) => m.id === assistantMessageId);
          
          if (msgIndex !== -1) {
            const msg = updated[msgIndex];
            
            // Handle different event types
            if (data.eventType === 'message') {
              msg.content = data.content || '';
            } else if (data.eventType === 'analysis') {
              // This is an analysis event
              msg.analysis = data as AgentAnalysis;
              msg.content = `Understanding: ${data.Intent || data.intent}`;
            } else if (data.eventType === 'tool-result') {
              // This is a tool result
              msg.toolResult = data as ToolCallResult;
              if (data.Success || data.success) {
                msg.content = 'Here are the results:';
              } else {
                msg.content = `Error: ${data.Error || data.error}`;
              }
            }
            
            updated[msgIndex] = msg;
          }
          
          return updated;
        });
      },
      (error) => {
        setMessages((prev) => {
          const updated = [...prev];
          const msgIndex = updated.findIndex((m) => m.id === assistantMessageId);
          
          if (msgIndex !== -1) {
            updated[msgIndex] = {
              ...updated[msgIndex],
              content: `Error: ${error}`,
              isStreaming: false,
            };
          }
          
          return updated;
        });
        setIsLoading(false);
      },
      () => {
        setMessages((prev) => {
          const updated = [...prev];
          const msgIndex = updated.findIndex((m) => m.id === assistantMessageId);
          
          if (msgIndex !== -1) {
            updated[msgIndex] = {
              ...updated[msgIndex],
              isStreaming: false,
            };
          }
          
          return updated;
        });
        setIsLoading(false);
      }
    );
  }, []);

  const clearMessages = useCallback(() => {
    setMessages([]);
  }, []);

  return {
    messages,
    sendMessage,
    isLoading,
    clearMessages,
  };
};

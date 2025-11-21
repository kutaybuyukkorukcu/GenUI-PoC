import { AgentAnalysis, ChatMessage, GenerativeUIResponse, ToolCallResult } from '../types';
import { useCallback, useRef, useState } from 'react';

import { chatWithAgentSSE } from '../services/api';

// Feature flag to use Generative UI DSL
const USE_GENERATIVE_UI = import.meta.env.VITE_USE_GENERATIVE_UI_DSL === 'true';

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
            const msg = { ...updated[msgIndex] };
            
            // 🚀 NEW: Handle Generative UI DSL format
            if (data.eventType === 'generative-ui' && USE_GENERATIVE_UI) {
              try {
                // Parse the JSON DSL response
                const jsonResponse = JSON.parse(data.response);
                msg.generativeUIResponse = jsonResponse as GenerativeUIResponse;
                msg.isGenerativeUI = true;
                msg.isStreaming = true;
                msg.content = ''; // Clear content, we'll use generativeUIResponse instead
              } catch (error) {
                console.error('Failed to parse generative UI response:', error);
                msg.content = 'Error parsing response';
              }
            }
            // EXISTING: Handle legacy event types
            else if (data.eventType === 'message') {
              // Append streaming text content
              msg.content = (msg.content || '') + (data.content || '');
            } else if (data.eventType === 'analysis') {
              // This is an analysis event
              msg.analysis = data as AgentAnalysis;
              msg.content = `Understanding: ${data.Intent || data.intent}`;
            } else if (data.eventType === 'tool-result') {
              // This is a tool result - store it but don't override content
              msg.toolResult = data as ToolCallResult;
              // Don't set content here - let the LLM's streaming response provide the natural language
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

import { ChatMessage, GenerativeUIResponse, UsageInfo } from '../types';
import { useCallback, useState } from 'react';

import { chatWithAgentSSE } from '../services/api';

export const useSSEChat = () => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [lastUsage, setLastUsage] = useState<UsageInfo | null>(null);
  const [totalUsage, setTotalUsage] = useState<UsageInfo>({
    promptTokens: 0,
    completionTokens: 0,
    totalTokens: 0,
    estimatedCost: {
      promptCost: 0,
      completionCost: 0,
      totalCost: 0,
      currency: 'USD',
      model: ''
    }
  });

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
            
            // Handle Generative UI DSL format
            if (data.eventType === 'generative-ui') {
              try {
                // Parse the JSON DSL response
                const jsonResponse = JSON.parse(data.response);
                msg.generativeUIResponse = jsonResponse as GenerativeUIResponse;
                msg.isStreaming = true;
                msg.content = ''; // Clear content, we'll use generativeUIResponse instead
              } catch (error) {
                console.error('Failed to parse generative UI response:', error);
                msg.content = 'Error parsing response';
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
      },
      // Usage callback
      (usage) => {
        setLastUsage(usage);
        setTotalUsage((prev) => ({
          promptTokens: prev.promptTokens + usage.promptTokens,
          completionTokens: prev.completionTokens + usage.completionTokens,
          totalTokens: prev.totalTokens + usage.totalTokens,
          estimatedCost: {
            promptCost: (prev.estimatedCost?.promptCost ?? 0) + (usage.estimatedCost?.promptCost ?? 0),
            completionCost: (prev.estimatedCost?.completionCost ?? 0) + (usage.estimatedCost?.completionCost ?? 0),
            totalCost: (prev.estimatedCost?.totalCost ?? 0) + (usage.estimatedCost?.totalCost ?? 0),
            currency: usage.estimatedCost?.currency ?? 'USD',
            model: usage.estimatedCost?.model ?? ''
          }
        }));
      }
    );
  }, []);

  const clearMessages = useCallback(() => {
    setMessages([]);
    setLastUsage(null);
    setTotalUsage({
      promptTokens: 0,
      completionTokens: 0,
      totalTokens: 0,
      estimatedCost: {
        promptCost: 0,
        completionCost: 0,
        totalCost: 0,
        currency: 'USD',
        model: ''
      }
    });
  }, []);

  return {
    messages,
    sendMessage,
    isLoading,
    clearMessages,
    lastUsage,
    totalUsage,
  };
};

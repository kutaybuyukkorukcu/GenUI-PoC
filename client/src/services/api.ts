// In Docker: empty string (nginx proxies /v1/ to backend)
// In local dev: http://localhost:5176
const API_BASE_URL = import.meta.env.VITE_API_URL || '';

// Conversation history for context (in-memory, resets on page refresh)
let conversationHistory: Array<{ role: string; content: string }> = [];

// Callback types for SSE events
interface SSECallbacks {
  onMessage: (data: { response: string; eventType: string }) => void;
  onUsage?: (usage: {
    promptTokens: number;
    completionTokens: number;
    totalTokens: number;
    estimatedCost?: {
      promptCost: number;
      completionCost: number;
      totalCost: number;
      currency: string;
      model: string;
    };
  }) => void;
  onError: (error: string) => void;
  onComplete: () => void;
}

// SSE-based chat with support for generative UI responses
// Now uses OpenAI-compatible /v1/chat/completions endpoint
export const chatWithAgentSSE = async (
  message: string,
  onMessage: SSECallbacks['onMessage'],
  onError: SSECallbacks['onError'],
  onComplete: SSECallbacks['onComplete'],
  onUsage?: SSECallbacks['onUsage']
) => {
  try {
    // Add user message to history
    conversationHistory.push({ role: 'user', content: message });

    const response = await fetch(`${API_BASE_URL}/v1/chat/completions`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        // LLM API key - for demo, we'll let backend use .env
        // In production, this would come from user auth
      },
      body: JSON.stringify({
        model: 'gpt-4o-mini',
        messages: conversationHistory,
        stream: true,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.error?.message || `HTTP error! status: ${response.status}`);
    }

    const reader = response.body?.getReader();
    const decoder = new TextDecoder();

    if (!reader) {
      throw new Error('No response body');
    }

    let buffer = '';
    let currentEvent = '';
    let fullContent = '';

    while (true) {
      const { done, value } = await reader.read();
      
      if (done) {
        // Add assistant response to history
        if (fullContent) {
          conversationHistory.push({ role: 'assistant', content: fullContent });
        }
        onComplete();
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() || '';

      for (const line of lines) {
        if (line.startsWith('event:')) {
          currentEvent = line.substring(6).trim();
        } else if (line.startsWith('data:')) {
          const data = line.substring(5).trim();
          
          // Handle [DONE] marker
          if (data === '[DONE]') {
            continue;
          }
          
          if (data) {
            try {
              const parsed = JSON.parse(data);
              
              // Handle genui event (parsed UI response)
              if (currentEvent === 'genui') {
                onMessage({ 
                  response: JSON.stringify(parsed), 
                  eventType: 'generative-ui' 
                });
              }
              // Handle usage event (token counts and cost)
              else if (currentEvent === 'usage') {
                if (onUsage) {
                  onUsage(parsed);
                }
              }
              // Handle streaming chunks
              else if (parsed.choices?.[0]?.delta?.content) {
                fullContent += parsed.choices[0].delta.content;
              }
              // Handle non-streaming response
              else if (parsed.genui) {
                onMessage({ 
                  response: JSON.stringify(parsed.genui), 
                  eventType: 'generative-ui' 
                });
              }
            } catch (e) {
              console.error('Failed to parse SSE data:', e, 'Line:', line);
            }
          }
        }
      }
    }
  } catch (error) {
    onError(error instanceof Error ? error.message : 'Unknown error');
  }
};

// Reset conversation history
export const resetConversation = () => {
  conversationHistory = [];
};

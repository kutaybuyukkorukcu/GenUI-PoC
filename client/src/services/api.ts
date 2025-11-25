const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

// SSE-based chat with support for generative UI responses
export const chatWithAgentSSE = async (
  message: string,
  onMessage: (data: any) => void,
  onError: (error: string) => void,
  onComplete: () => void
) => {
  try {
    const response = await fetch(`${API_BASE_URL}/api/agent/chat`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ message }),
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const reader = response.body?.getReader();
    const decoder = new TextDecoder();

    if (!reader) {
      throw new Error('No response body');
    }

    let buffer = '';
    let currentEvent = '';

    while (true) {
      const { done, value } = await reader.read();
      
      if (done) {
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
          if (data) {
            try {
              const parsed = JSON.parse(data);
              // Add event type to the parsed data
              onMessage({ ...parsed, eventType: currentEvent });
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

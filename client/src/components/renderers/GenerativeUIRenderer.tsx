import type { ContentBlock, GenerativeUIResponse } from '../../types';

import { DynamicComponent } from './ComponentRegistry';
import { ThinkingList } from './ThinkingIndicator';

interface GenerativeUIRendererProps {
  response: GenerativeUIResponse;
  isStreaming?: boolean;
}

/**
 * GenerativeUIRenderer - Main component for rendering JSON DSL responses
 * 
 * This component parses the GenerativeUIResponse structure and renders:
 * 1. Thinking states (AI reasoning process)
 * 2. Content blocks (text and components mixed together)
 * 3. Metadata (optional, for debugging)
 * 
 * Supports progressive streaming where partial responses are rendered
 * and updated as more data arrives.
 */
export const GenerativeUIRenderer = ({ 
  response, 
  isStreaming = false 
}: GenerativeUIRendererProps) => {
  
  // Handle empty or invalid response
  if (!response) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">
        Invalid response format
      </div>
    );
  }

  // Handle error responses
  if (response.metadata?.error) {
    return (
      <div className="space-y-3">
        {response.thinking && response.thinking.length > 0 && (
          <ThinkingList thinking={response.thinking} />
        )}
        <div className="rounded-lg border border-red-200 bg-red-50 p-4">
          <p className="font-semibold text-red-800">Error</p>
          {response.content && response.content.length > 0 && (
            <p className="mt-1 text-sm text-red-700">
              {response.content.find(block => block.type === 'text')?.value || 'An error occurred'}
            </p>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {/* Render thinking states */}
      {response.thinking && response.thinking.length > 0 && (
        <ThinkingList thinking={response.thinking} />
      )}

      {/* Render content blocks */}
      {response.content && response.content.length > 0 && (
        <div className="space-y-3">
          {response.content.map((block, index) => (
            <ContentBlockRenderer key={index} block={block} />
          ))}
        </div>
      )}

      {/* Show streaming indicator */}
      {isStreaming && (
        <div className="text-xs text-muted-foreground">
          <span className="inline-block h-2 w-2 animate-pulse rounded-full bg-blue-500"></span>
          {' '}Generating...
        </div>
      )}

      {/* Optional: Show metadata in development */}
      {import.meta.env.DEV && response.metadata && (
        <details className="mt-4 text-xs text-muted-foreground">
          <summary className="cursor-pointer hover:text-foreground">
            Metadata
          </summary>
          <pre className="mt-2 overflow-auto rounded bg-muted p-2">
            {JSON.stringify(response.metadata, null, 2)}
          </pre>
        </details>
      )}
    </div>
  );
};

/**
 * ContentBlockRenderer - Renders individual content blocks (text or component)
 */
const ContentBlockRenderer = ({ block }: { block: ContentBlock }) => {
  if (block.type === 'text') {
    return (
      <div className="prose prose-sm max-w-none">
        <p className="text-foreground">{block.value}</p>
      </div>
    );
  }

  if (block.type === 'component') {
    return <DynamicComponent block={block} />;
  }

  // Unknown block type
  console.warn('Unknown block type:', block);
  return null;
};

import { Check, Loader2 } from 'lucide-react';

import type { ThinkingItem } from '../../types';

interface ThinkingIndicatorProps {
  item: ThinkingItem;
}

/**
 * ThinkingIndicator - Shows the AI's reasoning process
 * Displays a thinking item with animated loader or checkmark
 */
export const ThinkingIndicator = ({ item }: ThinkingIndicatorProps) => {
  const isActive = item.status === 'active';
  const isComplete = item.status === 'complete';

  return (
    <div className="flex items-center gap-2 text-sm text-muted-foreground">
      {isActive && (
        <Loader2 className="h-4 w-4 animate-spin text-blue-500" />
      )}
      {isComplete && (
        <Check className="h-4 w-4 text-green-500" />
      )}
      <span className={isActive ? 'text-blue-600' : 'text-muted-foreground'}>
        {item.message}
      </span>
    </div>
  );
};

interface ThinkingListProps {
  thinking: ThinkingItem[];
}

/**
 * ThinkingList - Displays multiple thinking items
 */
export const ThinkingList = ({ thinking }: ThinkingListProps) => {
  if (!thinking || thinking.length === 0) {
    return null;
  }

  return (
    <div className="mb-4 space-y-1 rounded-lg border bg-muted/30 p-3">
      <div className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
        Thinking
      </div>
      {thinking.map((item, index) => (
        <ThinkingIndicator key={index} item={item} />
      ))}
    </div>
  );
};

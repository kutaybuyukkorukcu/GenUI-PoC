import { UsageInfo } from '../../types';
import { Coins, Zap } from 'lucide-react';

interface UsageDisplayProps {
  lastUsage: UsageInfo | null;
  totalUsage: UsageInfo | null;
}

export const UsageDisplay = ({ lastUsage, totalUsage }: UsageDisplayProps) => {
  const formatCost = (cost?: number) => {
    if (cost == null) return '$0.00';
    if (cost < 0.01) {
      return `$${cost.toFixed(6)}`;
    }
    return `$${cost.toFixed(4)}`;
  };

  const formatTokens = (tokens?: number) => {
    if (tokens == null) return '0';
    if (tokens >= 1000) {
      return `${(tokens / 1000).toFixed(1)}k`;
    }
    return tokens.toString();
  };

  // Don't render if no usage data
  if (!lastUsage && (!totalUsage || totalUsage.totalTokens === 0)) {
    return null;
  }

  return (
    <div className="flex items-center gap-4 px-4 py-2 bg-muted/30 border-t text-xs text-muted-foreground">
      {/* Last request */}
      {lastUsage && (
        <div className="flex items-center gap-2">
          <Zap className="h-3 w-3" />
          <span>Last: {formatTokens(lastUsage.totalTokens)} tokens</span>
          {lastUsage.estimatedCost && (
            <span className="text-amber-600 dark:text-amber-400">
              ({formatCost(lastUsage.estimatedCost.totalCost)})
            </span>
          )}
        </div>
      )}
      
      {/* Separator */}
      {lastUsage && totalUsage && totalUsage.totalTokens > 0 && (
        <span className="text-muted-foreground/50">|</span>
      )}
      
      {/* Session total */}
      {totalUsage && totalUsage.totalTokens > 0 && (
        <div className="flex items-center gap-2">
          <Coins className="h-3 w-3" />
          <span>Session: {formatTokens(totalUsage.totalTokens)} tokens</span>
          {totalUsage.estimatedCost && (
            <span className="text-amber-600 dark:text-amber-400 font-medium">
              ({formatCost(totalUsage.estimatedCost.totalCost)})
            </span>
          )}
        </div>
      )}
      
      {/* Model info */}
      {lastUsage?.estimatedCost?.model && (
        <>
          <span className="text-muted-foreground/50">|</span>
          <span className="text-muted-foreground/70">
            {lastUsage.estimatedCost.model}
          </span>
        </>
      )}
    </div>
  );
};

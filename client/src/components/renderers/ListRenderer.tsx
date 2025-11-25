import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';

interface ListRendererProps {
  title?: string;
  items: unknown[];
  layout?: 'list' | 'grid' | 'compact';
  onItemClick?: (item: unknown) => void;
}

/**
 * Generic List Renderer - displays collections of items
 * Works domain-agnostic: can render products, users, transactions, etc.
 * Supports multiple layouts: list, grid, compact
 */
export const ListRenderer = ({ 
  title, 
  items, 
  layout = 'grid',
  onItemClick 
}: ListRendererProps) => {
  
  if (!items || items.length === 0) {
    return (
      <Card>
        <CardContent className="pt-6">
          <p className="text-sm text-muted-foreground">No items to display</p>
        </CardContent>
      </Card>
    );
  }

  const renderValue = (value: unknown): string => {
    if (value === null || value === undefined) return 'N/A';
    if (typeof value === 'object') return JSON.stringify(value, null, 2);
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    if (typeof value === 'number') return value.toLocaleString();
    if (value instanceof Date) return value.toLocaleDateString();
    return String(value);
  };

  const renderListItem = (item: unknown, index: number) => {
    // If item is a simple value (string, number), render it directly
    if (typeof item !== 'object' || item === null) {
      return (
        <div
          key={index}
          className="rounded-lg border bg-card p-3 text-sm hover:bg-accent"
          onClick={() => onItemClick?.(item)}
        >
          {renderValue(item)}
        </div>
      );
    }

    // If item is an object, render its fields
    const entries = Object.entries(item as Record<string, unknown>);
    const firstEntry = entries[0];
    const title = firstEntry ? renderValue(firstEntry[1]) : `Item ${index + 1}`;

    return (
      <Card
        key={index}
        className={onItemClick ? 'cursor-pointer hover:shadow-md transition-shadow' : ''}
        onClick={() => onItemClick?.(item)}
      >
        <CardHeader className="pb-3">
          <CardTitle className="text-base">{title}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          {entries.slice(1).map(([key, value]) => (
            <div key={key} className="flex justify-between text-sm">
              <span className="text-muted-foreground">
                {key.replace(/([A-Z])/g, ' $1').trim()}:
              </span>
              <span className="font-medium">{renderValue(value)}</span>
            </div>
          ))}
        </CardContent>
      </Card>
    );
  };

  const renderCompactItem = (item: unknown, index: number) => {
    if (typeof item !== 'object' || item === null) {
      return (
        <div
          key={index}
          className="flex items-center justify-between rounded border bg-card px-3 py-2 text-sm hover:bg-accent"
          onClick={() => onItemClick?.(item)}
        >
          <span>{renderValue(item)}</span>
        </div>
      );
    }

    const entries = Object.entries(item as Record<string, unknown>);
    const [, firstValue] = entries[0] || ['Item', `#${index + 1}`];
    const [secondKey, secondValue] = entries[1] || [null, null];

    return (
      <div
        key={index}
        className={`flex items-center justify-between rounded border bg-card px-3 py-2 text-sm ${
          onItemClick ? 'cursor-pointer hover:bg-accent' : ''
        }`}
        onClick={() => onItemClick?.(item)}
      >
        <div className="flex flex-col">
          <span className="font-medium">{renderValue(firstValue)}</span>
          {secondValue !== null && secondValue !== undefined && (
            <span className="text-xs text-muted-foreground">
              {String(secondKey)}: {renderValue(secondValue)}
            </span>
          )}
        </div>
        {entries.length > 2 && (
          <span className="text-xs text-muted-foreground">
            +{entries.length - 2} more
          </span>
        )}
      </div>
    );
  };

  const layoutClasses = {
    list: 'space-y-2',
    grid: 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4',
    compact: 'space-y-1',
  };

  return (
    <div className="space-y-3">
      {title && (
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-semibold">{title}</h3>
          <span className="text-sm text-muted-foreground">
            {items.length} {items.length === 1 ? 'item' : 'items'}
          </span>
        </div>
      )}
      <div className={layoutClasses[layout]}>
        {items.map((item, index) => 
          layout === 'compact' 
            ? renderCompactItem(item, index)
            : renderListItem(item, index)
        )}
      </div>
    </div>
  );
};

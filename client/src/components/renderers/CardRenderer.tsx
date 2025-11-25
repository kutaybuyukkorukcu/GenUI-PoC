import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';

interface CardRendererProps {
  title?: string;
  description?: string;
  data: Record<string, unknown> | unknown;
  onClick?: () => void;
}

/**
 * Generic Card Renderer - displays any single object in a card format
 * Works domain-agnostic: can render weather data, user profiles, product details, etc.
 */
export const CardRenderer = ({ 
  title, 
  description, 
  data,
  onClick 
}: CardRendererProps) => {
  
  const renderValue = (value: unknown): string => {
    if (value === null || value === undefined) return 'N/A';
    if (typeof value === 'object') return JSON.stringify(value);
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    if (typeof value === 'number') return value.toLocaleString();
    if (value instanceof Date) return value.toLocaleDateString();
    return String(value);
  };

  const renderDataFields = () => {
    if (typeof data !== 'object' || data === null) {
      return <p className="text-sm text-muted-foreground">{renderValue(data)}</p>;
    }

    const entries = Object.entries(data as Record<string, unknown>);
    
    return (
      <div className="grid gap-3">
        {entries.map(([key, value]) => (
          <div key={key} className="flex flex-col space-y-1">
            <span className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              {key.replace(/([A-Z])/g, ' $1').trim()}
            </span>
            <span className="text-sm font-semibold text-foreground">
              {renderValue(value)}
            </span>
          </div>
        ))}
      </div>
    );
  };

  return (
    <Card 
      className={onClick ? 'cursor-pointer hover:shadow-lg transition-shadow' : ''}
      onClick={onClick}
    >
      {(title || description) && (
        <CardHeader>
          {title && <CardTitle>{title}</CardTitle>}
          {description && <CardDescription>{description}</CardDescription>}
        </CardHeader>
      )}
      <CardContent>
        {renderDataFields()}
      </CardContent>
    </Card>
  );
};

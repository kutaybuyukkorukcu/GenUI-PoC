import { Card, CardContent } from '../ui/card';

import { AlertCircle } from 'lucide-react';
import { ChartRenderer } from './ChartRenderer';
import { TableRenderer } from './TableRenderer';
import type { ToolCallResult } from '../../types';
import { WeatherCard } from './WeatherCard';

interface ComponentRendererProps {
  toolResult: ToolCallResult;
}

export const ComponentRenderer = ({ toolResult }: ComponentRendererProps) => {
  const success = toolResult.Success ?? toolResult.success;
  const error = toolResult.Error ?? toolResult.error;
  const data = toolResult.Data ?? toolResult.data;
  const componentType = toolResult.ComponentType ?? toolResult.componentType;

  if (!success) {
    return (
      <Card className="w-full border-destructive">
        <CardContent className="p-6">
          <div className="flex items-center gap-3 text-destructive">
            <AlertCircle className="h-5 w-5" />
            <div>
              <p className="font-semibold">Error</p>
              <p className="text-sm">{error}</p>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  switch (componentType) {
    case 'weather':
      return <WeatherCard data={data} />;
    case 'chart':
      return <ChartRenderer data={data} />;
    case 'table':
      return <TableRenderer data={data} />;
    default:
      return (
        <Card className="w-full">
          <CardContent className="p-6">
            <pre className="overflow-auto rounded bg-muted p-4 text-sm">
              {JSON.stringify(data, null, 2)}
            </pre>
          </CardContent>
        </Card>
      );
  }
};

import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';

import { TrendingUp } from 'lucide-react';

interface ChartRendererProps {
  chartData: Record<string, unknown>[];
  title?: string;
}

export const ChartRenderer = ({ chartData, title }: ChartRendererProps) => {
  if (!chartData || chartData.length === 0) {
    return (
      <Card className="w-full">
        <CardContent className="p-6">
          <p className="text-muted-foreground">No chart data available</p>
        </CardContent>
      </Card>
    );
  }

  // Determine what to plot based on the data structure
  const firstItem = chartData[0];
  const keys = Object.keys(firstItem);
  const xAxisKey = keys[0]; // First key is typically the label/category
  const yAxisKeys = keys.filter(k => typeof firstItem[k] === 'number'); // Numeric keys

  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <TrendingUp className="h-5 w-5" />
          {title || 'Chart'}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={400}>
          <BarChart 
            data={chartData} 
            margin={{ top: 20, right: 30, left: 20, bottom: 5 }}
          >
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey={xAxisKey} 
              angle={-45}
              textAnchor="end"
              height={100}
              interval={0}
            />
            <YAxis />
            <Tooltip 
              formatter={(value: number) => typeof value === 'number' ? `$${value.toLocaleString()}` : value}
              contentStyle={{ borderRadius: '8px' }}
            />
            <Legend />
            {yAxisKeys.map((key, index) => (
              <Bar 
                key={key}
                dataKey={key} 
                fill={`hsl(var(--chart-${(index % 5) + 1}))`}
                name={key.replaceAll(/([A-Z])/g, ' $1').trim()} // Convert camelCase to Title Case
                radius={[8, 8, 0, 0]}
              />
            ))}
          </BarChart>
        </ResponsiveContainer>

        <div className="mt-6 space-y-2">
          <h3 className="text-sm font-semibold">Breakdown</h3>
          {chartData.map((item, index) => (
            <div key={item[xAxisKey] as string || index} className="flex items-center justify-between rounded-lg border p-3">
              <div>
                <p className="font-medium">{item[xAxisKey] as string}</p>
              </div>
              <div className="text-right space-y-1">
                {yAxisKeys.map(key => (
                  <p key={key} className="text-sm">
                    <span className="text-muted-foreground">{key}: </span>
                    <span className="font-semibold">
                      {typeof item[key] === 'number' ? `$${(item[key] as number).toLocaleString()}` : String(item[key])}
                    </span>
                  </p>
                ))}
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
};

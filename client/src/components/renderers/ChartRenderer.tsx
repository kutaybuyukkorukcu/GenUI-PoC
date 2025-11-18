import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';

import type { SalesPersonPerformance } from '../../types';
import { TrendingUp } from 'lucide-react';

interface ChartRendererProps {
  data: SalesPersonPerformance[];
}

export const ChartRenderer = ({ data }: ChartRendererProps) => {
  if (!data || data.length === 0) {
    return (
      <Card className="w-full">
        <CardContent className="p-6">
          <p className="text-muted-foreground">No chart data available</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <TrendingUp className="h-5 w-5" />
          Top Sales Performance
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={400}>
          <BarChart 
            data={data.map(item => ({
              // Handle both PascalCase and camelCase
              salesPersonName: item.SalesPersonName || item.salesPersonName,
              totalSales: item.TotalSales ?? item.totalSales,
              salesCount: item.SalesCount ?? item.salesCount,
              region: item.Region || item.region
            }))} 
            margin={{ top: 20, right: 30, left: 20, bottom: 5 }}
          >
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis 
              dataKey="salesPersonName" 
              angle={-45}
              textAnchor="end"
              height={100}
              interval={0}
            />
            <YAxis />
            <Tooltip 
              formatter={(value: number) => `$${value.toLocaleString()}`}
              contentStyle={{ borderRadius: '8px' }}
            />
            <Legend />
            <Bar 
              dataKey="totalSales" 
              fill="hsl(var(--chart-1))" 
              name="Total Sales"
              radius={[8, 8, 0, 0]}
            />
          </BarChart>
        </ResponsiveContainer>

        <div className="mt-6 space-y-2">
          <h3 className="text-sm font-semibold">Breakdown</h3>
          {data.map((person, index) => {
            const salesPersonName = person.SalesPersonName || person.salesPersonName;
            const region = person.Region || person.region;
            const totalSales = person.TotalSales ?? person.totalSales ?? 0;
            const salesCount = person.SalesCount ?? person.salesCount ?? 0;

            return (
              <div key={index} className="flex items-center justify-between rounded-lg border p-3">
                <div>
                  <p className="font-medium">{salesPersonName}</p>
                  <p className="text-sm text-muted-foreground">{region}</p>
                </div>
                <div className="text-right">
                  <p className="font-semibold">${Number(totalSales).toLocaleString()}</p>
                  <p className="text-sm text-muted-foreground">{salesCount} sales</p>
                </div>
              </div>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
};

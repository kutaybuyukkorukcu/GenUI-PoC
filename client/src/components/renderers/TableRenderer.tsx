import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';

import { Table2 } from 'lucide-react';

interface TableRendererProps {
  columns?: unknown[];
  rows?: Record<string, unknown>[];
  title?: string;
}

// Helper to safely convert column to string
const getColumnName = (col: unknown): string => {
  if (typeof col === 'string') return col;
  if (typeof col === 'object' && col !== null) {
    // Handle {name: "..."} or {header: "..."} or {label: "..."} patterns
    const obj = col as Record<string, unknown>;
    return String(obj.name ?? obj.header ?? obj.label ?? obj.key ?? JSON.stringify(col));
  }
  return String(col);
};

export const TableRenderer = ({ columns, rows, title }: TableRendererProps) => {
  // Normalize columns to strings
  const normalizedColumns = columns?.map(getColumnName) ?? [];
  
  // If no columns provided, derive from first row
  const effectiveColumns = normalizedColumns.length > 0 
    ? normalizedColumns 
    : (rows?.[0] ? Object.keys(rows[0]) : []);

  if (!rows || rows.length === 0) {
    return (
      <Card className="w-full">
        <CardContent className="p-6">
          <p className="text-muted-foreground">No table data available</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Table2 className="h-5 w-5" />
          {title ?? 'Data Table'}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b">
                {effectiveColumns.map((col, idx) => (
                  <th key={`${col}-${idx}`} className="p-3 text-left text-sm font-semibold">
                    {col}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, index) => {
                // Use a combination of fields for key, fallback to index
                const rowKey = row.product || row.Product || row.id || row.Id || index;
                return (
                  <tr key={`${rowKey}-${index}`} className="border-b hover:bg-muted/50">
                    {effectiveColumns.map((col, colIdx) => {
                      // Map column names to row keys (case-insensitive)
                      const colLower = col.toLowerCase();
                      const value = row[colLower] ?? row[col] ?? '';
                      
                      // Format amount as currency
                      const isAmount = colLower.includes('amount') || colLower.includes('price') || colLower.includes('cost');
                      const displayValue = isAmount && typeof value === 'number' 
                        ? `$${value.toLocaleString()}` 
                        : typeof value === 'object' 
                          ? JSON.stringify(value)
                          : String(value ?? '');

                      return (
                        <td 
                          key={`${col}-${colIdx}`} 
                          className={`p-3 text-sm ${isAmount ? 'text-right font-semibold' : ''}`}
                        >
                          {displayValue}
                        </td>
                      );
                    })}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {/* Calculate total if there's an amount column */}
        {effectiveColumns.some(col => col.toLowerCase().includes('amount')) && (
          <div className="mt-4 flex items-center justify-between rounded-lg border p-3 bg-muted/30">
            <span className="text-sm font-medium">Total</span>
            <span className="text-lg font-bold">
              ${rows.reduce((sum, row) => {
                const amount = row.amount ?? row.Amount ?? 0;
                return sum + Number(amount);
              }, 0).toLocaleString()}
            </span>
          </div>
        )}
      </CardContent>
    </Card>
  );
};

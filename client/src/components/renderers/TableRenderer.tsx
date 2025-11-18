import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';

import type { SalesData } from '../../types';
import { Table2 } from 'lucide-react';

interface TableRendererProps {
  data: SalesData[];
}

export const TableRenderer = ({ data }: TableRendererProps) => {
  if (!data || data.length === 0) {
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
          Sales Data
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b">
                <th className="p-3 text-left text-sm font-semibold">Date</th>
                <th className="p-3 text-left text-sm font-semibold">Product</th>
                <th className="p-3 text-left text-sm font-semibold">Salesperson</th>
                <th className="p-3 text-left text-sm font-semibold">Region</th>
                <th className="p-3 text-right text-sm font-semibold">Amount</th>
              </tr>
            </thead>
            <tbody>
              {data.map((sale) => {
                // Handle both PascalCase and camelCase from backend
                const saleDate = sale.SaleDate || sale.saleDate;
                const product = sale.Product || sale.product;
                const region = sale.Region || sale.region;
                const amount = sale.Amount || sale.amount;
                const salesPerson = sale.SalesPerson || sale.salesPerson;
                const firstName = salesPerson?.FirstName || salesPerson?.firstName;
                const lastName = salesPerson?.LastName || salesPerson?.lastName;
                const id = sale.Id || sale.id;

                return (
                  <tr key={id} className="border-b hover:bg-muted/50">
                    <td className="p-3 text-sm">
                      {new Date(saleDate).toLocaleDateString()}
                    </td>
                    <td className="p-3 text-sm font-medium">{product}</td>
                    <td className="p-3 text-sm">
                      {firstName} {lastName}
                    </td>
                    <td className="p-3 text-sm">{region}</td>
                    <td className="p-3 text-right text-sm font-semibold">
                      ${Number(amount).toLocaleString()}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <div className="mt-4 flex items-center justify-between rounded-lg border p-3 bg-muted/30">
          <span className="text-sm font-medium">Total Sales</span>
          <span className="text-lg font-bold">
            ${data.reduce((sum, sale) => sum + Number(sale.Amount || sale.amount || 0), 0).toLocaleString()}
          </span>
        </div>
      </CardContent>
    </Card>
  );
};

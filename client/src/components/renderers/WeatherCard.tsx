import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Cloud, Droplets, Thermometer, Wind } from 'lucide-react';

import type { WeatherData } from '../../types';

interface WeatherCardProps {
  data: WeatherData[];
}

export const WeatherCard = ({ data }: WeatherCardProps) => {
  if (!data || data.length === 0) {
    return (
      <Card className="w-full">
        <CardContent className="p-6">
          <p className="text-muted-foreground">No weather data available</p>
        </CardContent>
      </Card>
    );
  }

  const latest = data[0];
  // Handle both PascalCase and camelCase
  const location = latest.Location || latest.location;
  const temperature = latest.Temperature ?? latest.temperature;
  const humidity = latest.Humidity ?? latest.humidity;
  const windSpeed = latest.WindSpeed ?? latest.windSpeed;
  const condition = latest.Condition || latest.condition;

  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Cloud className="h-5 w-5" />
          Weather in {location}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <div className="flex items-center gap-3 rounded-lg border p-4">
            <Thermometer className="h-8 w-8 text-orange-500" />
            <div>
              <p className="text-sm text-muted-foreground">Temperature</p>
              <p className="text-2xl font-bold">{temperature}°C</p>
            </div>
          </div>
          
          <div className="flex items-center gap-3 rounded-lg border p-4">
            <Droplets className="h-8 w-8 text-blue-500" />
            <div>
              <p className="text-sm text-muted-foreground">Humidity</p>
              <p className="text-2xl font-bold">{humidity}%</p>
            </div>
          </div>
          
          <div className="flex items-center gap-3 rounded-lg border p-4">
            <Wind className="h-8 w-8 text-gray-500" />
            <div>
              <p className="text-sm text-muted-foreground">Wind Speed</p>
              <p className="text-2xl font-bold">{windSpeed} km/h</p>
            </div>
          </div>
          
          <div className="flex items-center gap-3 rounded-lg border p-4">
            <Cloud className="h-8 w-8 text-sky-500" />
            <div>
              <p className="text-sm text-muted-foreground">Condition</p>
              <p className="text-2xl font-bold">{condition}</p>
            </div>
          </div>
        </div>

        {data.length > 1 && (
          <div className="mt-6">
            <h3 className="mb-3 text-sm font-semibold">7-Day Forecast</h3>
            <div className="space-y-2">
              {data.map((item) => {
                const itemId = item.Id || item.id;
                const itemDate = item.Date || item.date;
                const itemCondition = item.Condition || item.condition;
                const itemTemp = item.Temperature ?? item.temperature;

                return (
                  <div key={itemId} className="flex items-center justify-between rounded-md border p-3">
                    <span className="text-sm text-muted-foreground">
                      {new Date(itemDate!).toLocaleDateString()}
                    </span>
                    <div className="flex items-center gap-4">
                      <span className="text-sm">{itemCondition}</span>
                      <span className="font-semibold">{itemTemp}°C</span>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
};

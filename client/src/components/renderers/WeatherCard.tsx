import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Cloud, Droplets, Thermometer, Wind } from 'lucide-react';

interface WeatherCardProps {
  location: string;
  temperature: number;
  humidity?: number;
  windSpeed?: number;
  condition?: string;
}

export const WeatherCard = ({ location, temperature, humidity, windSpeed, condition }: WeatherCardProps) => {
  if (!location || temperature === undefined) {
    return (
      <Card className="w-full">
        <CardContent className="p-6">
          <p className="text-muted-foreground">No weather data available</p>
        </CardContent>
      </Card>
    );
  }

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
          
          {humidity !== undefined && (
            <div className="flex items-center gap-3 rounded-lg border p-4">
              <Droplets className="h-8 w-8 text-blue-500" />
              <div>
                <p className="text-sm text-muted-foreground">Humidity</p>
                <p className="text-2xl font-bold">{humidity}%</p>
              </div>
            </div>
          )}
          
          {windSpeed !== undefined && (
            <div className="flex items-center gap-3 rounded-lg border p-4">
              <Wind className="h-8 w-8 text-gray-500" />
              <div>
                <p className="text-sm text-muted-foreground">Wind Speed</p>
                <p className="text-2xl font-bold">{windSpeed} km/h</p>
              </div>
            </div>
          )}
          
          {condition && (
            <div className="flex items-center gap-3 rounded-lg border p-4">
              <Cloud className="h-8 w-8 text-sky-500" />
              <div>
                <p className="text-sm text-muted-foreground">Condition</p>
                <p className="text-2xl font-bold">{condition}</p>
              </div>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
};

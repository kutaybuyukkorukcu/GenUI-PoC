namespace FogData.Database.Entities;

public class WeatherData
{
    public int Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Temperature { get; set; }
    public string Condition { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public int WindSpeed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

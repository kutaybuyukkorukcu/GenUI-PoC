## Clean Architecture Implementation Example

### Current Structure (What You Have Now)
```
FogData/
├── Controllers/WeatherForecastController.cs
├── Models/WeatherForecast.cs
├── Services/ (empty)
└── Repositories/ (empty)
```

### Clean Architecture Structure (Evolution Path)
```
src/
├── FogData.Core/                    # 🏛️ ENTITIES LAYER
│   ├── Entities/
│   │   └── WeatherForecast.cs       # Business entity
│   └── Interfaces/
│       ├── IWeatherRepository.cs    # Repository contract
│       └── IWeatherService.cs       # Service contract
│
├── FogData.Application/             # 🔄 USE CASES LAYER
│   ├── UseCases/
│   │   ├── GetWeatherForecast/
│   │   │   ├── GetWeatherForecastRequest.cs
│   │   │   ├── GetWeatherForecastResponse.cs
│   │   │   └── GetWeatherForecastUseCase.cs
│   └── Services/
│       └── IWeatherForecastService.cs  
│
├── FogData.Infrastructure/          # 🔌 INFRASTRUCTURE LAYER
│   ├── Repositories/
│   │   └── WeatherRepository.cs     # EF Core implementation
│   ├── Persistence/
│   │   ├── FogDataDbContext.cs
│   │   └── Configurations/
│   └── External/
│       └── WeatherApiClient.cs
│
└── FogData.Api/                     # ⚙️ PRESENTATION LAYER
    ├── Controllers/
    │   └── WeatherForecastController.cs
    ├── DTOs/
    │   ├── WeatherForecastDto.cs
    └── Extensions/
        └── ServiceCollectionExtensions.cs
```

### 🏛️ ENTITIES LAYER (FogData.Core)
**Purpose**: Core business rules, no external dependencies

```csharp
// Entities/WeatherForecast.cs
namespace FogData.Core.Entities;

public class WeatherForecast
{
    public DateOnly Date { get; private set; }
    public int TemperatureC { get; private set; }
    public string? Summary { get; private set; }

    // Business logic methods
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public bool IsFreezing => TemperatureC <= 0;
    public bool IsHot => TemperatureC >= 30;

    // Constructor for creating new instances
    public WeatherForecast(DateOnly date, int temperatureC, string? summary)
    {
        Date = date;
        TemperatureC = temperatureC;
        Summary = summary ?? "Unknown";
    }

    // Factory method for business rules
    public static WeatherForecast CreateForecast(DateOnly date, int temperatureC, string? summary)
    {
        // Business validation
        if (temperatureC < -100 || temperatureC > 100)
            throw new ArgumentException("Temperature out of realistic range");

        return new WeatherForecast(date, temperatureC, summary);
    }
}
```

```csharp
// Interfaces/IWeatherRepository.cs
namespace FogData.Core.Interfaces;

public interface IWeatherRepository
{
    Task<IEnumerable<WeatherForecast>> GetWeatherForecastsAsync(int days = 5);
    Task<WeatherForecast?> GetWeatherForecastAsync(DateOnly date);
}
```

### 🔄 USE CASES LAYER (FogData.Application)
**Purpose**: Application-specific business logic

```csharp
// UseCases/GetWeatherForecast/GetWeatherForecastRequest.cs
namespace FogData.Application.UseCases.GetWeatherForecast;

public record GetWeatherForecastRequest(int Days = 5);
```

```csharp
// UseCases/GetWeatherForecast/GetWeatherForecastResponse.cs
using FogData.Core.Entities;

namespace FogData.Application.UseCases.GetWeatherForecast;

public record GetWeatherForecastResponse(IEnumerable<WeatherForecast> Forecasts);
```

```csharp
// UseCases/GetWeatherForecast/GetWeatherForecastUseCase.cs
using FogData.Core.Entities;
using FogData.Core.Interfaces;

namespace FogData.Application.UseCases.GetWeatherForecast;

public class GetWeatherForecastUseCase
{
    private readonly IWeatherRepository _weatherRepository;

    public GetWeatherForecastUseCase(IWeatherRepository weatherRepository)
    {
        _weatherRepository = weatherRepository;
    }

    public async Task<GetWeatherForecastResponse> ExecuteAsync(GetWeatherForecastRequest request)
    {
        var forecasts = await _weatherRepository.GetWeatherForecastsAsync(request.Days);

        // Application business rules
        var validForecasts = forecasts.Where(f => f.Date >= DateOnly.FromDateTime(DateTime.Now));

        return new GetWeatherForecastResponse(validForecasts);
    }
}
```

### 🔌 INFRASTRUCTURE LAYER (FogData.Infrastructure)
**Purpose**: External concerns (database, APIs, file system)

```csharp
// Repositories/WeatherRepository.cs
using FogData.Core.Entities;
using FogData.Core.Interfaces;

namespace FogData.Infrastructure.Repositories;

public class WeatherRepository : IWeatherRepository
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public async Task<IEnumerable<WeatherForecast>> GetWeatherForecastsAsync(int days = 5)
    {
        // Simulate async database call
        await Task.Delay(10);

        return Enumerable.Range(1, days).Select(index =>
            WeatherForecast.CreateForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            ));
    }

    public async Task<WeatherForecast?> GetWeatherForecastAsync(DateOnly date)
    {
        await Task.Delay(10);
        // Implementation for single forecast
        return null;
    }
}
```

### ⚙️ PRESENTATION LAYER (FogData.Api)
**Purpose**: HTTP adapters, no business logic

```csharp
// Controllers/WeatherForecastController.cs
using FogData.Application.UseCases.GetWeatherForecast;
using Microsoft.AspNetCore.Mvc;

namespace FogData.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly GetWeatherForecastUseCase _useCase;

    public WeatherForecastController(GetWeatherForecastUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int days = 5)
    {
        var request = new GetWeatherForecastRequest(days);
        var response = await _useCase.ExecuteAsync(request);

        // Convert to DTOs for API response
        var dtos = response.Forecasts.Select(f => new WeatherForecastDto
        {
            Date = f.Date,
            TemperatureC = f.TemperatureC,
            TemperatureF = f.TemperatureF,
            Summary = f.Summary
        });

        return Ok(dtos);
    }
}
```

```csharp
// DTOs/WeatherForecastDto.cs
namespace FogData.Api.DTOs;

public record WeatherForecastDto(
    DateOnly Date,
    int TemperatureC,
    int TemperatureF,
    string? Summary
);
```

```csharp
// Extensions/ServiceCollectionExtensions.cs
using FogData.Application.UseCases.GetWeatherForecast;
using FogData.Core.Interfaces;
using FogData.Infrastructure.Repositories;

namespace FogData.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Infrastructure
        services.AddScoped<IWeatherRepository, WeatherRepository>();

        // Use Cases
        services.AddScoped<GetWeatherForecastUseCase>();

        return services;
    }
}
```

## 🎯 Key Benefits of This Structure

### ✅ **Dependency Direction** (Inner → Outer)
```
Entities ← Use Cases ← Controllers
Entities ← Use Cases ← Repositories
```
- **Core never depends on Infrastructure**
- **Application never depends on Presentation**
- **Easy to test** - mock interfaces at boundaries

### ✅ **Testability**
```csharp
// Test the Use Case in isolation
[Fact]
public async Task GetWeatherForecast_ValidRequest_ReturnsForecasts()
{
    // Arrange
    var mockRepo = new Mock<IWeatherRepository>();
    var useCase = new GetWeatherForecastUseCase(mockRepo.Object);

    // Act
    var result = await useCase.ExecuteAsync(new GetWeatherForecastRequest(3));

    // Assert
    // Test business logic without HTTP or database
}
```

### ✅ **Framework Independence**
- Want to switch from ASP.NET Core to Minimal API? Just change Controllers
- Want to switch from EF Core to Dapper? Just change Repositories
- Business logic stays the same

### ✅ **Parallel Development**
- Backend team works on Core/Application
- Frontend team works on React
- DevOps team works on Infrastructure
- All can work simultaneously

## 🚀 Migration Strategy

**Start Small (Your Current Approach):**
1. Keep everything in one project
2. Use folders for separation
3. Learn patterns gradually

**Scale Up (When Ready):**
1. Extract to separate projects
2. Add proper dependency injection
3. Implement full Clean Architecture

**Your Current Project is Perfect for Learning!** Start with the patterns, then split into projects when you understand why.

## 📚 Recommended Learning Resources

1. **"Clean Architecture" by Robert C. Martin** - The bible
2. **Jason Taylor's Clean Architecture Template** - .NET implementation
3. **Microsoft's eShopOnContainers** - Real-world example
4. **Ardalis Clean Architecture** - .NET focused

---

**Bottom Line:** Clean Architecture is about **protecting your business logic** from external changes. It's not overkill - it's essential for professional, maintainable code. Start with your current structure and evolve toward this pattern as you learn! 🚀
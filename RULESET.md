# FogData Project Ruleset

## Developer Context
- **Familiar with**: .NET 8, C#, ASP.NET Core Web API
- **Current Project**: .NET 9.0 Web API with React TypeScript frontend
- **Experience Level**: Intermediate to Advanced .NET developer
- **Learning Goal**: Actively learning .NET and backend development properly, don't need to explain frontend since i'm proficient
- **Preferred Approach**: Educational explanations with feedback and best practices

## Code Guidelines

### Educational Approach (IMPORTANT)
When implementing features or providing solutions:
- ✅ **DO** explain the "why" behind architectural decisions
- ✅ **DO** provide educational context and learning opportunities
- ✅ **DO** point out best practices and common pitfalls
- ✅ **DO** suggest better alternatives when applicable
- ✅ **DO** explain trade-offs between different approaches
- ✅ **DO** give feedback on code quality and improvements
- ✅ **DO** teach concepts, not just write code

### Always Explain .NET Version Differences
When implementing features or using syntax:
- ✅ **DO** explain if it's new in .NET 9 vs .NET 8
- ✅ **DO** point out breaking changes or new features
- ✅ **DO** provide context on why we're using newer features
- ✅ **DO** mention alternatives that existed in .NET 8

### Architecture Patterns

#### Models/DTOs
- **Use `record` types** for DTOs and API models (available since C# 9.0/.NET 5)
- **Use `class` types** for entities with behavior
- **No interfaces needed** for models/DTOs (they're just data containers)
- Keep models in `/Models` folder

#### Services (Business Logic)
- **Always define interfaces** (e.g., `IWeatherService`)
- **Implement interface** in concrete class (e.g., `WeatherService`)
- Register as scoped services: `services.AddScoped<IWeatherService, WeatherService>()`
- Keep in `/Services` folder

#### Repositories (Data Access)
- **Always define interfaces** (e.g., `IWeatherRepository`)
- **Implement interface** in concrete class (e.g., `WeatherRepository`)
- Register as scoped services: `services.AddScoped<IWeatherRepository, WeatherRepository>()`
- Keep in `/Repositories` folder

#### Controllers
- Inherit from `ControllerBase` (API-only, no views)
- Use `[ApiController]` attribute
- Use `[Route("api/[controller]")]` for routing
- Inject services via constructor (Dependency Injection)
- Keep in `/Controllers` folder

#### Middleware
- **Define as classes** with `InvokeAsync` method
- Register in `Program.cs` with `app.UseMiddleware<T>()`
- Keep in `/Middleware` folder

#### Extensions
- Use static classes with extension methods
- Name: `*Extensions.cs` (e.g., `ServiceCollectionExtensions.cs`)
- Purpose: Clean up `Program.cs` by grouping service registrations
- Keep in `/Extensions` folder

### Dependency Injection
- Use constructor injection for all dependencies
- Register services in `Program.cs` or extension methods
- Prefer scoped lifetime for most services
- Use interfaces for testability

### Naming Conventions
- **Controllers**: `*Controller.cs` (e.g., `WeatherForecastController`)
- **Services**: `I*Service.cs` + `*Service.cs`
- **Repositories**: `I*Repository.cs` + `*Repository.cs`
- **Models**: Descriptive names (e.g., `WeatherForecast`, `UserDto`)
- **Folders**: PascalCase singular or plural as appropriate

### API Design
- Use RESTful conventions
- Route: `/api/[controller]/[action]`
- Return proper HTTP status codes
- Use async/await for all I/O operations
- Use action verbs: `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`

### Frontend (React)
- TypeScript only
- Use Vite for build tooling
- API calls via fetch to `/api/*` endpoints
- Proxy configured in `vite.config.ts` for development

### Code Quality
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Use implicit usings
- Prefer pattern matching and modern C# syntax
- Keep methods focused and single-purpose
- Add XML documentation comments for public APIs

## Project Structure Rules

### Current Structure (Monolithic API)
```
FogData/
├── Controllers/       # API Controllers
├── Models/           # DTOs, Records (no interfaces)
├── Services/         # Business logic (with interfaces)
├── Repositories/     # Data access (with interfaces)
├── Middleware/       # Custom middleware
├── Extensions/       # Service extensions
├── client/           # React TypeScript app
└── wwwroot/          # Built React app (production)
```

### Future Growth Path
When project reaches maturity, split into:
- `src/FogData.Api/` - Web API project
- `src/FogData.Core/` - Domain/business logic (Class Library)
- `src/FogData.Infrastructure/` - Data access (Class Library)

## Technology Stack
- **Backend**: ASP.NET Core 9.0 Web API
- **Frontend**: React 18+ with TypeScript
- **Build Tool**: Vite
- **Package Manager**: npm
- **Language**: C# 13, TypeScript 5+

## Development Workflow
1. **Backend**: Run with `dotnet run` (default port check console)
2. **Frontend Dev**: Run with `npm run dev` from `client/` folder (port 5173)
3. **Production Build**: `dotnet publish -c Release` (auto-builds React)

## Common Commands Reference
```bash
# Backend
dotnet run                    # Run API
dotnet build                  # Build API
dotnet publish -c Release     # Build for production
dotnet add package <name>     # Add NuGet package

# Frontend
cd client
npm install                   # Install dependencies
npm run dev                   # Run dev server
npm run build                 # Build for production
npm run preview               # Preview production build
```

## Notes
- CORS enabled in development for React app (port 5173)
- Production serves React from `wwwroot/`
- API proxy configured in Vite for `/api/*` routes
- Use record types for DTOs (not new, available since .NET 5)
- Interfaces required for services/repositories, NOT for models

---
**Last Updated**: November 12, 2025
**Reference this file** for all architectural and coding decisions.

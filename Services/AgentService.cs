using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FogData.Database;
using FogData.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FogData.Services;

namespace FogData.Services;

public class AgentService : IAgentService
{
    private readonly Kernel _kernel;
    private readonly FogDataDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AgentService(FogDataDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;

        // Initialize Semantic Kernel with AI provider
        var builder = Kernel.CreateBuilder();
        
        var provider = _configuration["SemanticKernel:Provider"];
        
        if (provider == "OpenAI")
        {
            var apiKey = _configuration["SemanticKernel:OpenAI:ApiKey"];
            var modelId = _configuration["SemanticKernel:OpenAI:ModelId"] ?? "gpt-4o-mini";
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                builder.AddOpenAIChatCompletion(modelId, apiKey);
            }
        }
        else if (provider == "AzureOpenAI")
        {
            var endpoint = _configuration["SemanticKernel:AzureOpenAI:Endpoint"];
            var apiKey = _configuration["SemanticKernel:AzureOpenAI:ApiKey"];
            var deploymentName = _configuration["SemanticKernel:AzureOpenAI:DeploymentName"];
            
            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                builder.AddAzureOpenAIChatCompletion(deploymentName!, endpoint, apiKey);
            }
        }
        
        _kernel = builder.Build();

        // Register available tools
        RegisterTools();
    }

    private void RegisterTools()
    {
        // Weather tool
        _kernel.Plugins.AddFromFunctions("WeatherTools",
            new[] {
                _kernel.CreateFunctionFromMethod(
                    method: GetWeatherDataAsync,
                    functionName: "GetWeatherData",
                    description: "Get weather information for a specific location. Returns current weather conditions including temperature, humidity, and wind speed."
                )
            });

        // Sales tool
        _kernel.Plugins.AddFromFunctions("SalesTools",
            new[] {
                _kernel.CreateFunctionFromMethod(
                    method: GetSalesDataAsync,
                    functionName: "GetSalesData",
                    description: "Get sales data for analysis. Can filter by region, date range, or salesperson."
                ),
                _kernel.CreateFunctionFromMethod(
                    method: GetTopSalesPeopleAsync,
                    functionName: "GetTopSalesPeople",
                    description: "Get the top performing salespeople by total sales amount."
                )
            });
    }

    public async Task<AgentResponse> AnalyzeIntentAsync(string userInput)
    {
        // Check if AI is configured
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        
        var prompt = $$$"""
        You are an intelligent agent that analyzes user requests and determines which tool to call.

        Available tools:
        1. GetWeatherData - For weather-related queries (location, temperature, conditions)
           Parameters: location (string)
        
        2. GetSalesData - For sales data queries (sales by region, product, date range)
           Parameters: region (string, optional), startDate (string, optional), endDate (string, optional)
        
        3. GetTopSalesPeople - For finding top performing salespeople by total sales amount
           Parameters: limit (number, default: 5)

        User input: "{{{userInput}}}"

        Analyze the request and respond with ONLY a JSON object in this exact format:
        {
            "intent": "brief description of what user wants",
            "toolToCall": "exact tool name from the list above",
            "parameters": { "key": "value" },
            "reasoning": "why this tool was chosen"
        }
        """;

        var result = await _kernel.InvokePromptAsync(prompt);
        var response = result.GetValue<string>();

        try
        {
            // Extract JSON from response (in case AI adds extra text)
            var jsonStart = response!.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = System.Text.Json.JsonSerializer.Deserialize<AgentResponse>(jsonContent);
                return parsed ?? new AgentResponse
                {
                    Intent = "unknown",
                    ToolToCall = "none",
                    Reasoning = "Failed to parse response"
                };
            }
            
            return new AgentResponse
            {
                Intent = "unknown",
                ToolToCall = "none",
                Reasoning = "No JSON found in response"
            };
        }
        catch (Exception ex)
        {
            return new AgentResponse
            {
                Intent = "unknown",
                ToolToCall = "none",
                Reasoning = $"Failed to parse AI response: {ex.Message}"
            };
        }
    }

    public async Task<ToolCallResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        try
        {
            switch (toolName)
            {
                case "GetWeatherData":
                    var weatherData = await GetWeatherDataAsync(
                        parameters.GetValueOrDefault("location", "New York")?.ToString() ?? "New York");
                    return new ToolCallResult
                    {
                        Success = true,
                        Data = weatherData,
                        ComponentType = "weather"
                    };

                case "GetSalesData":
                    var salesData = await GetSalesDataAsync(
                        parameters.GetValueOrDefault("region")?.ToString(),
                        parameters.GetValueOrDefault("startDate")?.ToString(),
                        parameters.GetValueOrDefault("endDate")?.ToString());
                    return new ToolCallResult
                    {
                        Success = true,
                        Data = salesData,
                        ComponentType = "table"
                    };

                case "GetTopSalesPeople":
                    var topPerformers = await GetTopSalesPeopleAsync(
                        int.Parse(parameters.GetValueOrDefault("limit", "5")?.ToString() ?? "5"));
                    return new ToolCallResult
                    {
                        Success = true,
                        Data = topPerformers,
                        ComponentType = "chart"
                    };

                default:
                    return new ToolCallResult
                    {
                        Success = false,
                        Error = $"Unknown tool: {toolName}",
                        ComponentType = "error"
                    };
            }
        }
        catch (Exception ex)
        {
            return new ToolCallResult
            {
                Success = false,
                Error = ex.Message,
                ComponentType = "error"
            };
        }
    }

    // Tool implementations
    private async Task<List<WeatherData>> GetWeatherDataAsync(string location)
    {
        return await _dbContext.WeatherData
            .Where(w => w.Location.Contains(location))
            .OrderByDescending(w => w.Date)
            .Take(7)
            .ToListAsync();
    }

    private async Task<List<SalesData>> GetSalesDataAsync(string? region = null, string? startDate = null, string? endDate = null)
    {
        var query = _dbContext.SalesData.AsQueryable();

        if (!string.IsNullOrEmpty(region))
            query = query.Where(s => s.Region == region);

        if (DateTime.TryParse(startDate, out var start))
            query = query.Where(s => s.SaleDate >= start);

        if (DateTime.TryParse(endDate, out var end))
            query = query.Where(s => s.SaleDate <= end);

        return await query
            .Include(s => s.SalesPerson)
            .OrderByDescending(s => s.SaleDate)
            .Take(50)
            .ToListAsync();
    }

    private async Task<List<SalesPersonPerformance>> GetTopSalesPeopleAsync(int limit = 5)
    {
        return await _dbContext.SalesData
            .Include(s => s.SalesPerson)
            .GroupBy(s => s.SalesPerson)
            .Select(g => new SalesPersonPerformance
            {
                SalesPersonName = $"{g.Key!.FirstName} {g.Key.LastName}",
                TotalSales = g.Sum(s => s.Amount),
                SalesCount = g.Count(),
                Region = g.Key!.Region
            })
            .OrderByDescending(p => p.TotalSales)
            .Take(limit)
            .ToListAsync();
    }
}

public record SalesPersonPerformance
{
    public string SalesPersonName { get; init; } = string.Empty;
    public decimal TotalSales { get; init; }
    public int SalesCount { get; init; }
    public string Region { get; init; } = string.Empty;
}
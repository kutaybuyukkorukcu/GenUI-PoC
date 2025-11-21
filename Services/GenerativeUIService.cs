using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FogData.Database;
using FogData.Database.Entities;
using Microsoft.EntityFrameworkCore;
using FogData.Services.GenerativeUI;
using System.Text.Json;

namespace FogData.Services;

/// <summary>
/// Service that uses LLM to generate UI components via JSON DSL format.
/// This is a parallel implementation to AgentService that uses a different approach:
/// instead of pattern matching and tool calling, the LLM directly generates
/// the complete UI specification in structured JSON format.
/// </summary>
public class GenerativeUIService : IGenerativeUIService
{
    private readonly Kernel _kernel;
    private readonly FogDataDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GenerativeUIService> _logger;
    private readonly ChatHistory _chatHistory;
    
    public GenerativeUIService(
        FogDataDbContext dbContext, 
        IConfiguration configuration,
        ILogger<GenerativeUIService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _chatHistory = new ChatHistory();

        // Initialize Semantic Kernel (similar setup to AgentService)
        _kernel = CreateKernel(configuration, logger);
    }
    
    /// <summary>
    /// Creates and configures the Semantic Kernel based on provider settings
    /// </summary>
    private static Kernel CreateKernel(IConfiguration configuration, ILogger logger)
    {
        var builder = Kernel.CreateBuilder();
        
        var provider = configuration["SemanticKernel:Provider"];
        logger.LogInformation("Initializing GenerativeUIService with provider: {Provider}", provider);
        
        if (provider == "OpenAI")
        {
            var apiKey = configuration["SemanticKernel:OpenAI:ApiKey"];
            var modelId = configuration["SemanticKernel:OpenAI:ModelId"] ?? "gpt-4o-mini";
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                builder.AddOpenAIChatCompletion(modelId, apiKey);
            }
        }
        else if (provider == "AzureOpenAI")
        {
            var endpoint = configuration["SemanticKernel:AzureOpenAI:Endpoint"];
            var apiKey = configuration["SemanticKernel:AzureOpenAI:ApiKey"];
            var deploymentName = configuration["SemanticKernel:AzureOpenAI:DeploymentName"];
            
            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                builder.AddAzureOpenAIChatCompletion(deploymentName!, endpoint, apiKey);
            }
        }
        else if (provider == "Ollama")
        {
            var endpoint = configuration["SemanticKernel:Ollama:Endpoint"];
            var modelId = configuration["SemanticKernel:Ollama:ModelId"] ?? "llama3.2";
            
            if (!string.IsNullOrEmpty(endpoint))
            {
                builder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: null,
                    endpoint: new Uri(endpoint)
                );
            }
        }
        
        return builder.Build();
    }
    
    /// <summary>
    /// Processes user message and generates JSON DSL response
    /// </summary>
    public async IAsyncEnumerable<string> ProcessUserMessageAsync(string userMessage)
    {
        _logger.LogInformation("GenerativeUIService processing message: {Message}", userMessage);
        
        var responseBuilder = new GenerativeUIResponseBuilder();
        
        // Step 1: Add initial thinking state
        responseBuilder.AddThinkingItem("Analyzing your query...", "active");
        yield return responseBuilder.BuildPartial();
        
        QueryAnalysis? queryAnalysis = null;
        object? data = null;
        Exception? processingError = null;
        
        // Step 2: Analyze query and determine what data is needed
        try
        {
            queryAnalysis = await AnalyzeQueryAsync(userMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing query");
            processingError = ex;
        }
        
        if (processingError == null && queryAnalysis != null)
        {
            responseBuilder.UpdateLastThinkingStatus("complete");
            responseBuilder.AddThinkingItem($"Query type: {queryAnalysis.QueryType}", "complete");
            responseBuilder.AddThinkingItem("Fetching data...", "active");
            yield return responseBuilder.BuildPartial();
            
            // Step 3: Fetch data based on query analysis
            try
            {
                data = await FetchDataAsync(queryAnalysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data");
                processingError = ex;
            }
        }
        
        if (processingError == null && queryAnalysis != null)
        {
            responseBuilder.UpdateLastThinkingStatus("complete");
            responseBuilder.AddThinkingItem("Generating response...", "active");
            yield return responseBuilder.BuildPartial();
        }
        
        // Step 4: Build the response with text and components
        if (processingError != null)
        {
            responseBuilder.UpdateLastThinkingStatus("complete");
            responseBuilder.AddText($"I encountered an error: {processingError.Message}");
            responseBuilder.AddMetadata("error", true);
        }
        else if (queryAnalysis != null)
        {
            await BuildResponseContentAsync(responseBuilder, queryAnalysis, data, userMessage);
            responseBuilder.UpdateLastThinkingStatus("complete");
            
            // Step 5: Add metadata
            responseBuilder.AddMetadata("modelUsed", _configuration["SemanticKernel:Provider"] ?? "unknown");
            responseBuilder.AddMetadata("queryType", queryAnalysis.QueryType);
        }
        
        // Step 6: Return final response
        yield return responseBuilder.Build();
    }
    
    /// <summary>
    /// Analyzes the user query to determine intent and data requirements
    /// </summary>
    private async Task<QueryAnalysis> AnalyzeQueryAsync(string userMessage)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        
        var analysisPrompt = $@"
Analyze this user query and determine:
1. What type of query is it? (weather, sales, performance, general)
2. What specific data is requested?
3. What location/timeframe/filters are mentioned?

User query: ""{userMessage}""

Respond with a JSON object:
{{
  ""queryType"": ""weather|sales|performance|general"",
  ""location"": ""location if mentioned"",
  ""timeframe"": ""timeframe if mentioned"",
  ""filters"": {{}}
}}
";

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(analysisPrompt);
        
        var response = await chatService.GetChatMessageContentAsync(chatHistory);
        var responseText = response.Content ?? "{}";
        
        try
        {
            return JsonSerializer.Deserialize<QueryAnalysis>(responseText) ?? new QueryAnalysis();
        }
        catch
        {
            // Fallback: try to infer from keywords
            return InferQueryType(userMessage);
        }
    }
    
    /// <summary>
    /// Fallback method to infer query type from keywords
    /// </summary>
    private QueryAnalysis InferQueryType(string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();
        
        if (lower.Contains("weather") || lower.Contains("temperature") || lower.Contains("forecast"))
        {
            return new QueryAnalysis { QueryType = "weather" };
        }
        else if (lower.Contains("sales") || lower.Contains("revenue") || lower.Contains("sold"))
        {
            return new QueryAnalysis { QueryType = "sales" };
        }
        else if (lower.Contains("performance") || lower.Contains("top") || lower.Contains("best"))
        {
            return new QueryAnalysis { QueryType = "performance" };
        }
        
        return new QueryAnalysis { QueryType = "general" };
    }
    
    /// <summary>
    /// Fetches data from database based on query analysis
    /// </summary>
    private async Task<object?> FetchDataAsync(QueryAnalysis analysis)
    {
        switch (analysis.QueryType)
        {
            case "weather":
                return await _dbContext.WeatherData
                    .OrderByDescending(w => w.Date)
                    .Take(10)
                    .ToListAsync();
                
            case "sales":
                return await _dbContext.SalesData
                    .Include(s => s.SalesPerson)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(20)
                    .ToListAsync();
                
            case "performance":
                return await _dbContext.SalesData
                    .Include(s => s.SalesPerson)
                    .GroupBy(s => new { s.SalesPerson!.Id, s.SalesPerson.FirstName, s.SalesPerson.LastName })
                    .Select(g => new
                    {
                        SalesPersonName = $"{g.Key.FirstName} {g.Key.LastName}",
                        TotalSales = g.Sum(s => s.Amount),
                        SalesCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .Take(10)
                    .ToListAsync();
                
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Builds the response content with text and components
    /// </summary>
    private async Task BuildResponseContentAsync(
        GenerativeUIResponseBuilder builder, 
        QueryAnalysis analysis, 
        object? data,
        string userMessage)
    {
        switch (analysis.QueryType)
        {
            case "weather":
                await BuildWeatherResponseAsync(builder, data as List<WeatherData>, userMessage);
                break;
                
            case "sales":
                await BuildSalesResponseAsync(builder, data as List<SalesData>, userMessage);
                break;
                
            case "performance":
                await BuildPerformanceResponseAsync(builder, data, userMessage);
                break;
                
            default:
                builder.AddText("I'm not sure how to help with that. Try asking about weather, sales, or performance data.");
                break;
        }
    }
    
    private async Task BuildWeatherResponseAsync(GenerativeUIResponseBuilder builder, List<WeatherData>? weatherData, string userMessage)
    {
        if (weatherData == null || !weatherData.Any())
        {
            builder.AddText("I couldn't find any weather data.");
            return;
        }
        
        var latestWeather = weatherData.First();
        
        builder.AddText($"Here's the latest weather data for {latestWeather.Location}:");
        
        builder.AddComponent("weather", new
        {
            location = latestWeather.Location,
            temperature = latestWeather.Temperature,
            condition = latestWeather.Condition,
            humidity = latestWeather.Humidity,
            windSpeed = latestWeather.WindSpeed,
            date = latestWeather.Date.ToString("MMM dd, yyyy")
        });
        
        // Add contextual text based on conditions
        if (latestWeather.Temperature > 80)
        {
            builder.AddText("It's quite warm! Stay hydrated. 🌞");
        }
        else if (latestWeather.Temperature < 40)
        {
            builder.AddText("Bundle up, it's cold outside! 🧥");
        }
        
        await Task.CompletedTask;
    }
    
    private async Task BuildSalesResponseAsync(GenerativeUIResponseBuilder builder, List<SalesData>? salesData, string userMessage)
    {
        if (salesData == null || !salesData.Any())
        {
            builder.AddText("I couldn't find any sales data.");
            return;
        }
        
        builder.AddText($"Here are the latest {salesData.Count} sales records:");
        
        builder.AddComponent("table", new
        {
            columns = new[] { "Product", "Amount", "Region", "Date", "Salesperson" },
            rows = salesData.Select(s => new
            {
                product = s.Product,
                amount = s.Amount,
                region = s.Region,
                date = s.SaleDate.ToString("MMM dd, yyyy"),
                salesperson = s.SalesPerson != null ? $"{s.SalesPerson.FirstName} {s.SalesPerson.LastName}" : "Unknown"
            }).ToList()
        });
        
        var totalRevenue = salesData.Sum(s => s.Amount);
        builder.AddText($"Total revenue: ${totalRevenue:N2}");
        
        await Task.CompletedTask;
    }
    
    private async Task BuildPerformanceResponseAsync(GenerativeUIResponseBuilder builder, object? performanceData, string userMessage)
    {
        if (performanceData == null)
        {
            builder.AddText("I couldn't find any performance data.");
            return;
        }
        
        builder.AddText("Here are the top performing salespeople:");
        
        builder.AddComponent("chart", new
        {
            type = "bar",
            data = performanceData,
            xAxis = "salesPersonName",
            yAxis = "totalSales",
            title = "Sales Performance"
        });
        
        builder.AddText("Great work from the team! 🎯");
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Internal class for query analysis
    /// </summary>
    private class QueryAnalysis
    {
        public string QueryType { get; set; } = "general";
        public string? Location { get; set; }
        public string? Timeframe { get; set; }
        public Dictionary<string, object>? Filters { get; set; }
    }
}

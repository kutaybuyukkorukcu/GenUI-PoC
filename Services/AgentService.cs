using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FogData.Database;
using FogData.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FogData.Services;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FogData.Services;

public class AgentService : IAgentService
{
    private readonly Kernel _kernel;
    private readonly FogDataDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AgentService> _logger;
    private readonly ChatHistory _chatHistory;
    
    // Track tool invocations for component rendering
    private readonly List<ToolInvocationInfo> _toolInvocations = new();

    public AgentService(FogDataDbContext dbContext, IConfiguration configuration, ILogger<AgentService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _chatHistory = new ChatHistory();

        // Initialize Semantic Kernel with AI provider
        var builder = Kernel.CreateBuilder();
        
        var provider = _configuration["SemanticKernel:Provider"];
        _logger.LogInformation("Initializing Semantic Kernel with provider: {Provider}", provider);
        
        if (provider == "OpenAI")
        {
            var apiKey = _configuration["SemanticKernel:OpenAI:ApiKey"];
            var modelId = _configuration["SemanticKernel:OpenAI:ModelId"] ?? "gpt-4o-mini";
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                _logger.LogInformation("Configuring OpenAI with model: {ModelId}", modelId);
                builder.AddOpenAIChatCompletion(modelId, apiKey);
            }
            else
            {
                _logger.LogWarning("OpenAI API key is empty");
            }
        }
        else if (provider == "AzureOpenAI")
        {
            var endpoint = _configuration["SemanticKernel:AzureOpenAI:Endpoint"];
            var apiKey = _configuration["SemanticKernel:AzureOpenAI:ApiKey"];
            var deploymentName = _configuration["SemanticKernel:AzureOpenAI:DeploymentName"];
            
            _logger.LogInformation("Configuring Azure OpenAI - Endpoint: {Endpoint}, Deployment: {Deployment}", 
                endpoint, deploymentName);
            
            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                _logger.LogInformation("Azure OpenAI API key length: {Length}", apiKey.Length);
                builder.AddAzureOpenAIChatCompletion(deploymentName!, endpoint, apiKey);
            }
            else
            {
                _logger.LogError("Azure OpenAI configuration incomplete - Endpoint: {HasEndpoint}, ApiKey: {HasKey}", 
                    !string.IsNullOrEmpty(endpoint), !string.IsNullOrEmpty(apiKey));
            }
        }
        else if (provider == "Ollama")
        {
            var endpoint = _configuration["SemanticKernel:Ollama:Endpoint"];
            var modelId = _configuration["SemanticKernel:Ollama:ModelId"] ?? "llama3.2";
            
            _logger.LogInformation("Configuring Ollama - Endpoint: {Endpoint}, Model: {ModelId}", 
                endpoint, modelId);
            
            if (!string.IsNullOrEmpty(endpoint))
            {
                var ollamaUri = new Uri(endpoint);
                builder.AddOpenAIChatCompletion(
                    modelId: modelId,
                    apiKey: null, // Ollama doesn't require API key
                    endpoint: ollamaUri
                );
            }
            else
            {
                _logger.LogError("Ollama endpoint is not configured");
            }
        }
        
        _kernel = builder.Build();

        // Register available tools
        RegisterTools();
        
        // Set system prompt
        _chatHistory.AddSystemMessage($"""
            You are a helpful AI assistant with access to tools for retrieving weather data, sales data, and sales performance metrics.
            Current date: {DateTime.UtcNow:yyyy-MM-dd}
            
            When users ask for data:
            1. Use the appropriate tool to fetch the information
            2. After receiving the tool results, provide a natural, conversational summary
            3. Highlight key insights and interesting patterns
            4. Be concise but informative
            
            Available tools and when to use them:
            - GetWeatherData: For weather-related queries about specific locations
            - GetSalesData: For detailed sales transactions, filtered by region or date range
            - GetTopSalesPeople: For sales performance rankings and top performers
            """);
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
                    description: "Get sales data for analysis. Can filter by region, date range, or salesperson. Returns detailed sales transactions."
                ),
                _kernel.CreateFunctionFromMethod(
                    method: GetTopSalesPeopleAsync,
                    functionName: "GetTopSalesPeople",
                    description: "Get the top performing salespeople by total sales amount. Use this for sales rankings and performance comparisons."
                )
            });
    }

    public async IAsyncEnumerable<StreamingChatUpdate> ProcessUserMessageAsync(string userInput)
    {
        _logger.LogInformation("ProcessUserMessageAsync called with input: {Input}", userInput);
        var cancellationToken = CancellationToken.None;
        
        // Clear previous tool invocations
        _toolInvocations.Clear();
        
        // Add user message to history
        _chatHistory.AddUserMessage(userInput);
        
        // Get chat completion service
        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        
        // Configure automatic function calling with a filter to track invocations
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            MaxTokens = 2000
        };

        // Create a filter to capture tool invocations
        var kernelArguments = new KernelArguments(executionSettings);
        
        // Add function filter to track tool calls
        _kernel.FunctionInvocationFilters.Add(new ToolTrackingFilter(_toolInvocations, _logger));

        try
        {
            // Get streaming response
            var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
                _chatHistory,
                executionSettings,
                _kernel,
                cancellationToken);

            var fullMessage = string.Empty;
            var hasYieldedToolResults = false;

            await foreach (var content in response.WithCancellation(cancellationToken))
            {
                // Stream text content
                if (!string.IsNullOrEmpty(content.Content))
                {
                    fullMessage += content.Content;
                    
                    // If we have tool invocations and haven't yielded them yet, do it before streaming text
                    if (_toolInvocations.Count > 0 && !hasYieldedToolResults)
                    {
                        foreach (var toolInfo in _toolInvocations)
                        {
                            _logger.LogInformation("Yielding tool result for: {ToolName}", toolInfo.ToolName);
                            
                            yield return new StreamingChatUpdate
                            {
                                Type = "tool-result",
                                ToolName = toolInfo.ToolName,
                                ToolResult = new ToolCallResult
                                {
                                    Success = true,
                                    Data = toolInfo.Result,
                                    ComponentType = toolInfo.ComponentType
                                },
                                ComponentType = toolInfo.ComponentType
                            };
                        }
                        hasYieldedToolResults = true;
                    }
                    
                    // Stream synthesis text
                    yield return new StreamingChatUpdate
                    {
                        Type = "synthesis",
                        Content = content.Content
                    };
                }
            }

            // Add assistant response to history
            if (!string.IsNullOrEmpty(fullMessage))
            {
                _chatHistory.AddAssistantMessage(fullMessage);
            }
            
            _logger.LogInformation("Response complete. Message length: {Length}, Tool calls: {ToolCount}", 
                fullMessage.Length, _toolInvocations.Count);
        }
        finally
        {
            // Clean up the filter
            _kernel.FunctionInvocationFilters.Clear();
        }
    }

    // Tool implementations with return type annotations for Semantic Kernel
    [Description("Get weather data for a specific location")]
    private async Task<List<WeatherData>> GetWeatherDataAsync(
        [Description("The location to get weather for")] string location)
    {
        _logger.LogInformation("GetWeatherDataAsync called with location: {Location}", location);
        
        return await _dbContext.WeatherData
            .Where(w => w.Location.Contains(location))
            .OrderByDescending(w => w.Date)
            .Take(7)
            .ToListAsync();
    }

    [Description("Get sales data with optional filters")]
    private async Task<List<SalesData>> GetSalesDataAsync(
        [Description("Optional region filter")] string? region = null,
        [Description("Optional start date in YYYY-MM-DD format")] string? startDate = null,
        [Description("Optional end date in YYYY-MM-DD format")] string? endDate = null)
    {
        _logger.LogInformation("GetSalesDataAsync called - Region: {Region}, StartDate: {StartDate}, EndDate: {EndDate}", 
            region, startDate, endDate);
        
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

    [Description("Get top performing salespeople by total sales")]
    private async Task<List<SalesPersonPerformance>> GetTopSalesPeopleAsync(
        [Description("Number of top performers to return, default is 5")] int limit = 5)
    {
        _logger.LogInformation("GetTopSalesPeopleAsync called with limit: {Limit}", limit);
        
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

// Helper class to track tool invocations
internal class ToolInvocationInfo
{
    public string ToolName { get; set; } = string.Empty;
    public object? Arguments { get; set; }
    public object? Result { get; set; }
    public string ComponentType { get; set; } = string.Empty;
}

// Filter to capture tool invocations
internal class ToolTrackingFilter : IFunctionInvocationFilter
{
    private readonly List<ToolInvocationInfo> _toolInvocations;
    private readonly ILogger _logger;

    public ToolTrackingFilter(List<ToolInvocationInfo> toolInvocations, ILogger logger)
    {
        _toolInvocations = toolInvocations;
        _logger = logger;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        // Let the function execute
        await next(context);

        // Capture the result
        var toolName = context.Function.Name;
        var result = context.Result?.GetValue<object>();

        // Map tool name to component type
        var componentType = toolName switch
        {
            "GetWeatherData" => "weather",
            "GetSalesData" => "table",
            "GetTopSalesPeople" => "chart",
            _ => "unknown"
        };

        _logger.LogInformation("Tool invoked: {ToolName}, ComponentType: {ComponentType}, ResultType: {ResultType}", 
            toolName, componentType, result?.GetType().Name ?? "null");

        _toolInvocations.Add(new ToolInvocationInfo
        {
            ToolName = toolName,
            Arguments = context.Arguments,
            Result = result,
            ComponentType = componentType
        });
    }
}

public record SalesPersonPerformance
{
    public string SalesPersonName { get; init; } = string.Empty;
    public decimal TotalSales { get; init; }
    public int SalesCount { get; init; }
    public string Region { get; init; } = string.Empty;
}
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
        
        // Register tools for LLM to call
        RegisterTools();
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
    
    private void RegisterTools()
    {
        // Weather tool
        _kernel.Plugins.AddFromFunctions("WeatherTools",
            new[] {
                _kernel.CreateFunctionFromMethod(
                    method: GetWeatherDataAsync,
                    functionName: "GetWeatherData",
                    description: "Get weather information for a specific location. Returns current weather conditions."
                )
            });

        // Sales tools
        _kernel.Plugins.AddFromFunctions("SalesTools",
            new[] {
                _kernel.CreateFunctionFromMethod(
                    method: GetSalesDataAsync,
                    functionName: "GetSalesData",
                    description: "Get sales data for analysis. Returns detailed sales transactions."
                ),
                _kernel.CreateFunctionFromMethod(
                    method: GetTopSalesPeopleAsync,
                    functionName: "GetTopSalesPeople",
                    description: "Get the top performing salespeople by total sales amount."
                )
            });
            
        // Data modification tools
        _kernel.Plugins.AddFromFunctions("DataTools",
            new[] {
                _kernel.CreateFunctionFromMethod(
                    method: ShowAddSaleFormAsync,
                    functionName: "ShowAddSaleForm",
                    description: "Shows a form to collect information for adding a new sale to the database. Use when user wants to add/create a sale."
                ),
                _kernel.CreateFunctionFromMethod(
                    method: ShowAddPersonFormAsync,
                    functionName: "ShowAddPersonForm",
                    description: "Shows a form to collect information for adding a new salesperson. Use when user wants to add/create a person or salesperson."
                )
            });
    }
    
    [System.ComponentModel.Description("Get weather data for a specific location")]
    private async Task<List<WeatherData>> GetWeatherDataAsync(
        [System.ComponentModel.Description("The location to get weather for")] string location)
    {
        return await _dbContext.WeatherData
            .Where(w => w.Location.Contains(location))
            .OrderByDescending(w => w.Date)
            .Take(7)
            .ToListAsync();
    }

    [System.ComponentModel.Description("Get sales data with optional filters")]
    private async Task<List<SalesData>> GetSalesDataAsync(
        [System.ComponentModel.Description("Optional region filter")] string? region = null,
        [System.ComponentModel.Description("Optional start date in YYYY-MM-DD format")] string? startDate = null,
        [System.ComponentModel.Description("Optional end date in YYYY-MM-DD format")] string? endDate = null)
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
            .Take(20)
            .ToListAsync();
    }

    [System.ComponentModel.Description("Get top performing salespeople by total sales")]
    private async Task<List<SalesPersonPerformance>> GetTopSalesPeopleAsync(
        [System.ComponentModel.Description("Number of top performers to return, default is 5")] int limit = 5)
    {
        return await _dbContext.SalesData
            .Include(s => s.SalesPerson)
            .GroupBy(s => new { s.SalesPerson!.Id, s.SalesPerson.FirstName, s.SalesPerson.LastName })
            .Select(g => new SalesPersonPerformance
            {
                SalesPersonName = $"{g.Key.FirstName} {g.Key.LastName}",
                TotalSales = g.Sum(s => s.Amount),
                SalesCount = g.Count()
            })
            .OrderByDescending(p => p.TotalSales)
            .Take(limit)
            .ToListAsync();
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
        else if (lower.Contains("add") && (lower.Contains("sale") || lower.Contains("person")))
        {
            return new QueryAnalysis { QueryType = "add-data" };
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
                
            case "add-data":
                await BuildAddDataFlowAsync(builder, userMessage);
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
    /// Builds an interactive flow for adding data with form and confirmation
    /// </summary>
    private async Task BuildAddDataFlowAsync(GenerativeUIResponseBuilder builder, string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();
        
        // Check if this is a form submission (will contain structured data)
        if (userMessage.Contains("FORM_SUBMIT:"))
        {
            // Parse form data and show confirmation
            await BuildConfirmationFromFormAsync(builder, userMessage);
            return;
        }
        
        // Check if this is a confirmation (user clicked confirm button)
        if (lower.Contains("confirm") && _chatHistory.Count > 2)
        {
            await ExecuteDataAdditionAsync(builder);
            return;
        }
        
        // If we get here, the LLM should have called a tool but didn't
        builder.AddText("I can help you add data to the database. Try:");
        builder.AddText("• 'add a sale' - Add a new sale record");
        builder.AddText("• 'add a salesperson' - Add a new person to the team");
        
        await Task.CompletedTask;
    }
    
    // Tool method for showing add sale form
    [System.ComponentModel.Description("Shows a form to add a new sale to the database")]
    private async Task<string> ShowAddSaleFormAsync()
    {
        return await Task.FromResult("SHOW_SALE_FORM");
    }
    
    // Tool method for showing add person form
    [System.ComponentModel.Description("Shows a form to add a new salesperson to the database")]
    private async Task<string> ShowAddPersonFormAsync()
    {
        return await Task.FromResult("SHOW_PERSON_FORM");
    }
    
    /// <summary>
    /// Builds confirmation dialog from form submission data
    /// </summary>
    private async Task BuildConfirmationFromFormAsync(GenerativeUIResponseBuilder builder, string userMessage)
    {
        // Extract form data (in real app, this would be properly parsed JSON)
        var formDataStart = userMessage.IndexOf("FORM_SUBMIT:") + 12;
        var formDataJson = userMessage.Substring(formDataStart);
        
        try
        {
            var formData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(formDataJson);
            
            if (formData == null)
            {
                builder.AddText("Sorry, I couldn't parse the form data. Please try again.");
                return;
            }
            
            // Check if this is a sale or person
            if (formData.ContainsKey("product"))
            {
                // Sale confirmation
                builder.AddText("Great! Here's what you're about to add:");
                
                var confirmationData = new Dictionary<string, object>
                {
                    { "Product", formData["product"].GetString() ?? "" },
                    { "Amount", $"${formData["amount"].GetDecimal()}" },
                    { "Region", formData["region"].GetString() ?? "" },
                    { "Salesperson", formData["salesperson"].GetString() ?? "" },
                    { "Date", formData["date"].GetString() ?? "" }
                };
                
                builder.AddComponent("confirmation", new
                {
                    title = "Add This Sale?",
                    message = "Would you like to add this sale to the database?",
                    confirmText = "Yes, Add Sale",
                    cancelText = "Cancel",
                    variant = "info",
                    data = confirmationData
                });
                
                // Store form data in chat history metadata for later use
                _chatHistory.AddUserMessage($"PENDING_SALE:{formDataJson}");
            }
            else if (formData.ContainsKey("firstName"))
            {
                // Person confirmation
                builder.AddText("Perfect! Review the information:");
                
                var confirmationData = new Dictionary<string, object>
                {
                    { "First Name", formData["firstName"].GetString() ?? "" },
                    { "Last Name", formData["lastName"].GetString() ?? "" },
                    { "Email", formData["email"].GetString() ?? "" },
                    { "Region", formData["region"].GetString() ?? "" }
                };
                
                builder.AddComponent("confirmation", new
                {
                    title = "Add This Person?",
                    message = "Would you like to add this salesperson to the database?",
                    confirmText = "Yes, Add Person",
                    cancelText = "Cancel",
                    variant = "info",
                    data = confirmationData
                });
                
                _chatHistory.AddUserMessage($"PENDING_PERSON:{formDataJson}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing form data");
            builder.AddText("Sorry, there was an error processing your form. Please try again.");
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Executes the actual database addition after confirmation
    /// </summary>
    private async Task ExecuteDataAdditionAsync(GenerativeUIResponseBuilder builder)
    {
        // Find the pending data in chat history
        var pendingMessage = _chatHistory.LastOrDefault(m => 
            m.Content?.Contains("PENDING_SALE:") == true || 
            m.Content?.Contains("PENDING_PERSON:") == true);
        
        if (pendingMessage == null)
        {
            builder.AddText("I couldn't find the pending data. Please start over.");
            return;
        }
        
        try
        {
            if (pendingMessage.Content!.Contains("PENDING_SALE:"))
            {
                var jsonStart = pendingMessage.Content.IndexOf("PENDING_SALE:") + 13;
                var jsonData = pendingMessage.Content.Substring(jsonStart);
                var formData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonData);
                
                if (formData != null)
                {
                    // Find or create salesperson
                    var email = formData["salesperson"].GetString();
                    var salesperson = await _dbContext.People
                        .FirstOrDefaultAsync(p => p.Email == email);
                    
                    if (salesperson == null)
                    {
                        builder.AddText($"⚠️ Salesperson with email {email} not found. Please add them first.");
                        return;
                    }
                    
                    // Create sale
                    var sale = new SalesData
                    {
                        Product = formData["product"].GetString() ?? "",
                        Amount = formData["amount"].GetDecimal(),
                        Region = formData["region"].GetString() ?? "",
                        SaleDate = DateTime.Parse(formData["date"].GetString() ?? DateTime.Today.ToString()),
                        SalesPersonId = salesperson.Id
                    };
                    
                    _dbContext.SalesData.Add(sale);
                    await _dbContext.SaveChangesAsync();
                    
                    builder.AddText("✅ Sale added successfully!");
                    
                    // Show the added sale in a table
                    builder.AddComponent("table", new
                    {
                        columns = new[] { "Product", "Amount", "Region", "Date", "Salesperson" },
                        rows = new[]
                        {
                            new
                            {
                                product = sale.Product,
                                amount = sale.Amount,
                                region = sale.Region,
                                date = sale.SaleDate.ToString("MMM dd, yyyy"),
                                salesperson = $"{salesperson.FirstName} {salesperson.LastName}"
                            }
                        }
                    });
                }
            }
            else if (pendingMessage.Content.Contains("PENDING_PERSON:"))
            {
                var jsonStart = pendingMessage.Content.IndexOf("PENDING_PERSON:") + 15;
                var jsonData = pendingMessage.Content.Substring(jsonStart);
                var formData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonData);
                
                if (formData != null)
                {
                    var person = new Person
                    {
                        FirstName = formData["firstName"].GetString() ?? "",
                        LastName = formData["lastName"].GetString() ?? "",
                        Email = formData["email"].GetString() ?? "",
                        Region = formData["region"].GetString() ?? ""
                    };
                    
                    _dbContext.People.Add(person);
                    await _dbContext.SaveChangesAsync();
                    
                    builder.AddText("✅ Salesperson added successfully!");
                    
                    builder.AddComponent("table", new
                    {
                        columns = new[] { "Name", "Email", "Region" },
                        rows = new[]
                        {
                            new
                            {
                                name = $"{person.FirstName} {person.LastName}",
                                email = person.Email,
                                region = person.Region
                            }
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding data to database");
            builder.AddText($"❌ Error: {ex.Message}");
        }
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

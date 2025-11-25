using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FogData.Database;
using FogData.Database.Entities;
using FogData.Models.GenerativeUI;
using Microsoft.EntityFrameworkCore;
using FogData.Services.GenerativeUI;
using System.Text.Json;

namespace FogData.Services;

/// <summary>
/// Generic Generative UI Service - Domain Agnostic.
/// Works with any data domain by analyzing query intent and data structure,
/// not hardcoded business logic. Suitable for SaaS products serving multiple industries.
/// </summary>
public class GenerativeUIService : IGenerativeUIService
{
    private readonly Kernel _kernel;
    private readonly FogDataDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GenerativeUIService> _logger;
    private readonly ChatHistory _chatHistory;
    private readonly QueryAnalyzer _queryAnalyzer;
    private readonly ComponentDecisionEngine _componentEngine;
    
    public GenerativeUIService(
        FogDataDbContext dbContext, 
        IConfiguration configuration,
        ILogger<GenerativeUIService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _chatHistory = new ChatHistory();

        // Initialize Semantic Kernel
        _kernel = CreateKernel(configuration, logger);
        
        // Initialize generic analyzers
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _queryAnalyzer = new QueryAnalyzer(_kernel, loggerFactory.CreateLogger<QueryAnalyzer>());
        _componentEngine = new ComponentDecisionEngine();
        
        // Register generic data access tools (not domain-specific)
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
        // Generic data query tools - these can work with any entity
        _kernel.Plugins.AddFromFunctions("DataAccess",
            new[] {
                _kernel.CreateFunctionFromMethod(
                    method: QueryDataAsync,
                    functionName: "QueryData",
                    description: "Query any data from the database. Specify entity type (WeatherData, SalesData, Person) and optional filters."
                ),
                _kernel.CreateFunctionFromMethod(
                    method: AggregateDataAsync,
                    functionName: "AggregateData",
                    description: "Aggregate/group data for analysis. Useful for performance metrics, summaries, and comparisons."
                )
            });
    }
    
    /// <summary>
    /// Generic data query method - works with any entity
    /// </summary>
    [System.ComponentModel.Description("Query data from database")]
    private async Task<string> QueryDataAsync(
        [System.ComponentModel.Description("Entity type: WeatherData, SalesData, Person")] string entityType,
        [System.ComponentModel.Description("Optional filters as JSON")] string? filters = null)
    {
        try
        {
            object? result = entityType.ToLowerInvariant() switch
            {
                "weatherdata" or "weather" => await QueryWeatherDataAsync(filters),
                "salesdata" or "sales" => await QuerySalesDataAsync(filters),
                "person" or "people" => await QueryPeopleDataAsync(filters),
                _ => null
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying {EntityType}", entityType);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generic aggregation method for analytics
    /// </summary>
    [System.ComponentModel.Description("Aggregate data for analysis")]
    private async Task<string> AggregateDataAsync(
        [System.ComponentModel.Description("Entity type to aggregate")] string entityType,
        [System.ComponentModel.Description("Grouping field")] string groupBy,
        [System.ComponentModel.Description("Aggregation type: sum, count, avg")] string aggregationType = "sum")
    {
        try
        {
            object? result = entityType.ToLowerInvariant() switch
            {
                "salesdata" or "sales" => await AggregateSalesDataAsync(groupBy, aggregationType),
                _ => null
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating {EntityType}", entityType);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    // Helper methods for specific entity queries
    private async Task<List<WeatherData>> QueryWeatherDataAsync(string? filters)
    {
        var query = _dbContext.WeatherData.AsQueryable();
        
        // Apply basic filters (can be extended with JSON parsing)
        return await query
            .OrderByDescending(w => w.Date)
            .Take(20)
            .ToListAsync();
    }

    private async Task<List<SalesData>> QuerySalesDataAsync(string? filters)
    {
        var query = _dbContext.SalesData.Include(s => s.SalesPerson).AsQueryable();
        
        // Apply basic filters (can be extended with JSON parsing)
        return await query
            .OrderByDescending(s => s.SaleDate)
            .Take(20)
            .ToListAsync();
    }

    private async Task<List<Person>> QueryPeopleDataAsync(string? filters)
    {
        return await _dbContext.People
            .OrderBy(p => p.LastName)
            .Take(20)
            .ToListAsync();
    }

    private async Task<object> AggregateSalesDataAsync(string groupBy, string aggregationType)
    {
        if (groupBy.ToLowerInvariant().Contains("person") || groupBy.ToLowerInvariant().Contains("salesperson"))
        {
            return await _dbContext.SalesData
                .Include(s => s.SalesPerson)
                .GroupBy(s => new { s.SalesPerson!.Id, s.SalesPerson.FirstName, s.SalesPerson.LastName })
                .Select(g => new
                {
                    Name = $"{g.Key.FirstName} {g.Key.LastName}",
                    TotalSales = g.Sum(s => s.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalSales)
                .Take(10)
                .ToListAsync();
        }
        
        // Default: group by region
        return await _dbContext.SalesData
            .GroupBy(s => s.Region)
            .Select(g => new
            {
                Region = g.Key,
                TotalSales = g.Sum(s => s.Amount),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalSales)
            .ToListAsync();
    }
    
    /// <summary>
    /// Processes user message and generates JSON DSL response - GENERIC APPROACH
    /// Works with any data domain by analyzing intent and data structure
    /// </summary>
    public async IAsyncEnumerable<string> ProcessUserMessageAsync(string userMessage)
    {
        _logger.LogInformation("GenerativeUIService processing message: {Message}", userMessage);
        
        var responseBuilder = new GenerativeUIResponseBuilder();
        
        // Step 1: Add initial thinking state
        responseBuilder.AddThinkingItem("Analyzing your query...", "active");
        yield return responseBuilder.BuildPartial();
        
        QueryIntent? intent = null;
        object? data = null;
        Exception? processingError = null;
        
        // Step 2: Analyze query intent (generic - not domain-specific)
        try
        {
            intent = await _queryAnalyzer.AnalyzeIntentAsync(userMessage);
            _logger.LogInformation("Query intent: {Intent}, RequiresInput: {RequiresInput}", 
                intent.Type, intent.RequiresInput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing query intent");
            processingError = ex;
        }
        
        if (processingError == null && intent != null)
        {
            responseBuilder.UpdateLastThinkingStatus("complete");
            responseBuilder.AddThinkingItem($"Intent: {intent.Type}", "complete");
            
            // Step 3: Handle input-required scenarios (forms)
            if (intent.RequiresInput)
            {
                responseBuilder.AddThinkingItem("Preparing input form...", "active");
                yield return responseBuilder.BuildPartial();
                
                await BuildInputFormAsync(responseBuilder, intent, userMessage);
                responseBuilder.UpdateLastThinkingStatus("complete");
            }
            else
            {
                // Step 4: Fetch data using LLM tool calling
                responseBuilder.AddThinkingItem("Fetching relevant data...", "active");
                yield return responseBuilder.BuildPartial();
                
                try
                {
                    data = await FetchDataUsingLLMAsync(userMessage, intent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching data");
                    processingError = ex;
                }
                
                if (processingError == null)
                {
                    responseBuilder.UpdateLastThinkingStatus("complete");
                    responseBuilder.AddThinkingItem("Analyzing data structure...", "active");
                    yield return responseBuilder.BuildPartial();
                    
                    // Step 5: Analyze data structure and decide component
                    var dataAnalysis = _queryAnalyzer.AnalyzeDataStructure(data);
                    var componentDecision = _componentEngine.DecideComponent(intent, dataAnalysis, data);
                    
                    _logger.LogInformation("Component decision: {ComponentType} - {Reasoning}", 
                        componentDecision.ComponentType, componentDecision.Reasoning);
                    
                    responseBuilder.UpdateLastThinkingStatus("complete");
                    responseBuilder.AddThinkingItem($"Rendering {componentDecision.ComponentType}...", "active");
                    yield return responseBuilder.BuildPartial();
                    
                    // Step 6: Build generic response with selected component
                    await BuildGenericResponseAsync(responseBuilder, intent, data, componentDecision, userMessage);
                    responseBuilder.UpdateLastThinkingStatus("complete");
                }
            }
        }
        
        // Step 7: Handle errors
        if (processingError != null)
        {
            responseBuilder.UpdateLastThinkingStatus("complete");
            responseBuilder.AddText($"I encountered an error: {processingError.Message}");
            responseBuilder.AddMetadata("error", true);
        }
        
        // Step 8: Add metadata
        if (intent != null)
        {
            responseBuilder.AddMetadata("modelUsed", _configuration["SemanticKernel:Provider"] ?? "unknown");
            responseBuilder.AddMetadata("intentType", intent.Type.ToString());
        }
        
        // Step 9: Return final response
        yield return responseBuilder.Build();
    }
    
    /// <summary>
    /// Fetches data using LLM tool calling - lets the LLM decide which tools to use
    /// </summary>
    private async Task<object?> FetchDataUsingLLMAsync(string userMessage, QueryIntent intent)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        
        // Build a prompt that guides the LLM to use tools
        var dataPrompt = $@"User query: ""{userMessage}""

Based on this query, determine what data needs to be fetched from the database.
Use the available tools to query the appropriate data.
Return the results as JSON.";

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a data assistant. Use the available tools to fetch relevant data based on user queries.");
        chatHistory.AddUserMessage(dataPrompt);
        
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.3
        };
        
        try
        {
            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel
            );
            
            var content = response.Content ?? "{}";
            
            // Try to deserialize as data
            try
            {
                return JsonSerializer.Deserialize<List<object>>(content);
            }
            catch
            {
                // If not a list, might be single object or aggregated data
                return content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LLM tool calling");
            
            // Fallback: direct database query based on intent
            return await FallbackDataFetchAsync(intent);
        }
    }
    
    /// <summary>
    /// Fallback data fetching when LLM tool calling fails
    /// </summary>
    private async Task<object?> FallbackDataFetchAsync(QueryIntent intent)
    {
        return intent.Type switch
        {
            QueryIntentType.Analyze or QueryIntentType.Compare => await AggregateSalesDataAsync("salesperson", "sum"),
            QueryIntentType.View or QueryIntentType.Search => await QuerySalesDataAsync(null),
            _ => await QuerySalesDataAsync(null)
        };
    }
    
    /// <summary>
    /// Builds input forms for CREATE/UPDATE operations
    /// </summary>
    private async Task BuildInputFormAsync(GenerativeUIResponseBuilder builder, QueryIntent intent, string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();
        
        // Determine what type of form to show based on keywords
        if (lower.Contains("sale"))
        {
            await BuildAddSaleFormAsync(builder);
        }
        else if (lower.Contains("person") || lower.Contains("employee") || lower.Contains("salesperson"))
        {
            await BuildAddPersonFormAsync(builder);
        }
        else
        {
            // Generic help text
            builder.AddText("I can help you add data. What would you like to create?");
            builder.AddText("• **Sale record** - Add a new transaction");
            builder.AddText("• **Person** - Add a team member or contact");
        }
    }
    
    /// <summary>
    /// Builds a generic response with appropriate component based on decision engine
    /// </summary>
    private async Task BuildGenericResponseAsync(
        GenerativeUIResponseBuilder builder,
        QueryIntent intent,
        object? data,
        ComponentDecision decision,
        string userMessage)
    {
        if (data == null)
        {
            builder.AddText("I couldn't find any relevant data for your query.");
            return;
        }
        
        // Add contextual text based on intent
        var contextText = intent.Type switch
        {
            QueryIntentType.Analyze => "Here's an analysis of the data:",
            QueryIntentType.Compare => "Here's a comparison:",
            QueryIntentType.Search => "Here are the search results:",
            QueryIntentType.View => "Here's the requested information:",
            _ => "Here's what I found:"
        };
        
        builder.AddText(contextText);
        
        // Render the appropriate component based on decision
        switch (decision.ComponentType)
        {
            case "card":
                await RenderCardComponentAsync(builder, data);
                break;
                
            case "list":
                await RenderListComponentAsync(builder, data);
                break;
                
            case "table":
                await RenderTableComponentAsync(builder, data);
                break;
                
            case "chart":
                await RenderChartComponentAsync(builder, data, intent);
                break;
                
            default:
                // Fallback: just show data as JSON in a card
                builder.AddCard("Data", data);
                break;
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Generic card renderer - works with any single object
    /// </summary>
    private async Task RenderCardComponentAsync(GenerativeUIResponseBuilder builder, object data)
    {
        // Extract a title if possible
        var type = data.GetType(); 
        var title = type.Name;
        
        builder.AddCard(title, data);
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Generic list renderer - works with any collection
    /// </summary>
    private async Task RenderListComponentAsync(GenerativeUIResponseBuilder builder, object data)
    {
        if (data is System.Collections.IEnumerable enumerable)
        {
            var items = enumerable.Cast<object>().Take(50).ToList();
            builder.AddList(items, layout: "grid");
        }
        else
        {
            builder.AddList(new[] { data }, layout: "list");
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Generic table renderer - dynamically extracts columns from data
    /// </summary>
    private async Task RenderTableComponentAsync(GenerativeUIResponseBuilder builder, object data)
    {
        if (data is not System.Collections.IEnumerable enumerable)
        {
            builder.AddText("Unable to render table: data is not a collection");
            return;
        }
        
        var items = enumerable.Cast<object>().ToList();
        if (items.Count == 0)
        {
            builder.AddText("No data to display");
            return;
        }
        
        // Extract columns from first item
        var firstItem = items[0];
        var properties = firstItem.GetType().GetProperties();
        var columns = properties
            .Where(p => p.PropertyType.IsPrimitive || 
                       p.PropertyType == typeof(string) || 
                       p.PropertyType == typeof(decimal) ||
                       p.PropertyType == typeof(DateTime))
            .Select(p => new { name = p.Name, label = p.Name })
            .ToArray();
        
        // Extract rows
        var rows = items.Select(item =>
        {
            var row = new Dictionary<string, object?>();
            foreach (var prop in properties)
            {
                if (columns.Any(c => c.name == prop.Name))
                {
                    var value = prop.GetValue(item);
                    row[prop.Name] = value;
                }
            }
            return row;
        }).ToArray();
        
        builder.AddTable(columns, rows, sortable: true);
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Generic chart renderer - determines chart type based on data
    /// </summary>
    private async Task RenderChartComponentAsync(GenerativeUIResponseBuilder builder, object data, QueryIntent intent)
    {
        if (data is not System.Collections.IEnumerable enumerable)
        {
            builder.AddText("Unable to render chart: data is not a collection");
            return;
        }
        
        var items = enumerable.Cast<object>().ToList();
        if (items.Count == 0)
        {
            builder.AddText("No data to visualize");
            return;
        }
        
        // Determine chart type based on data structure
        var firstItem = items[0];
        var properties = firstItem.GetType().GetProperties();
        
        // Find numeric and label fields
        var numericProp = properties.FirstOrDefault(p => 
            p.PropertyType == typeof(int) || 
            p.PropertyType == typeof(decimal) || 
            p.PropertyType == typeof(double));
        
        var labelProp = properties.FirstOrDefault(p => p.PropertyType == typeof(string));
        
        if (numericProp != null && labelProp != null)
        {
            var chartType = intent.Type == QueryIntentType.Compare ? "bar" : "line";
            
            builder.AddChart(
                chartType: chartType,
                data: items,
                xAxis: labelProp.Name,
                yAxis: numericProp.Name,
                title: $"{numericProp.Name} by {labelProp.Name}"
            );
        }
        else
        {
            // Fallback to table if can't determine chart structure
            await RenderTableComponentAsync(builder, data);
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Builds a form to add a new sale with embedded submit action
    /// </summary>
    private async Task BuildAddSaleFormAsync(GenerativeUIResponseBuilder builder)
    {
        // Get available salespeople for the dropdown
        var salespeople = await _dbContext.People
            .Select(p => new { p.Email, Name = $"{p.FirstName} {p.LastName}" })
            .ToListAsync();
        
        builder.AddText("Fill out the form below to add a new sale:");
        
        var fields = new object[]
        {
            new
            {
                name = "product",
                label = "Product",
                type = "select",
                required = true,
                options = new[] { "Laptop Pro 15", "Mechanical Keyboard", "Wireless Mouse", "Webcam HD", "USB-C Hub" }
            },
            new
            {
                name = "amount",
                label = "Sale Amount ($)",
                type = "number",
                placeholder = "1299.99",
                required = true
            },
            new
            {
                name = "region",
                label = "Region",
                type = "select",
                required = true,
                options = new[] { "North America", "Europe", "Asia Pacific", "South America" }
            },
            new
            {
                name = "salespersonEmail",
                label = "Salesperson",
                type = "select",
                required = true,
                options = salespeople.Select(s => s.Email).ToArray()
            },
            new
            {
                name = "date",
                label = "Sale Date",
                type = "date",
                required = true,
                defaultValue = DateTime.Today.ToString("yyyy-MM-dd")
            }
        };
        
        // The action contains everything needed to create the sale - STATELESS!
        builder.AddForm(
            title: "Add New Sale",
            description: "Enter the sale details below",
            fields: fields,
            onSubmitAction: new ComponentAction
            {
                Endpoint = "/api/actions/sales",
                Method = "POST",
                ConfirmMessage = "Are you sure you want to add this sale?"
            },
            submitText: "Add Sale"
        );
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Builds a form to add a new salesperson with embedded submit action
    /// </summary>
    private async Task BuildAddPersonFormAsync(GenerativeUIResponseBuilder builder)
    {
        builder.AddText("Fill out the form below to add a new salesperson:");
        
        var fields = new object[]
        {
            new
            {
                name = "firstName",
                label = "First Name",
                type = "text",
                placeholder = "John",
                required = true
            },
            new
            {
                name = "lastName",
                label = "Last Name",
                type = "text",
                placeholder = "Smith",
                required = true
            },
            new
            {
                name = "email",
                label = "Email",
                type = "email",
                placeholder = "john.smith@company.com",
                required = true
            },
            new
            {
                name = "region",
                label = "Region",
                type = "select",
                required = true,
                options = new[] { "North America", "Europe", "Asia Pacific", "South America" }
            }
        };
        
        // The action contains everything needed - STATELESS!
        builder.AddForm(
            title: "Add New Salesperson",
            description: "Enter the person's details below",
            fields: fields,
            onSubmitAction: new ComponentAction
            {
                Endpoint = "/api/actions/people",
                Method = "POST",
                ConfirmMessage = "Are you sure you want to add this person?"
            },
            submitText: "Add Person"
        );
        
        await Task.CompletedTask;
    }
    
}

using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FogData.Services.GenerativeUI;

/// <summary>
/// Generic query analyzer that determines user intent without domain-specific logic.
/// Works for any domain: finance, healthcare, e-commerce, etc.
/// </summary>
public class QueryAnalyzer
{
    private readonly Kernel _kernel;
    private readonly ILogger<QueryAnalyzer> _logger;

    public QueryAnalyzer(Kernel kernel, ILogger<QueryAnalyzer> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a user query to determine intent using LLM reasoning
    /// </summary>
    public async Task<QueryIntent> AnalyzeIntentAsync(string userMessage)
    {
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        
        var analysisPrompt = $@"Analyze this user query and determine their intent.

User query: ""{userMessage}""

Classify the intent as one of:
- VIEW: User wants to see/retrieve data
- ANALYZE: User wants to analyze/visualize data (trends, patterns, comparisons)
- COMPARE: User wants to compare multiple items
- SEARCH: User wants to search/filter for specific data
- CREATE: User wants to add/create new data
- UPDATE: User wants to modify existing data
- DELETE: User wants to remove data

Also determine:
- Does this require user input (form)? (true/false)
- Extract any parameters mentioned (filters, time ranges, etc.)

Respond ONLY with valid JSON in this exact format:
{{
  ""intentType"": ""VIEW|ANALYZE|COMPARE|SEARCH|CREATE|UPDATE|DELETE"",
  ""requiresInput"": true|false,
  ""parameters"": {{}}
}}";

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(analysisPrompt);
        
        try
        {
            var response = await chatService.GetChatMessageContentAsync(chatHistory);
            var responseText = response.Content ?? "{}";
            
            // Clean up response (remove markdown code blocks if present)
            responseText = CleanJsonResponse(responseText);
            
            var parsed = JsonSerializer.Deserialize<IntentAnalysisResponse>(responseText);
            
            return new QueryIntent
            {
                Type = ParseIntentType(parsed?.IntentType),
                RequiresInput = parsed?.RequiresInput ?? false,
                Parameters = parsed?.Parameters ?? new()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM intent analysis, using keyword fallback");
            return FallbackIntentAnalysis(userMessage);
        }
    }

    /// <summary>
    /// Analyzes data structure to determine best component representation
    /// </summary>
    public DataStructureAnalysis AnalyzeDataStructure(object? data)
    {
        if (data == null)
        {
            return new DataStructureAnalysis { StructureType = DataStructureType.Unknown };
        }

        // Check if it's a collection
        if (data is System.Collections.IEnumerable enumerable and not string)
        {
            var items = enumerable.Cast<object>().ToList();
            
            if (items.Count == 0)
            {
                return new DataStructureAnalysis 
                { 
                    StructureType = DataStructureType.Collection,
                    RecordCount = 0
                };
            }

            if (items.Count == 1)
            {
                return new DataStructureAnalysis 
                { 
                    StructureType = DataStructureType.SingleRecord,
                    RecordCount = 1
                };
            }

            // Analyze the first item to understand structure
            var firstItem = items[0];
            var properties = GetObjectProperties(firstItem);
            
            bool hasTimeComponent = properties.Any(p => 
                p.Contains("date", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("time", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("timestamp", StringComparison.OrdinalIgnoreCase));
            
            bool isAggregated = properties.Any(p =>
                p.Contains("total", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("sum", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("count", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("average", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("avg", StringComparison.OrdinalIgnoreCase));

            var structureType = DataStructureType.Collection;
            if (hasTimeComponent && items.Count > 2)
            {
                structureType = DataStructureType.TimeSeries;
            }
            else if (isAggregated)
            {
                structureType = DataStructureType.Aggregated;
            }

            return new DataStructureAnalysis
            {
                StructureType = structureType,
                HasManyColumns = properties.Count > 4,
                HasTimeComponent = hasTimeComponent,
                IsAggregated = isAggregated,
                RecordCount = items.Count
            };
        }

        // Single object
        return new DataStructureAnalysis 
        { 
            StructureType = DataStructureType.SingleRecord,
            RecordCount = 1
        };
    }

    private List<string> GetObjectProperties(object obj)
    {
        if (obj == null) return new List<string>();
        
        var type = obj.GetType();
        return type.GetProperties()
            .Select(p => p.Name)
            .ToList();
    }

    private string CleanJsonResponse(string response)
    {
        // Remove markdown code blocks
        response = response.Trim();
        if (response.StartsWith("```json"))
        {
            response = response.Substring(7);
        }
        if (response.StartsWith("```"))
        {
            response = response.Substring(3);
        }
        if (response.EndsWith("```"))
        {
            response = response.Substring(0, response.Length - 3);
        }
        return response.Trim();
    }

    private QueryIntentType ParseIntentType(string? intentType)
    {
        return intentType?.ToUpperInvariant() switch
        {
            "VIEW" => QueryIntentType.View,
            "ANALYZE" => QueryIntentType.Analyze,
            "COMPARE" => QueryIntentType.Compare,
            "SEARCH" => QueryIntentType.Search,
            "CREATE" => QueryIntentType.Create,
            "UPDATE" => QueryIntentType.Update,
            "DELETE" => QueryIntentType.Delete,
            _ => QueryIntentType.Unknown
        };
    }

    /// <summary>
    /// Fallback keyword-based intent analysis when LLM fails
    /// </summary>
    private QueryIntent FallbackIntentAnalysis(string userMessage)
    {
        var lower = userMessage.ToLowerInvariant();
        
        if (lower.Contains("add") || lower.Contains("create") || lower.Contains("new"))
        {
            return new QueryIntent 
            { 
                Type = QueryIntentType.Create,
                RequiresInput = true
            };
        }
        
        if (lower.Contains("update") || lower.Contains("edit") || lower.Contains("modify"))
        {
            return new QueryIntent 
            { 
                Type = QueryIntentType.Update,
                RequiresInput = true
            };
        }
        
        if (lower.Contains("delete") || lower.Contains("remove"))
        {
            return new QueryIntent 
            { 
                Type = QueryIntentType.Delete,
                RequiresInput = false
            };
        }
        
        if (lower.Contains("compare") || lower.Contains("vs") || lower.Contains("versus"))
        {
            return new QueryIntent { Type = QueryIntentType.Compare };
        }
        
        if (lower.Contains("analyze") || lower.Contains("trend") || lower.Contains("pattern") || 
            lower.Contains("performance") || lower.Contains("top") || lower.Contains("best"))
        {
            return new QueryIntent { Type = QueryIntentType.Analyze };
        }
        
        if (lower.Contains("search") || lower.Contains("find") || lower.Contains("filter"))
        {
            return new QueryIntent { Type = QueryIntentType.Search };
        }
        
        // Default to VIEW
        return new QueryIntent { Type = QueryIntentType.View };
    }

    private class IntentAnalysisResponse
    {
        public string? IntentType { get; set; }
        public bool RequiresInput { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}

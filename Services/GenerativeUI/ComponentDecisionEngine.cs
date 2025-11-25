using System.Text.Json;

namespace FogData.Services.GenerativeUI;

/// <summary>
/// Generic component decision engine that maps user intent and data structure to appropriate UI components.
/// This is domain-agnostic - it works with any data, not just sales/weather.
/// </summary>
public class ComponentDecisionEngine
{
    /// <summary>
    /// Decides which component(s) to render based on query analysis and data
    /// </summary>
    public ComponentDecision DecideComponent(QueryIntent intent, DataStructureAnalysis dataAnalysis, object? data)
    {
        // Handle multi-step/form interactions
        if (intent.RequiresInput)
        {
            return new ComponentDecision
            {
                ComponentType = "form",
                Reasoning = "User needs to provide input data",
                RecommendedProps = new { }
            };
        }
        
        // Handle empty data
        if (data == null || IsDataEmpty(data))
        {
            return new ComponentDecision
            {
                ComponentType = "text",
                Reasoning = "No data available",
                RecommendedProps = new { }
            };
        }
        
        // Match based on data structure and intent
        return (dataAnalysis.StructureType, intent.Type) switch
        {
            // Single record → Card
            (DataStructureType.SingleRecord, _) => new ComponentDecision
            {
                ComponentType = "card",
                Reasoning = "Single record is best displayed as a card",
                RecommendedProps = new { layout = "detailed" }
            },
            
            // Time-series data → Chart
            (DataStructureType.TimeSeries, QueryIntentType.Analyze or QueryIntentType.Compare) => new ComponentDecision
            {
                ComponentType = "chart",
                Reasoning = "Time-series data is best visualized as a chart",
                RecommendedProps = new { chartType = "line" }
            },
            
            // Aggregated/grouped data → Chart (bar/pie)
            (DataStructureType.Aggregated, QueryIntentType.Analyze or QueryIntentType.Compare) => new ComponentDecision
            {
                ComponentType = "chart",
                Reasoning = "Aggregated data is best visualized as a bar/pie chart",
                RecommendedProps = new { chartType = "bar" }
            },
            
            // Collection of records → Table or List
            (DataStructureType.Collection, QueryIntentType.View or QueryIntentType.Search) when dataAnalysis.HasManyColumns => 
                new ComponentDecision
                {
                    ComponentType = "table",
                    Reasoning = "Tabular data with multiple columns is best shown in a table",
                    RecommendedProps = new { sortable = true, filterable = true }
                },
            
            (DataStructureType.Collection, QueryIntentType.View or QueryIntentType.Search) => new ComponentDecision
            {
                ComponentType = "list",
                Reasoning = "Collection of items is best shown as a list",
                RecommendedProps = new { layout = "grid" }
            },
            
            // Hierarchical data → List with nesting or Tree
            (DataStructureType.Hierarchical, _) => new ComponentDecision
            {
                ComponentType = "list",
                Reasoning = "Hierarchical data shown as nested list",
                RecommendedProps = new { layout = "nested" }
            },
            
            // Default fallback
            _ => new ComponentDecision
            {
                ComponentType = "card",
                Reasoning = "Default card view for unstructured data",
                RecommendedProps = new { }
            }
        };
    }
    
    private bool IsDataEmpty(object data)
    {
        if (data is System.Collections.IEnumerable enumerable)
        {
            return !enumerable.Cast<object>().Any();
        }
        return false;
    }
}

/// <summary>
/// Represents the user's intent when making a query
/// </summary>
public class QueryIntent
{
    public QueryIntentType Type { get; set; }
    public bool RequiresInput { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public enum QueryIntentType
{
    View,       // View/retrieve data
    Analyze,    // Analyze/visualize data
    Compare,    // Compare multiple items
    Search,     // Search/filter data
    Create,     // Create new record
    Update,     // Update existing record
    Delete,     // Delete record
    Unknown
}

/// <summary>
/// Analysis of the data structure to determine best component
/// </summary>
public class DataStructureAnalysis
{
    public DataStructureType StructureType { get; set; }
    public bool HasManyColumns { get; set; }
    public bool HasTimeComponent { get; set; }
    public bool IsAggregated { get; set; }
    public int RecordCount { get; set; }
}

public enum DataStructureType
{
    SingleRecord,   // One object/record
    Collection,     // Array/list of similar items
    TimeSeries,     // Data with time dimension
    Aggregated,     // Grouped/summarized data
    Hierarchical,   // Nested/tree structure
    Unknown
}

/// <summary>
/// Result of component decision process
/// </summary>
public class ComponentDecision
{
    public string ComponentType { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public object RecommendedProps { get; set; } = new { };
}

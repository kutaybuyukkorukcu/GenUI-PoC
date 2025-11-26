using System.Text.Json;

namespace FogData.Services.GenerativeUI;

/// <summary>
/// Helper class to build GenerativeUI JSON DSL responses progressively.
/// Supports streaming by allowing partial builds during response generation.
/// </summary>
public class GenerativeUIResponseBuilder
{
    private readonly GenerativeUIResponse _response = new();
    private readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false // Compact for streaming
    };
    
    /// <summary>
    /// Adds a thinking item to show the AI's reasoning process
    /// </summary>
    /// <param name="message">The thinking message</param>
    /// <param name="status">"active" or "complete"</param>
    public void AddThinkingItem(string message, string status = "active")
    {
        _response.Thinking.Add(new ThinkingItem 
        { 
            Message = message, 
            Status = status,
            Timestamp = DateTime.UtcNow.ToString("o")
        });
    }
    
    /// <summary>
    /// Updates the last thinking item's status
    /// </summary>
    /// <param name="status">"active" or "complete"</param>
    public void UpdateLastThinkingStatus(string status)
    {
        if (_response.Thinking.Count > 0)
        {
            _response.Thinking[^1].Status = status;
        }
    }
    
    /// <summary>
    /// Adds a text block to the content
    /// </summary>
    /// <param name="text">The text content</param>
    public void AddText(string text)
    {
        _response.Content.Add(new TextBlock { Value = text });
    }
    
    /// <summary>
    /// Adds a component block to the content
    /// </summary>
    /// <param name="componentType">Type of component (weather, chart, table, etc.)</param>
    /// <param name="props">Component properties as an object</param>
    public void AddComponent(string componentType, object props)
    {
        var propsJson = JsonSerializer.SerializeToElement(props, _jsonOptions);
        _response.Content.Add(new ComponentBlock 
        { 
            ComponentType = componentType,
            Props = propsJson
        });
    }
    
    /// <summary>
    /// Adds a component block with actions (for interactive components like forms, confirmations)
    /// Actions are merged into props since frontend passes all props to components
    /// </summary>
    /// <param name="componentType">Type of component</param>
    /// <param name="props">Component properties</param>
    /// <param name="actions">Actions that can be triggered by user interaction</param>
    public void AddComponentWithActions(string componentType, object props, ComponentActions actions)
    {
        // Merge actions into props for frontend compatibility
        var propsWithActions = new Dictionary<string, object?>
        {
            ["props"] = props,
            ["actions"] = actions
        };
        var propsJson = JsonSerializer.SerializeToElement(propsWithActions, _jsonOptions);
        _response.Content.Add(new ComponentBlock 
        { 
            ComponentType = componentType,
            Props = propsJson
        });
    }
    
    /// <summary>
    /// Adds a confirmation dialog with embedded actions (stateless pattern)
    /// </summary>
    public void AddConfirmation(
        string title,
        string message,
        object data,
        ComponentAction onConfirmAction,
        string confirmText = "Confirm",
        string cancelText = "Cancel",
        string variant = "info")
    {
        var props = new
        {
            title,
            message,
            confirmText,
            cancelText,
            variant,
            data
        };
        
        AddComponentWithActions("confirmation", props, new ComponentActions
        {
            OnConfirm = onConfirmAction,
            OnCancel = new DismissAction { Message = "Action cancelled" }
        });
    }
    
    /// <summary>
    /// Adds a form with submit action (stateless pattern)
    /// </summary>
    public void AddForm(
        string title,
        string description,
        object[] fields,
        ComponentAction onSubmitAction,
        string submitText = "Submit")
    {
        var props = new
        {
            title,
            description,
            fields,
            submitText
        };
        
        AddComponentWithActions("form", props, new ComponentActions
        {
            OnSubmit = onSubmitAction,
            OnCancel = new DismissAction()
        });
    }
    
    /// <summary>
    /// Adds a card component for displaying a single record/entity
    /// </summary>
    public void AddCard(string title, object data, string? description = null, ComponentAction? onClick = null)
    {
        var props = new
        {
            title,
            description,
            data
        };
        
        if (onClick != null)
        {
            AddComponentWithActions("card", props, new ComponentActions { OnClick = onClick });
        }
        else
        {
            AddComponent("card", props);
        }
    }
    
    /// <summary>
    /// Adds a list component for displaying collections of items
    /// </summary>
    public void AddList(
        object items, 
        string layout = "grid", 
        string? title = null,
        ComponentAction? onItemClick = null)
    {
        var props = new
        {
            title,
            items,
            layout // grid, list, compact
        };
        
        if (onItemClick != null)
        {
            AddComponentWithActions("list", props, new ComponentActions { OnClick = onItemClick });
        }
        else
        {
            AddComponent("list", props);
        }
    }
    
    /// <summary>
    /// Adds a table component for displaying tabular data
    /// </summary>
    public void AddTable(
        object[] columns,
        object rows,
        string? title = null,
        bool sortable = true,
        bool filterable = false,
        ComponentAction? onRowClick = null)
    {
        var props = new
        {
            title,
            columns,
            rows,
            sortable,
            filterable
        };
        
        if (onRowClick != null)
        {
            AddComponentWithActions("table", props, new ComponentActions { OnRowClick = onRowClick });
        }
        else
        {
            AddComponent("table", props);
        }
    }
    
    /// <summary>
    /// Adds a chart component for data visualization
    /// </summary>
    public void AddChart(
        string chartType,
        object data,
        string? title = null,
        string? xAxis = null,
        string? yAxis = null)
    {
        var props = new
        {
            type = chartType, // line, bar, pie, area
            title,
            data,
            xAxis,
            yAxis
        };
        
        AddComponent("chart", props);
    }
    
    /// <summary>
    /// Adds metadata to the response
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    public void AddMetadata(string key, object value)
    {
        _response.Metadata[key] = value;
    }
    
    /// <summary>
    /// Builds the complete JSON response
    /// </summary>
    /// <returns>JSON string of the complete response</returns>
    public string Build()
    {
        // Add final metadata
        _response.Metadata["timestamp"] = DateTime.UtcNow;
        _response.Metadata["version"] = "1.0";
        
        return JsonSerializer.Serialize(_response, _jsonOptions);
    }
    
    /// <summary>
    /// Builds a partial JSON response for streaming.
    /// Useful for sending intermediate states during LLM generation.
    /// </summary>
    /// <returns>JSON string of the current state</returns>
    public string BuildPartial()
    {
        return JsonSerializer.Serialize(_response, _jsonOptions);
    }
    
    /// <summary>
    /// Gets the current response object (for advanced scenarios)
    /// </summary>
    public GenerativeUIResponse GetResponse() => _response;
}

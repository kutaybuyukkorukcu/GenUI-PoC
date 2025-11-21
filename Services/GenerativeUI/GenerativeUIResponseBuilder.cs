using System.Text.Json;
using FogData.Models.GenerativeUI;

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
            Timestamp = DateTime.UtcNow
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

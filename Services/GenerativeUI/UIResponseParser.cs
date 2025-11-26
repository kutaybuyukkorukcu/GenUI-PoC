using System.Text.Json;
using System.Text.RegularExpressions;

namespace FogData.Services.GenerativeUI;

/// <summary>
/// Parses LLM responses and extracts structured UI components.
/// Handles the <genui>...</genui> wrapper format.
/// </summary>
public class UIResponseParser
{
    private readonly ILogger<UIResponseParser> _logger;
    private static readonly Regex GenUITagRegex = new(@"<genui>(.*?)</genui>", RegexOptions.Singleline | RegexOptions.Compiled);
    
    public UIResponseParser(ILogger<UIResponseParser> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Parses LLM response text and extracts the GenerativeUI response
    /// </summary>
    public GenerativeUIResponse? Parse(string llmResponse)
    {
        if (string.IsNullOrWhiteSpace(llmResponse))
        {
            _logger.LogWarning("Empty LLM response received");
            return null;
        }
        
        try
        {
            // Try to extract content from <genui> tags
            var match = GenUITagRegex.Match(llmResponse);
            
            string jsonContent;
            if (match.Success)
            {
                jsonContent = match.Groups[1].Value.Trim();
                _logger.LogDebug("Extracted genui content: {Length} chars", jsonContent.Length);
            }
            else
            {
                // Fallback: try to parse the whole response as JSON
                jsonContent = llmResponse.Trim();
                
                // Remove markdown code blocks if present
                if (jsonContent.StartsWith("```json"))
                {
                    jsonContent = jsonContent.Substring(7);
                }
                if (jsonContent.StartsWith("```"))
                {
                    jsonContent = jsonContent.Substring(3);
                }
                if (jsonContent.EndsWith("```"))
                {
                    jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
                }
                jsonContent = jsonContent.Trim();
            }
            
            // Parse the JSON
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var response = JsonSerializer.Deserialize<GenerativeUIResponse>(jsonContent, options);
            
            if (response != null)
            {
                _logger.LogInformation("Successfully parsed UI response with {ContentCount} content blocks", 
                    response.Content?.Count ?? 0);
            }
            
            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response as JSON");
            
            // Fallback: wrap plain text as a simple text response
            return CreateFallbackResponse(llmResponse);
        }
    }
    
    /// <summary>
    /// Creates a fallback response when parsing fails - wraps plain text
    /// </summary>
    private GenerativeUIResponse CreateFallbackResponse(string text)
    {
        // Clean up the text - remove any partial tags
        var cleanText = GenUITagRegex.Replace(text, "").Trim();
        
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            cleanText = "I received your request but couldn't format a proper response.";
        }
        
        var metadata = new ResponseMetadata();
        metadata["fallback"] = true;
        
        return new GenerativeUIResponse
        {
            Thinking = new List<ThinkingItem>
            {
                new() { Message = "Processing response...", Status = "complete" }
            },
            Content = new List<ContentBlock>
            {
                new TextBlock { Value = cleanText }
            },
            Metadata = metadata
        };
    }
    
    /// <summary>
    /// Streams partial responses as they come in from the LLM
    /// </summary>
    public IEnumerable<GenerativeUIResponse> ParseStreaming(IEnumerable<string> chunks)
    {
        var buffer = new System.Text.StringBuilder();
        var lastYieldedContent = "";
        
        foreach (var chunk in chunks)
        {
            buffer.Append(chunk);
            var currentText = buffer.ToString();
            
            // Try to extract partial content
            var match = GenUITagRegex.Match(currentText);
            if (match.Success)
            {
                var jsonContent = match.Groups[1].Value;
                
                // Only yield if we have new content
                if (jsonContent != lastYieldedContent && IsValidPartialJson(jsonContent))
                {
                    var partial = TryParsePartial(jsonContent);
                    if (partial != null)
                    {
                        lastYieldedContent = jsonContent;
                        yield return partial;
                    }
                }
            }
        }
        
        // Final parse of complete response
        var finalResponse = Parse(buffer.ToString());
        if (finalResponse != null)
        {
            yield return finalResponse;
        }
    }
    
    /// <summary>
    /// Checks if a string might be valid partial JSON (for streaming)
    /// </summary>
    private bool IsValidPartialJson(string json)
    {
        // Basic check - starts with { and has some content
        return json.TrimStart().StartsWith("{") && json.Length > 10;
    }
    
    /// <summary>
    /// Attempts to parse partial JSON during streaming
    /// </summary>
    private GenerativeUIResponse? TryParsePartial(string json)
    {
        try
        {
            // Try to fix incomplete JSON by closing brackets
            var fixedJson = TryFixIncompleteJson(json);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<GenerativeUIResponse>(fixedJson, options);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Attempts to fix incomplete JSON by adding missing closing brackets
    /// </summary>
    private string TryFixIncompleteJson(string json)
    {
        var openBraces = json.Count(c => c == '{');
        var closeBraces = json.Count(c => c == '}');
        var openBrackets = json.Count(c => c == '[');
        var closeBrackets = json.Count(c => c == ']');
        
        var result = json;
        
        // Add missing brackets
        for (int i = 0; i < openBrackets - closeBrackets; i++)
            result += "]";
        for (int i = 0; i < openBraces - closeBraces; i++)
            result += "}";
        
        return result;
    }
}

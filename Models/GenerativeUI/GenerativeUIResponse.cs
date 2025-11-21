namespace FogData.Models.GenerativeUI;

/// <summary>
/// Complete Generative UI response structure in JSON DSL format.
/// This response format allows mixing text, components, and thinking states
/// in a single unified structure.
/// </summary>
public class GenerativeUIResponse
{
    /// <summary>
    /// List of thinking items showing the AI's reasoning process.
    /// Can be updated progressively during streaming.
    /// </summary>
    public List<ThinkingItem> Thinking { get; set; } = new();
    
    /// <summary>
    /// List of content blocks (text and components) that make up the response.
    /// This allows freely mixing text explanations with UI components.
    /// </summary>
    public List<ContentBlock> Content { get; set; } = new();
    
    /// <summary>
    /// Optional metadata about the response
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

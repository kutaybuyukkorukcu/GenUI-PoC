namespace FogData.Models.GenerativeUI;

/// <summary>
/// Represents a thinking state item that shows the AI's reasoning process.
/// </summary>
public class ThinkingItem
{
    /// <summary>
    /// Status of the thinking item: "active" (in progress) or "complete" (finished)
    /// </summary>
    public string Status { get; set; } = "active";
    
    /// <summary>
    /// The message describing this step in the reasoning process
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional timestamp when this thinking item was created
    /// </summary>
    public DateTime? Timestamp { get; set; }
}

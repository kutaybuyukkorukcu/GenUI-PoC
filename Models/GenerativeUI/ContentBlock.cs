using System.Text.Json;
using System.Text.Json.Serialization;

namespace FogData.Models.GenerativeUI;

/// <summary>
/// Base class for content blocks in a GenerativeUI response.
/// Content can be either text or a component.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextBlock), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ComponentBlock), typeDiscriminator: "component")]
public abstract class ContentBlock
{
    /// <summary>
    /// Type of content: "text" or "component"
    /// </summary>
    public abstract string Type { get; }
}

/// <summary>
/// A text content block containing plain text or markdown
/// </summary>
public class TextBlock : ContentBlock
{
    public override string Type => "text";
    
    /// <summary>
    /// The text content
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// A component content block that specifies a UI component to render
/// </summary>
public class ComponentBlock : ContentBlock
{
    public override string Type => "component";
    
    /// <summary>
    /// Type of component to render: "weather", "chart", "table", etc.
    /// </summary>
    public string ComponentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Properties to pass to the component (can be any JSON structure)
    /// </summary>
    public JsonElement Props { get; set; }
}

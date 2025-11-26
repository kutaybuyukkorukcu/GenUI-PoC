namespace FogData.Services.GenerativeUI;

/// <summary>
/// The main response format for Generative UI - aligns with frontend GenerativeUIResponse
/// </summary>
public class GenerativeUIResponse
{
    public List<ThinkingItem> Thinking { get; set; } = new();
    public List<ContentBlock> Content { get; set; } = new();
    public ResponseMetadata? Metadata { get; set; }
}

/// <summary>
/// Represents a thinking/reasoning step shown to the user
/// Aligns with frontend ThinkingItem
/// </summary>
public class ThinkingItem
{
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "active"; // "active" or "complete"
    public string? Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
}

/// <summary>
/// Base content block - aligns with frontend ContentBlock
/// Type should be "text" or "component"
/// </summary>
public class ContentBlock
{
    public string Type { get; set; } = "text"; // "text" or "component"
    public object? Value { get; set; }
}

/// <summary>
/// Text content block - aligns with frontend TextBlock
/// </summary>
public class TextBlock : ContentBlock
{
    public TextBlock()
    {
        Type = "text";
    }
    
    public TextBlock(string text)
    {
        Type = "text";
        Value = text;
    }
    
    public new string Value { get; set; } = string.Empty;
}

/// <summary>
/// Component content block - aligns with frontend ComponentBlock
/// </summary>
public class ComponentBlock : ContentBlock
{
    public ComponentBlock()
    {
        Type = "component";
    }
    
    public ComponentBlock(string componentType, object props)
    {
        Type = "component";
        ComponentType = componentType;
        Props = props;
    }
    
    /// <summary>
    /// The specific component type: "card", "list", "table", "chart", "form", "mini-card-block", "callout"
    /// </summary>
    public string ComponentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Component-specific properties
    /// </summary>
    public object? Props { get; set; }
}

/// <summary>
/// An action that can be performed on a component
/// </summary>
public class ComponentAction
{
    public string Label { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // "submit", "navigate", "api-call", etc.
    public string? Endpoint { get; set; }
    public string? Method { get; set; }
    public object? Payload { get; set; }
    public string? Variant { get; set; } // "primary", "secondary", "danger", etc.
}

/// <summary>
/// Collection of actions for a component (for forms, confirmations)
/// </summary>
public class ComponentActions
{
    public ComponentAction? OnClick { get; set; }
    public ComponentAction? OnSubmit { get; set; }
    public ComponentAction? OnConfirm { get; set; }
    public ComponentAction? OnCancel { get; set; }
    public ComponentAction? OnRowClick { get; set; }
}

/// <summary>
/// Dismiss action for confirmations/modals
/// </summary>
public class DismissAction : ComponentAction
{
    public string Message { get; set; } = "Action cancelled";
    
    public DismissAction()
    {
        Label = "Cancel";
        ActionType = "dismiss";
    }
}

/// <summary>
/// Metadata about the response - flexible dictionary for extensibility
/// Aligns with frontend metadata: { timestamp, version, modelUsed, queryType, error, ...}
/// </summary>
public class ResponseMetadata : Dictionary<string, object>
{
    public string? Timestamp 
    { 
        get => TryGetValue("timestamp", out var v) ? v?.ToString() : null;
        set { if (value != null) this["timestamp"] = value; }
    }
    
    public string? Version
    {
        get => TryGetValue("version", out var v) ? v?.ToString() : null;
        set { if (value != null) this["version"] = value; }
    }
    
    public string? ModelUsed
    {
        get => TryGetValue("modelUsed", out var v) ? v?.ToString() : null;
        set { if (value != null) this["modelUsed"] = value; }
    }
    
    public string? QueryType
    {
        get => TryGetValue("queryType", out var v) ? v?.ToString() : null;
        set { if (value != null) this["queryType"] = value; }
    }
    
    public bool? Error
    {
        get => TryGetValue("error", out var v) && v is bool b ? b : null;
        set { if (value.HasValue) this["error"] = value.Value; }
    }
}

// ============================================
// Component Props - aligned with frontend renderers
// ============================================

/// <summary>
/// Card component props - aligns with CardRendererProps
/// </summary>
public class CardProps
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// List component props - aligns with ListRendererProps
/// </summary>
public class ListProps
{
    public string? Title { get; set; }
    public List<object> Items { get; set; } = new();
    public string Layout { get; set; } = "grid"; // "list", "grid", "compact"
}

/// <summary>
/// Table component props - aligns with TableRendererProps
/// </summary>
public class TableProps
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object>> Rows { get; set; } = new();
}

/// <summary>
/// Chart component props - aligns with ChartRendererProps
/// </summary>
public class ChartProps
{
    public string? Title { get; set; }
    public List<Dictionary<string, object>> ChartData { get; set; } = new();
}

/// <summary>
/// Form field definition - aligns with frontend FormField
/// </summary>
public class FormFieldProps
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // "text", "number", "email", "date", "select", "textarea"
    public string? Placeholder { get; set; }
    public bool? Required { get; set; }
    public List<string>? Options { get; set; } // For select fields
    public object? DefaultValue { get; set; }
}

/// <summary>
/// Form component props - aligns with FormRendererProps
/// </summary>
public class FormProps
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<FormFieldProps> Fields { get; set; } = new();
    public string SubmitText { get; set; } = "Submit";
}

/// <summary>
/// Confirmation dialog props - aligns with ConfirmationDialog
/// </summary>
public class ConfirmationProps
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ConfirmText { get; set; } = "Confirm";
    public string CancelText { get; set; } = "Cancel";
    public string Variant { get; set; } = "info"; // "info", "warning", "danger"
    public object? Data { get; set; }
}

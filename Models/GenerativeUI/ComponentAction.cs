using System.Text.Json.Serialization;

namespace FogData.Models.GenerativeUI;

/// <summary>
/// Represents an action that can be executed by the client SDK.
/// Actions make the API stateless - all information needed to execute is embedded.
/// </summary>
public class ComponentAction
{
    /// <summary>
    /// The API endpoint to call
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// HTTP method: GET, POST, PUT, DELETE
    /// </summary>
    public string Method { get; set; } = "POST";
    
    /// <summary>
    /// The payload to send with the request
    /// </summary>
    public object? Payload { get; set; }
    
    /// <summary>
    /// Optional confirmation message to show before executing
    /// </summary>
    public string? ConfirmMessage { get; set; }
}

/// <summary>
/// A dismiss action that just closes the component (no API call)
/// </summary>
public class DismissAction
{
    public string Type { get; } = "dismiss";
    
    /// <summary>
    /// Optional message to show after dismissing
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// A navigate action that redirects the user
/// </summary>
public class NavigateAction
{
    public string Type { get; } = "navigate";
    
    /// <summary>
    /// The URL or route to navigate to
    /// </summary>
    public string Target { get; set; } = string.Empty;
}

/// <summary>
/// Container for all possible actions on a component.
/// The SDK will execute these when user interacts with the component.
/// </summary>
public class ComponentActions
{
    /// <summary>
    /// Action when user confirms (for confirmation dialogs)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ComponentAction? OnConfirm { get; set; }
    
    /// <summary>
    /// Action when user cancels
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DismissAction? OnCancel { get; set; }
    
    /// <summary>
    /// Action when form is submitted
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ComponentAction? OnSubmit { get; set; }
    
    /// <summary>
    /// Action when a row is clicked (for tables)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ComponentAction? OnRowClick { get; set; }
    
    /// <summary>
    /// Action when user clicks a button/link
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ComponentAction? OnClick { get; set; }
}

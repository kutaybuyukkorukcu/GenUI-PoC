using FogData.Database.Entities;

namespace FogData.Services;

public interface IAgentService
{
    Task<AgentResponse> AnalyzeIntentAsync(string userInput);
    Task<ToolCallResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters);
}

public record AgentResponse
{
    public string Intent { get; init; } = string.Empty;
    public string ToolToCall { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
    public string Reasoning { get; init; } = string.Empty;
}

public record ToolCallResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string? Error { get; init; }
    public string ComponentType { get; init; } = string.Empty; // "weather", "chart", "table", etc.
}
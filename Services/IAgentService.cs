using FogData.Database.Entities;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FogData.Services;

public interface IAgentService
{
    IAsyncEnumerable<StreamingChatUpdate> ProcessUserMessageAsync(string userInput);
}

public record StreamingChatUpdate
{
    public string Type { get; init; } = string.Empty; // "message", "tool-call", "tool-result", "synthesis"
    public string? Content { get; init; }
    public string? ToolName { get; init; }
    public object? ToolArguments { get; init; }
    public object? ToolResult { get; init; }
    public string? ComponentType { get; init; }
}

public record ToolCallResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string? Error { get; init; }
    public string ComponentType { get; init; } = string.Empty; // "weather", "chart", "table", etc.
}
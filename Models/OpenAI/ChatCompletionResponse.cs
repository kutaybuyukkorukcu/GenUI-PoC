using System.Text.Json.Serialization;

namespace GenUI.Models.OpenAI;

/// <summary>
/// OpenAI-compatible chat completion response.
/// Our API returns responses in this standard format.
/// </summary>
public class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"chatcmpl-{Guid.NewGuid():N}";

    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion";

    [JsonPropertyName("created")]
    public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<ChatChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public UsageInfo? Usage { get; set; }

    /// <summary>
    /// GenUI-specific: the structured UI response parsed from the content
    /// </summary>
    [JsonPropertyName("genui")]
    public object? GenUI { get; set; }
}

public class ChatChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; } = 0;

    [JsonPropertyName("message")]
    public ChatMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; } = "stop";
}

public class UsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
    
    /// <summary>
    /// GenUI extension: Estimated cost in USD
    /// </summary>
    [JsonPropertyName("estimated_cost")]
    public CostInfo? EstimatedCost { get; set; }
}

/// <summary>
/// Cost breakdown for the request
/// </summary>
public class CostInfo
{
    [JsonPropertyName("prompt_cost")]
    public decimal PromptCost { get; set; }
    
    [JsonPropertyName("completion_cost")]
    public decimal CompletionCost { get; set; }
    
    [JsonPropertyName("total_cost")]
    public decimal TotalCost { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// Streaming chunk response (SSE format)
/// </summary>
public class ChatCompletionChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = $"chatcmpl-{Guid.NewGuid():N}";

    [JsonPropertyName("object")]
    public string Object { get; set; } = "chat.completion.chunk";

    [JsonPropertyName("created")]
    public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<StreamChoice> Choices { get; set; } = new();
}

public class StreamChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; } = 0;

    [JsonPropertyName("delta")]
    public ChatMessageDelta Delta { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class ChatMessageDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

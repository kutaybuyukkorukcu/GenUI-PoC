using System.Text.Json.Serialization;

namespace GenUI.Models.OpenAI;

/// <summary>
/// Error response in OpenAI format
/// </summary>
public class OpenAIErrorResponse
{
    [JsonPropertyName("error")]
    public OpenAIError Error { get; set; } = new();
}

public class OpenAIError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "invalid_request_error";

    [JsonPropertyName("param")]
    public string? Param { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

/// <summary>
/// Supported LLM providers for BYOK
/// </summary>
public enum LLMProvider
{
    OpenAI,
    AzureOpenAI,
    Anthropic,
    Google
}

/// <summary>
/// Configuration for creating an LLM client from user's key
/// </summary>
public class LLMProviderConfig
{
    public LLMProvider Provider { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    
    // Azure-specific
    public string? Endpoint { get; set; }
    public string? DeploymentName { get; set; }
}

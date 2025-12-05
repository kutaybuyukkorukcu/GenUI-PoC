using Microsoft.SemanticKernel;
using GenUI.Models.OpenAI;

namespace GenUI.Services;

/// <summary>
/// Factory for creating Semantic Kernel instances from user-provided API keys (BYOK).
/// This enables runtime configuration of LLM providers based on request headers.
/// </summary>
public interface IKernelFactory
{
    /// <summary>
    /// Creates a Kernel configured with the specified LLM provider and credentials
    /// </summary>
    Kernel CreateKernel(LLMProviderConfig config);
    
    /// <summary>
    /// Extracts provider configuration from request headers
    /// </summary>
    LLMProviderConfig? ExtractConfig(
        string? llmApiKey,
        string model,
        string? llmProvider = null,
        string? azureEndpoint = null,
        string? azureDeployment = null);
}

/// <summary>
/// Implementation of kernel factory for BYOK model
/// </summary>
public class KernelFactory : IKernelFactory
{
    private readonly ILogger<KernelFactory> _logger;

    public KernelFactory(ILogger<KernelFactory> logger)
    {
        _logger = logger;
    }

    public Kernel CreateKernel(LLMProviderConfig config)
    {
        var builder = Kernel.CreateBuilder();

        switch (config.Provider)
        {
            case LLMProvider.OpenAI:
                _logger.LogInformation("Creating OpenAI kernel with model: {Model}", config.Model);
                builder.AddOpenAIChatCompletion(config.Model, config.ApiKey);
                break;

            case LLMProvider.AzureOpenAI:
                if (string.IsNullOrEmpty(config.Endpoint) || string.IsNullOrEmpty(config.DeploymentName))
                {
                    throw new ArgumentException("Azure OpenAI requires endpoint and deployment name");
                }
                _logger.LogInformation("Creating Azure OpenAI kernel with deployment: {Deployment}", config.DeploymentName);
                builder.AddAzureOpenAIChatCompletion(config.DeploymentName, config.Endpoint, config.ApiKey);
                break;

            case LLMProvider.Anthropic:
                // Note: Semantic Kernel doesn't have native Anthropic support
                // We'd need to use a custom connector or HTTP client
                throw new NotSupportedException("Anthropic support requires additional configuration. Use OpenAI-compatible endpoint.");

            case LLMProvider.Google:
                // Note: Would need Google AI connector
                throw new NotSupportedException("Google AI support requires additional configuration.");

            default:
                throw new ArgumentException($"Unsupported provider: {config.Provider}");
        }

        return builder.Build();
    }

    public LLMProviderConfig? ExtractConfig(
        string? llmApiKey,
        string model,
        string? llmProvider = null,
        string? azureEndpoint = null,
        string? azureDeployment = null)
    {
        if (string.IsNullOrEmpty(llmApiKey))
        {
            return null;
        }

        // Auto-detect provider from API key format if not specified
        var provider = DetectProvider(llmApiKey, llmProvider);

        return new LLMProviderConfig
        {
            Provider = provider,
            ApiKey = llmApiKey,
            Model = model,
            Endpoint = azureEndpoint,
            DeploymentName = azureDeployment ?? model
        };
    }

    private LLMProvider DetectProvider(string apiKey, string? providerHint)
    {
        // If provider explicitly specified
        if (!string.IsNullOrEmpty(providerHint))
        {
            return providerHint.ToLowerInvariant() switch
            {
                "openai" => LLMProvider.OpenAI,
                "azure" or "azureopenai" or "azure-openai" => LLMProvider.AzureOpenAI,
                "anthropic" or "claude" => LLMProvider.Anthropic,
                "google" or "gemini" => LLMProvider.Google,
                _ => LLMProvider.OpenAI
            };
        }

        // Auto-detect from key format
        if (apiKey.StartsWith("sk-"))
        {
            return LLMProvider.OpenAI;
        }
        if (apiKey.StartsWith("sk-ant-"))
        {
            return LLMProvider.Anthropic;
        }
        
        // Default to OpenAI
        return LLMProvider.OpenAI;
    }
}

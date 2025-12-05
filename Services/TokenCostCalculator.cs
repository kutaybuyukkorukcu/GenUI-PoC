using GenUI.Models.OpenAI;

namespace GenUI.Services;

/// <summary>
/// Calculates token costs for various LLM models.
/// Prices are per 1 million tokens (as of December 2024).
/// </summary>
public class TokenCostCalculator
{
    // Pricing per 1M tokens (input, output) in USD
    private static readonly Dictionary<string, (decimal Input, decimal Output)> ModelPricing = new()
    {
        // OpenAI models
        ["gpt-4o"] = (2.50m, 10.00m),
        ["gpt-4o-2024-11-20"] = (2.50m, 10.00m),
        ["gpt-4o-2024-08-06"] = (2.50m, 10.00m),
        ["gpt-4o-mini"] = (0.15m, 0.60m),
        ["gpt-4o-mini-2024-07-18"] = (0.15m, 0.60m),
        ["gpt-4-turbo"] = (10.00m, 30.00m),
        ["gpt-4-turbo-preview"] = (10.00m, 30.00m),
        ["gpt-4"] = (30.00m, 60.00m),
        ["gpt-4-32k"] = (60.00m, 120.00m),
        ["gpt-3.5-turbo"] = (0.50m, 1.50m),
        ["gpt-3.5-turbo-16k"] = (3.00m, 4.00m),
        
        // Azure OpenAI (same as OpenAI for standard deployments)
        ["gpt-4.1-mini"] = (0.15m, 0.60m),  // Common Azure deployment name
        ["gpt-4.1"] = (2.50m, 10.00m),
        
        // o1 models (reasoning)
        ["o1-preview"] = (15.00m, 60.00m),
        ["o1-mini"] = (3.00m, 12.00m),
        
        // Claude models (for future support)
        ["claude-3-5-sonnet-20241022"] = (3.00m, 15.00m),
        ["claude-3-5-haiku-20241022"] = (0.80m, 4.00m),
        ["claude-3-opus-20240229"] = (15.00m, 75.00m),
    };
    
    // Default pricing for unknown models (conservative estimate)
    private static readonly (decimal Input, decimal Output) DefaultPricing = (1.00m, 3.00m);

    /// <summary>
    /// Calculate cost for a given usage
    /// </summary>
    public CostInfo CalculateCost(string model, int promptTokens, int completionTokens)
    {
        var normalizedModel = NormalizeModelName(model);
        var pricing = ModelPricing.GetValueOrDefault(normalizedModel, DefaultPricing);
        
        // Calculate cost (pricing is per 1M tokens)
        var promptCost = (promptTokens / 1_000_000m) * pricing.Input;
        var completionCost = (completionTokens / 1_000_000m) * pricing.Output;
        
        return new CostInfo
        {
            PromptCost = Math.Round(promptCost, 6),
            CompletionCost = Math.Round(completionCost, 6),
            TotalCost = Math.Round(promptCost + completionCost, 6),
            Currency = "USD",
            Model = normalizedModel
        };
    }
    
    /// <summary>
    /// Build complete usage info with cost
    /// </summary>
    public UsageInfo BuildUsageInfo(string model, int promptTokens, int completionTokens)
    {
        return new UsageInfo
        {
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = promptTokens + completionTokens,
            EstimatedCost = CalculateCost(model, promptTokens, completionTokens)
        };
    }
    
    /// <summary>
    /// Normalize model name for pricing lookup
    /// </summary>
    private string NormalizeModelName(string model)
    {
        // Handle Azure deployment names that might differ
        var normalized = model.ToLowerInvariant().Trim();
        
        // Common Azure deployment name mappings
        if (normalized.Contains("gpt-4o-mini") || normalized.Contains("gpt-4.1-mini"))
            return "gpt-4o-mini";
        if (normalized.Contains("gpt-4o") || normalized.Contains("gpt-4.1"))
            return "gpt-4o";
        if (normalized.Contains("gpt-4-turbo"))
            return "gpt-4-turbo";
        if (normalized.Contains("gpt-4-32k"))
            return "gpt-4-32k";
        if (normalized.Contains("gpt-4"))
            return "gpt-4";
        if (normalized.Contains("gpt-3.5-turbo-16k"))
            return "gpt-3.5-turbo-16k";
        if (normalized.Contains("gpt-3.5"))
            return "gpt-3.5-turbo";
            
        return normalized;
    }
    
    /// <summary>
    /// Estimate token count for a string (rough approximation: ~4 chars per token for English)
    /// This is used when we can't get actual token counts from the API
    /// </summary>
    public static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        // Rough estimation: ~4 characters per token for English text
        // This is a simplification - actual tokenization varies by model
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}

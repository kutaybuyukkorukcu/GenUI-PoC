using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FogData.Services.GenerativeUI;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace FogData.Services;

/// <summary>
/// Proxy-style Generative UI Service that transforms LLM responses into structured UI.
/// 
/// This service acts as a middleware:
/// 1. Receives user message
/// 2. Injects system prompt that teaches LLM to output UI components
/// 3. Calls the LLM (OpenAI, Anthropic, Gemini)
/// 4. Parses response and extracts structured UI
/// 5. Returns UI JSON for frontend rendering
/// 
/// No database dependency - works with any LLM-powered application.
/// </summary>
public class GenerativeUIProxyService : IGenerativeUIService
{
    private readonly Kernel _kernel;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GenerativeUIProxyService> _logger;
    private readonly UIResponseParser _responseParser;
    
    /// <summary>
    /// Constructor with full DI support.
    /// Kernel can be configured externally (e.g., with MCP tools) and injected.
    /// </summary>
    public GenerativeUIProxyService(
        Kernel kernel,
        UIResponseParser responseParser,
        IConfiguration configuration,
        ILogger<GenerativeUIProxyService> logger)
    {
        _kernel = kernel;
        _responseParser = responseParser;
        _configuration = configuration;
        _logger = logger;
        
        _logger.LogInformation("GenerativeUIProxyService initialized with provider: {Provider}", 
            configuration["SemanticKernel:Provider"]);
    }

    
    /// <summary>
    /// Main entry point - processes user message and returns UI response stream
    /// </summary>
    public async IAsyncEnumerable<string> ProcessUserMessageAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing message: {Message}", userMessage);
        
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        
        // Create chat history with our system prompt
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(UIComponentPrompts.SystemPrompt);
        
        // Add user message
        chatHistory.AddUserMessage(userMessage);
        
        // Configure execution settings
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.7,
            MaxTokens = 4096,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };
        
        // Stream initial thinking state
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var thinkingResponse = new GenerativeUIResponse
        {
            Thinking = new List<ThinkingItem>
            {
                new() { Message = "Processing your request...", Status = "active" }
            },
            Content = new List<ContentBlock>()
        };
        yield return JsonSerializer.Serialize(thinkingResponse, jsonOptions);
        
        string finalResponse;
        try
        {
            // Get streaming response from LLM
            var fullResponse = new System.Text.StringBuilder();
            
            await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                executionSettings,
                _kernel,
                cancellationToken))
            {
                if (chunk.Content != null)
                {
                    fullResponse.Append(chunk.Content);
                }
            }
            
            // Parse the complete response
            var parsedResponse = _responseParser.Parse(fullResponse.ToString());
            
            if (parsedResponse != null)
            {
                // Add metadata
                parsedResponse.Metadata ??= new ResponseMetadata();
                parsedResponse.Metadata["modelUsed"] = _configuration["SemanticKernel:Provider"] ?? "unknown";
                parsedResponse.Metadata["timestamp"] = DateTime.UtcNow;
                
                finalResponse = JsonSerializer.Serialize(parsedResponse, jsonOptions);
            }
            else
            {
                // Fallback if parsing failed
                var fallback = new GenerativeUIResponse
                {
                    Thinking = new List<ThinkingItem>
                    {
                        new() { Message = "Processed your request", Status = "complete" }
                    },
                    Content = new List<ContentBlock>
                    {
                        new TextBlock { Value = fullResponse.ToString() }
                    }
                };
                
                finalResponse = JsonSerializer.Serialize(fallback, jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            
            var errorMetadata = new ResponseMetadata();
            errorMetadata["error"] = true;
            
            var errorResponse = new GenerativeUIResponse
            {
                Thinking = new List<ThinkingItem>
                {
                    new() { Message = "Error occurred", Status = "complete" }
                },
                Content = new List<ContentBlock>
                {
                    new TextBlock { Value = $"I encountered an error: {ex.Message}" }
                },
                Metadata = errorMetadata
            };
            
            finalResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
        }
        
        yield return finalResponse;
    }
    
    /// <summary>
    /// Overload for interface compatibility
    /// </summary>
    public async IAsyncEnumerable<string> ProcessUserMessageAsync(string userMessage)
    {
        await foreach (var response in ProcessUserMessageAsync(userMessage, CancellationToken.None))
        {
            yield return response;
        }
    }
}

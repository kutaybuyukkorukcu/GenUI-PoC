using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using GenUI.Models.OpenAI;
using GenUI.Services;
using FogData.Services.GenerativeUI;
using System.Text;
using System.Text.Json;

namespace GenUI.Controllers;

/// <summary>
/// OpenAI-compatible chat completions endpoint.
/// 
/// This is the main API endpoint for GenUI - users call this instead of OpenAI directly.
/// We inject our system prompts to transform LLM output into structured UI.
/// 
/// BYOK Model:
/// - User provides their own LLM API key via X-LLM-API-Key header
/// - We add the GenUI layer (system prompts, parsing, validation)
/// - User pays us for the GenUI value-add, manages their own LLM costs
/// </summary>
[ApiController]
[Route("v1")]
public class ChatCompletionsController : ControllerBase
{
    private readonly IKernelFactory _kernelFactory;
    private readonly UIResponseParser _responseParser;
    private readonly TokenCostCalculator _costCalculator;
    private readonly ILogger<ChatCompletionsController> _logger;
    private readonly IConfiguration _configuration;

    public ChatCompletionsController(
        IKernelFactory kernelFactory,
        UIResponseParser responseParser,
        TokenCostCalculator costCalculator,
        IConfiguration configuration,
        ILogger<ChatCompletionsController> logger)
    {
        _kernelFactory = kernelFactory;
        _responseParser = responseParser;
        _costCalculator = costCalculator;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// POST /v1/chat/completions
    /// OpenAI-compatible endpoint that returns GenUI-enhanced responses
    /// </summary>
    [HttpPost("chat/completions")]
    public async Task<IActionResult> ChatCompletions(
        [FromHeader(Name = "Authorization")] string? authHeader,
        [FromHeader(Name = "X-LLM-API-Key")] string? llmApiKey,
        [FromHeader(Name = "X-LLM-Provider")] string? llmProvider,
        [FromHeader(Name = "X-Azure-Endpoint")] string? azureEndpoint,
        [FromHeader(Name = "X-Azure-Deployment")] string? azureDeployment,
        [FromBody] ChatCompletionRequest request)
    {
        try
        {
            // TODO: Validate platform API key from Authorization header
            // For now, we'll skip platform auth during development
            
            // BYOK: Try to get config from headers first
            var llmConfig = _kernelFactory.ExtractConfig(
                llmApiKey,
                request.Model,
                llmProvider,
                azureEndpoint,
                azureDeployment);

            // Fallback: Use .env/appsettings config for local development
            if (llmConfig == null)
            {
                llmConfig = GetFallbackConfig(request.Model);
            }

            if (llmConfig == null)
            {
                return BadRequest(new OpenAIErrorResponse
                {
                    Error = new OpenAIError
                    {
                        Message = "Missing LLM API key. Provide your API key in the X-LLM-API-Key header.",
                        Type = "invalid_request_error",
                        Code = "missing_api_key"
                    }
                });
            }

            // Create kernel from user's credentials
            Kernel kernel;
            try
            {
                kernel = _kernelFactory.CreateKernel(llmConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create kernel");
                return BadRequest(new OpenAIErrorResponse
                {
                    Error = new OpenAIError
                    {
                        Message = $"Failed to initialize LLM provider: {ex.Message}",
                        Type = "invalid_request_error",
                        Code = "provider_error"
                    }
                });
            }

            // Handle streaming vs non-streaming
            if (request.Stream)
            {
                return await StreamResponse(kernel, request, llmConfig);
            }
            else
            {
                return await NonStreamResponse(kernel, request, llmConfig);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat completion");
            return StatusCode(500, new OpenAIErrorResponse
            {
                Error = new OpenAIError
                {
                    Message = "Internal server error",
                    Type = "server_error",
                    Code = "internal_error"
                }
            });
        }
    }

    private async Task<IActionResult> NonStreamResponse(
        Kernel kernel, 
        ChatCompletionRequest request,
        LLMProviderConfig config)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        
        // Build chat history with GenUI system prompt injected
        var chatHistory = BuildChatHistory(request.Messages);
        
        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = request.Temperature ?? 0.7,
            MaxTokens = request.MaxTokens ?? 4096,
            TopP = request.TopP ?? 1.0
        };

        // Get completion from LLM
        var result = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel);
        var content = result.Content ?? "";

        // Parse the GenUI response
        var genUIResponse = _responseParser.Parse(content);

        // Build OpenAI-compatible response
        var response = new ChatCompletionResponse
        {
            Model = request.Model,
            Choices = new List<ChatChoice>
            {
                new ChatChoice
                {
                    Index = 0,
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = content
                    },
                    FinishReason = "stop"
                }
            },
            GenUI = genUIResponse // Our value-add: parsed UI structure
        };

        return Ok(response);
    }

    private async Task<IActionResult> StreamResponse(
        Kernel kernel,
        ChatCompletionRequest request,
        LLMProviderConfig config)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = BuildChatHistory(request.Messages);
        
        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = request.Temperature ?? 0.7,
            MaxTokens = request.MaxTokens ?? 4096,
            TopP = request.TopP ?? 1.0
        };

        var fullContent = new StringBuilder();
        var completionId = $"chatcmpl-{Guid.NewGuid():N}";
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        try
        {
            // Stream the response
            await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
                chatHistory, settings, kernel))
            {
                if (chunk.Content != null)
                {
                    fullContent.Append(chunk.Content);

                    var streamChunk = new ChatCompletionChunk
                    {
                        Id = completionId,
                        Created = created,
                        Model = request.Model,
                        Choices = new List<StreamChoice>
                        {
                            new StreamChoice
                            {
                                Index = 0,
                                Delta = new ChatMessageDelta { Content = chunk.Content }
                            }
                        }
                    };

                    await WriteSSEData(streamChunk);
                }
            }

            // Send final chunk with finish_reason
            var finalChunk = new ChatCompletionChunk
            {
                Id = completionId,
                Created = created,
                Model = request.Model,
                Choices = new List<StreamChoice>
                {
                    new StreamChoice
                    {
                        Index = 0,
                        Delta = new ChatMessageDelta(),
                        FinishReason = "stop"
                    }
                }
            };
            await WriteSSEData(finalChunk);

            // Send parsed GenUI response as a special event
            var genUIResponse = _responseParser.Parse(fullContent.ToString());
            if (genUIResponse != null)
            {
                await WriteSSEEvent("genui", genUIResponse);
            }
            
            // Calculate and send usage/cost info
            var promptText = string.Join(" ", request.Messages.Select(m => m.Content));
            var promptTokens = TokenCostCalculator.EstimateTokenCount(promptText + UIComponentPrompts.SystemPrompt);
            var completionTokens = TokenCostCalculator.EstimateTokenCount(fullContent.ToString());
            var usage = _costCalculator.BuildUsageInfo(config.Model, promptTokens, completionTokens);
            
            await WriteSSEEvent("usage", usage);
            _logger.LogInformation("Request completed: {PromptTokens} prompt + {CompletionTokens} completion = ${TotalCost:F6}", 
                usage.PromptTokens, usage.CompletionTokens, usage.EstimatedCost?.TotalCost ?? 0);

            // Send [DONE] marker
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming");
            var errorEvent = new { error = ex.Message };
            await WriteSSEEvent("error", errorEvent);
        }

        return new EmptyResult();
    }

    private ChatHistory BuildChatHistory(List<ChatMessage> messages)
    {
        var history = new ChatHistory();
        
        // Always inject our GenUI system prompt first
        history.AddSystemMessage(UIComponentPrompts.SystemPrompt);

        // Add user's messages
        foreach (var msg in messages)
        {
            switch (msg.Role.ToLowerInvariant())
            {
                case "system":
                    // Append to our system prompt instead of replacing
                    history.AddSystemMessage(msg.Content);
                    break;
                case "user":
                    history.AddUserMessage(msg.Content);
                    break;
                case "assistant":
                    history.AddAssistantMessage(msg.Content);
                    break;
            }
        }

        return history;
    }

    /// <summary>
    /// Get fallback LLM config from environment/appsettings for local development
    /// </summary>
    private LLMProviderConfig? GetFallbackConfig(string model)
    {
        var provider = _configuration["SEMANTIC_KERNEL_PROVIDER"] 
            ?? Environment.GetEnvironmentVariable("SEMANTIC_KERNEL_PROVIDER");

        if (string.IsNullOrEmpty(provider))
        {
            _logger.LogWarning("No fallback LLM provider configured");
            return null;
        }

        return provider.ToLowerInvariant() switch
        {
            "openai" => new LLMProviderConfig
            {
                Provider = LLMProvider.OpenAI,
                ApiKey = _configuration["OPENAI_API_KEY"] 
                    ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "",
                Model = model
            },
            "azureopenai" => new LLMProviderConfig
            {
                Provider = LLMProvider.AzureOpenAI,
                ApiKey = _configuration["AZURE_OPENAI_API_KEY"] 
                    ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "",
                Endpoint = _configuration["AZURE_OPENAI_ENDPOINT"] 
                    ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
                DeploymentName = _configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] 
                    ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? model,
                Model = model
            },
            _ => null
        };
    }

    private async Task WriteSSEData(object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }

    private async Task WriteSSEEvent(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n");
        await Response.Body.FlushAsync();
    }
}

using Microsoft.AspNetCore.Mvc;
using FogData.Services;
using System.Text.Json;
using System.Text;

namespace FogData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost("chat")]
    public async Task ChatStream([FromBody] ChatRequest request, ILogger<AgentController> logger)
    {
        logger.LogInformation("ChatStream endpoint called with message: {Message}", request.Message);
        
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            logger.LogWarning("Empty message received");
            Response.StatusCode = 400;
            await Response.WriteAsync("Message is required");
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        logger.LogInformation("SSE headers set, starting to process message");

        try
        {
            var updateCount = 0;
            // Stream updates from the agent service
            await foreach (var update in _agentService.ProcessUserMessageAsync(request.Message))
            {
                updateCount++;
                logger.LogInformation("Received update #{Count}: Type={Type}, Content={Content}, ToolName={ToolName}", 
                    updateCount, update.Type, update.Content?.Substring(0, Math.Min(50, update.Content?.Length ?? 0)), update.ToolName);
                
                // Map StreamingChatUpdate type to SSE event type
                var eventType = update.Type switch
                {
                    "tool-result" => "tool-result",
                    "synthesis" => "message",
                    "message" => "message",
                    _ => "message"
                };

                // Prepare data based on update type
                object data = update.Type switch
                {
                    "tool-result" => update.ToolResult!,
                    "synthesis" => new { role = "assistant", content = update.Content },
                    "message" => new { role = "assistant", content = update.Content },
                    _ => new { content = update.Content }
                };

                logger.LogInformation("Sending SSE event: {EventType}", eventType);
                await SendSSEEvent(eventType, data, logger);
            }

            logger.LogInformation("Stream completed. Total updates sent: {Count}", updateCount);
            // Send completion event
            await SendSSEEvent("done", new { success = true }, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during chat stream processing");
            await SendSSEEvent("error", new { message = ex.Message, stackTrace = ex.StackTrace }, logger);
        }
    }

    private async Task SendSSEEvent(string eventType, object data, ILogger<AgentController> logger)
    {
        var json = JsonSerializer.Serialize(data);
        var message = $"event: {eventType}\ndata: {json}\n\n";
        
        logger.LogDebug("Sending SSE event: {EventType}, Data length: {Length}", eventType, json.Length);
        
        await Response.WriteAsync(message);
        await Response.Body.FlushAsync();
        
        logger.LogDebug("SSE event sent and flushed");
    }
}

public record ChatRequest
{
    public string Message { get; init; } = string.Empty;
}
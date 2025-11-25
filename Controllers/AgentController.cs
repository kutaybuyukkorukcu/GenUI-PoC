using Microsoft.AspNetCore.Mvc;
using FogData.Services;
using System.Text.Json;

namespace FogData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IGenerativeUIService _generativeUIService;

    public AgentController(IGenerativeUIService generativeUIService)
    {
        _generativeUIService = generativeUIService;
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
            await foreach (var jsonResponse in _generativeUIService.ProcessUserMessageAsync(request.Message))
            {
                logger.LogDebug("Sending GenerativeUI response, length: {Length}", jsonResponse.Length);
                await SendSSEEvent("generative-ui", new { response = jsonResponse }, logger);
            }
            
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
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
    public async Task ChatStream([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Message is required");
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            // Stream updates from the agent service
            await foreach (var update in _agentService.ProcessUserMessageAsync(request.Message))
            {
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

                await SendSSEEvent(eventType, data);
            }

            // Send completion event
            await SendSSEEvent("done", new { success = true });
        }
        catch (Exception ex)
        {
            await SendSSEEvent("error", new { message = ex.Message });
        }
    }

    private async Task SendSSEEvent(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var message = $"event: {eventType}\ndata: {json}\n\n";
        await Response.WriteAsync(message);
        await Response.Body.FlushAsync();
    }
}

public record ChatRequest
{
    public string Message { get; init; } = string.Empty;
}
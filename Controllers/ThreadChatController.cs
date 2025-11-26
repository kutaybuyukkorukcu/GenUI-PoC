using Microsoft.AspNetCore.Mvc;
using FogData.Services;
using System.Text.Json;
using System.Collections.Concurrent;

namespace FogData.Controllers;

/// <summary>
/// Thread-based chat API following the Thesys pattern.
/// 
/// Endpoints:
/// - POST /api/thread - Create a new conversation thread
/// - POST /api/chat/thread - Send a message and get UI response
/// - GET /api/thread/{threadId} - Get thread history
/// </summary>
[ApiController]
[Route("api")]
public class ThreadChatController : ControllerBase
{
    private readonly IGenerativeUIService _generativeUIService;
    private readonly ILogger<ThreadChatController> _logger;
    
    // In-memory thread storage (use Redis/DB for production)
    private static readonly ConcurrentDictionary<string, ChatThread> _threads = new();

    public ThreadChatController(
        IGenerativeUIService generativeUIService,
        ILogger<ThreadChatController> logger)
    {
        _generativeUIService = generativeUIService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new conversation thread
    /// POST /api/thread
    /// </summary>
    [HttpPost("thread")]
    public IActionResult CreateThread([FromBody] CreateThreadRequest? request)
    {
        var thread = new ChatThread
        {
            ThreadId = Guid.NewGuid().ToString(),
            Title = request?.Title ?? "New Conversation",
            CreatedAt = DateTime.UtcNow,
            Messages = new List<ThreadChatMessage>()
        };
        
        _threads[thread.ThreadId] = thread;
        
        _logger.LogInformation("Created thread: {ThreadId}", thread.ThreadId);
        
        return Ok(new CreateThreadResponse
        {
            ThreadId = thread.ThreadId,
            Title = thread.Title,
            CreatedAt = thread.CreatedAt
        });
    }

    /// <summary>
    /// Get thread details and history
    /// GET /api/thread/{threadId}
    /// </summary>
    [HttpGet("thread/{threadId}")]
    public IActionResult GetThread(string threadId)
    {
        if (!_threads.TryGetValue(threadId, out var thread))
        {
            return NotFound(new { error = "Thread not found" });
        }
        
        return Ok(thread);
    }

    /// <summary>
    /// Send a chat message and stream UI response
    /// POST /api/chat/thread
    /// </summary>
    [HttpPost("chat/thread")]
    public async Task ThreadChat([FromBody] ThreadChatRequest request)
    {
        _logger.LogInformation("Chat request - ThreadId: {ThreadId}, Message: {Message}", 
            request.ThreadId, request.Message);
        
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Message is required");
            return;
        }
        
        // Get or create thread
        if (!string.IsNullOrEmpty(request.ThreadId) && !_threads.ContainsKey(request.ThreadId))
        {
            // Auto-create thread if it doesn't exist
            _threads[request.ThreadId] = new ChatThread
            {
                ThreadId = request.ThreadId,
                Title = request.Message.Length > 50 ? request.Message.Substring(0, 50) + "..." : request.Message,
                CreatedAt = DateTime.UtcNow,
                Messages = new List<ThreadChatMessage>()
            };
        }
        
        // Set up SSE response
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering
        
        try
        {
            // Add user message to thread
            if (!string.IsNullOrEmpty(request.ThreadId) && _threads.TryGetValue(request.ThreadId, out var thread))
            {
                thread.Messages.Add(new ThreadChatMessage
                {
                    Role = "user",
                    Content = request.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            
            // Stream response from GenerativeUI service
            await foreach (var jsonResponse in _generativeUIService.ProcessUserMessageAsync(request.Message))
            {
                // Send as SSE event
                await Response.WriteAsync($"data: {jsonResponse}\n\n");
                await Response.Body.FlushAsync();
                
                // Store assistant response in thread (only final response)
                if (!string.IsNullOrEmpty(request.ThreadId) && 
                    _threads.TryGetValue(request.ThreadId, out var t))
                {
                    // Update or add assistant message
                    var existingAssistant = t.Messages.LastOrDefault(m => m.Role == "assistant");
                    if (existingAssistant != null && 
                        (DateTime.UtcNow - existingAssistant.Timestamp).TotalSeconds < 5)
                    {
                        existingAssistant.Content = jsonResponse;
                    }
                    else
                    {
                        t.Messages.Add(new ThreadChatMessage
                        {
                            Role = "assistant",
                            Content = jsonResponse,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            }
            
            // Send done event
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during chat processing");
            
            var errorJson = JsonSerializer.Serialize(new
            {
                thinking = new[] { new { message = "Error occurred", status = "complete" } },
                content = new[] { new { type = "text", value = $"Error: {ex.Message}" } },
                metadata = new { error = true }
            });
            
            await Response.WriteAsync($"data: {errorJson}\n\n");
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
    }

    /// <summary>
    /// Delete a thread
    /// DELETE /api/thread/{threadId}
    /// </summary>
    [HttpDelete("thread/{threadId}")]
    public IActionResult DeleteThread(string threadId)
    {
        if (_threads.TryRemove(threadId, out _))
        {
            return Ok(new { success = true, message = "Thread deleted" });
        }
        
        return NotFound(new { error = "Thread not found" });
    }

    /// <summary>
    /// List all threads (for development/debugging)
    /// GET /api/threads
    /// </summary>
    [HttpGet("threads")]
    public IActionResult ListThreads()
    {
        var threads = _threads.Values
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.ThreadId,
                t.Title,
                t.CreatedAt,
                MessageCount = t.Messages.Count
            })
            .ToList();
        
        return Ok(threads);
    }
}

#region Request/Response Models

public record CreateThreadRequest
{
    public string? Title { get; init; }
}

public record CreateThreadResponse
{
    public string ThreadId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record ThreadChatRequest
{
    public string? ThreadId { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class ChatThread
{
    public string ThreadId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ThreadChatMessage> Messages { get; set; } = new();
}

public class ThreadChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

#endregion

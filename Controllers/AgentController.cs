using Microsoft.AspNetCore.Mvc;
using FogData.Services;

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

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeIntent([FromBody] AnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            return BadRequest("User input is required");
        }

        var response = await _agentService.AnalyzeIntentAsync(request.UserInput);
        return Ok(response);
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteTool([FromBody] ExecuteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ToolName))
        {
            return BadRequest("Tool name is required");
        }

        var result = await _agentService.ExecuteToolAsync(request.ToolName, request.Parameters ?? new Dictionary<string, object>());
        return Ok(result);
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessUserRequest([FromBody] AnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserInput))
        {
            return BadRequest("User input is required");
        }

        // Step 1: Analyze intent
        var analysis = await _agentService.AnalyzeIntentAsync(request.UserInput);

        // Step 2: Execute the tool
        var result = await _agentService.ExecuteToolAsync(analysis.ToolToCall, analysis.Parameters);

        return Ok(new
        {
            Analysis = analysis,
            Result = result
        });
    }
}

public record AnalyzeRequest
{
    public string UserInput { get; init; } = string.Empty;
}

public record ExecuteRequest
{
    public string ToolName { get; init; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; init; }
}
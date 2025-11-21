namespace FogData.Services;

/// <summary>
/// Service interface for generating UI components using JSON DSL format.
/// This is a separate interface from IAgentService to allow parallel implementations.
/// </summary>
public interface IGenerativeUIService
{
    /// <summary>
    /// Processes a user message and returns a stream of JSON DSL responses.
    /// Each yielded string is a complete or partial GenerativeUIResponse in JSON format.
    /// </summary>
    /// <param name="userMessage">The user's natural language query</param>
    /// <returns>Async enumerable of JSON strings representing the response</returns>
    IAsyncEnumerable<string> ProcessUserMessageAsync(string userMessage);
}

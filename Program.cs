using FogData.Services.GenerativeUI;
using GenUI.Services;

var builder = WebApplication.CreateBuilder(args);

// Load .env file for local development
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ============================================
// GenUI BYOK Services
// ============================================

// Kernel Factory - creates LLM kernels from user's API keys at runtime
builder.Services.AddSingleton<IKernelFactory, KernelFactory>();

// UI Response Parser - extracts structured UI from LLM responses
builder.Services.AddSingleton<UIResponseParser>();

// Token Cost Calculator - calculates usage and cost per request
builder.Services.AddSingleton<TokenCostCalculator>();

// Add HttpClient for external API calls
builder.Services.AddHttpClient();

// Add CORS - allow any origin for API usage
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Always enable CORS for API
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", version = "1.0.0" }));

// API info endpoint
app.MapGet("/", () => Results.Ok(new
{
    name = "GenUI API",
    version = "1.0.0",
    description = "OpenAI-compatible API with Generative UI capabilities",
    endpoints = new
    {
        chatCompletions = "POST /v1/chat/completions",
        health = "GET /health"
    },
    headers = new
    {
        required = new[] { "X-LLM-API-Key: Your OpenAI/Azure API key" },
        optional = new[] {
            "X-LLM-Provider: openai | azure",
            "X-Azure-Endpoint: Azure OpenAI endpoint URL",
            "X-Azure-Deployment: Azure deployment name"
        }
    }
}));

app.Run();

using FogData.Services;
using FogData.Services.GenerativeUI;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register Semantic Kernel via DI
builder.Services.AddSingleton<Kernel>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();
    
    var provider = configuration["SemanticKernel:Provider"];
    
    switch (provider)
    {
        case "OpenAI":
            var openAiKey = configuration["SemanticKernel:OpenAI:ApiKey"];
            var openAiModel = configuration["SemanticKernel:OpenAI:ModelId"] ?? "gpt-4o-mini";
            if (!string.IsNullOrEmpty(openAiKey))
                kernelBuilder.AddOpenAIChatCompletion(openAiModel, openAiKey);
            break;
            
        case "AzureOpenAI":
        default:
            var endpoint = configuration["SemanticKernel:AzureOpenAI:Endpoint"];
            var apiKey = configuration["SemanticKernel:AzureOpenAI:ApiKey"];
            var deployment = configuration["SemanticKernel:AzureOpenAI:DeploymentName"];
            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
                kernelBuilder.AddAzureOpenAIChatCompletion(deployment!, endpoint, apiKey);
            break;
    }
    
    return kernelBuilder.Build();
});

// Register UIResponseParser via DI
builder.Services.AddSingleton<UIResponseParser>();

// Add Generative UI Service - pure LLM middleware, no database dependency
builder.Services.AddScoped<IGenerativeUIService, GenerativeUIProxyService>();

// Add HttpClient for external API calls (web search, etc.)
builder.Services.AddHttpClient();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Vite default port
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("AllowReactApp");
}
else
{
    // Serve React static files in production
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

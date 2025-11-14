# AI Configuration Guide

## Overview
The Agent Service uses Microsoft Semantic Kernel to power AI-driven intent analysis. The AI decides which tool to call and extracts parameters from natural language input.

## Supported Providers

### 1. OpenAI (Recommended for PoC)
**Setup:**
1. Get an API key from [OpenAI Platform](https://platform.openai.com/api-keys)
2. Add to `appsettings.Development.json`:
```json
{
  "SemanticKernel": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-proj-...",
      "ModelId": "gpt-4o-mini"
    }
  }
}
```

**OR** set environment variable:
```bash
export SEMANTICKERNEL__OPENAI__APIKEY="sk-proj-..."
```

**Docker Compose:**
```yaml
backend:
  environment:
    - SemanticKernel__OpenAI__ApiKey=${OPENAI_API_KEY}
```

### 2. Azure OpenAI
**Setup:**
```json
{
  "SemanticKernel": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/",
      "ApiKey": "your-azure-key",
      "DeploymentName": "gpt-4"
    }
  }
}
```

### 3. Ollama (Local, Free)
**Setup:**
1. Install Ollama: `brew install ollama` (macOS)
2. Pull a model: `ollama pull llama3.2`
3. Start Ollama: `ollama serve`
4. Configure:
```json
{
  "SemanticKernel": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelId": "llama3.2"
    }
  }
}
```

## AI Intent Analysis

The AI analyzes user input and decides:
- **Which tool to call** (GetWeatherData, GetSalesData, GetTopSalesPeople)
- **What parameters to extract** (location, region, dates, limits)
- **Reasoning** for the decision

### Example Queries

**Weather:**
```
"Show me the weather in Tokyo"
"What's the temperature in London?"
"Give me weather forecast for New York"
```

**Sales:**
```
"Show sales data for Europe"
"Get sales from last week in North America"
"What are the sales numbers?"
```

**Top Performers:**
```
"Who are the top salespeople?"
"Show me best performing sales reps"
"Top 3 salespeople by revenue"
```

## Testing Without AI (Fallback)

If no AI provider is configured, the system falls back to rule-based matching. This is NOT recommended for production but works for basic testing.

## Environment Variables

For Docker deployment, use environment variables:

```bash
# .env file
OPENAI_API_KEY=sk-proj-...

# docker-compose.override.yml
backend:
  environment:
    - SemanticKernel__Provider=OpenAI
    - SemanticKernel__OpenAI__ApiKey=${OPENAI_API_KEY}
    - SemanticKernel__OpenAI__ModelId=gpt-4o-mini
```

## Cost Considerations

**OpenAI GPT-4o-mini:** ~$0.15 per 1M input tokens, $0.60 per 1M output tokens
- Each query: ~200 tokens input, ~100 tokens output = $0.00009 per query
- 10,000 queries ≈ $0.90

**Ollama:** Free, runs locally, slower but no API costs

## Security Notes

⚠️ **Never commit API keys to Git!**

Use one of these methods:
1. Environment variables
2. User secrets: `dotnet user-secrets set "SemanticKernel:OpenAI:ApiKey" "sk-proj-..."`
3. Azure Key Vault for production
4. Docker secrets

## Verification

Test the AI integration:
```bash
curl -X POST http://localhost:5001/api/agent/analyze \
  -H "Content-Type: application/json" \
  -d '{"userInput": "Show me weather in Paris"}'
```

Expected response:
```json
{
  "intent": "Get weather information for Paris",
  "toolToCall": "GetWeatherData",
  "parameters": {"location": "Paris"},
  "reasoning": "User requested weather data for a specific location"
}
```

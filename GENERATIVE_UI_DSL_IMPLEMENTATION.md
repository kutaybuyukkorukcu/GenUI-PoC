# Generative UI DSL Implementation Guide

## 🎯 Overview

This document describes the implementation of a **JSON DSL-based Generative UI** approach, inspired by Thesys C1, that runs **parallel** to your existing tool-calling implementation.

### Key Features
- ✅ **Zero breaking changes** - Existing implementation untouched
- ✅ **Feature flag toggle** - Switch between implementations via configuration
- ✅ **Unified response format** - Single JSON structure for thinking, text, and components
- ✅ **Streaming support** - Progressive rendering of responses
- ✅ **LLM-native** - AI generates UI specifications directly

---

## 📐 Architecture

### JSON DSL Response Structure

```json
{
  "thinking": [
    {
      "status": "complete",
      "message": "Analyzing your query...",
      "timestamp": "2025-11-21T10:00:00Z"
    },
    {
      "status": "active",
      "message": "Fetching weather data..."
    }
  ],
  "content": [
    {
      "type": "text",
      "value": "Here's the current weather in New York:"
    },
    {
      "type": "component",
      "componentType": "weather",
      "props": {
        "location": "New York",
        "temperature": 72,
        "condition": "Sunny",
        "humidity": 45,
        "windSpeed": 10
      }
    },
    {
      "type": "text",
      "value": "Perfect weather for outdoor activities! 🌞"
    }
  ],
  "metadata": {
    "timestamp": "2025-11-21T10:00:05Z",
    "version": "1.0",
    "modelUsed": "AzureOpenAI",
    "queryType": "weather"
  }
}
```

---

## 🏗️ Implementation Files

### Backend Files (C#)

#### 1. **Configuration Files**
- `appsettings.json`
- `appsettings.Development.json`

Added feature flag:
```json
{
  "Features": {
    "UseGenerativeUIDSL": false
  }
}
```

#### 2. **Models** (`Models/GenerativeUI/`)
- **`ThinkingItem.cs`** - Represents AI reasoning steps
- **`ContentBlock.cs`** - Base class for content (text or component)
- **`GenerativeUIResponse.cs`** - Complete response structure

#### 3. **Services**
- **`IGenerativeUIService.cs`** - New service interface
- **`GenerativeUIService.cs`** - Main implementation
- **`GenerativeUI/GenerativeUIResponseBuilder.cs`** - Helper to build JSON DSL responses

#### 4. **Controllers**
- **`AgentController.cs`** - Updated with feature flag routing

#### 5. **Dependency Injection**
- **`Program.cs`** - Registered both services

---

## 🔄 How It Works

### Flow Diagram

```
User Query
    ↓
AgentController checks feature flag
    ↓
┌─────────────────────┬─────────────────────┐
│                     │                     │
UseGenerativeUIDSL    UseGenerativeUIDSL
= false               = true
│                     │
│                     │
AgentService          GenerativeUIService
(EXISTING)            (NEW)
│                     │
Tool Calling          Query Analysis
Pattern Match         ↓
│                     Fetch Data
│                     ↓
ComponentType         Build JSON DSL
Switch                ↓
│                     Stream Response
↓                     ↓
SSE Events            SSE Events
└─────────────────────┴─────────────────────┘
                      ↓
                  Frontend
```

### Request Flow (New Implementation)

1. **User sends query**: "Show me weather in New York"

2. **AgentController** checks `Features:UseGenerativeUIDSL`
   - If `false`: Routes to `AgentService` (existing)
   - If `true`: Routes to `GenerativeUIService` (new)

3. **GenerativeUIService.ProcessUserMessageAsync()**
   - Creates `GenerativeUIResponseBuilder`
   - Adds thinking item: "Analyzing your query..."
   - Streams partial response
   - Analyzes query type (weather/sales/performance)
   - Updates thinking: "Query type: weather"
   - Fetches data from database
   - Updates thinking: "Generating response..."
   - Builds content with text and components
   - Adds metadata
   - Streams final response

4. **Controller sends SSE event**
   ```
   event: generative-ui
   data: {"response": "{...JSON DSL...}"}
   ```

5. **Frontend receives and renders**
   - Parses JSON DSL
   - Shows thinking states
   - Renders text blocks
   - Renders components dynamically
   - Shows metadata

---

## 🚀 Usage

### Enable New Implementation

**appsettings.Development.json:**
```json
{
  "Features": {
    "UseGenerativeUIDSL": true
  }
}
```

### Disable (Use Existing Implementation)

```json
{
  "Features": {
    "UseGenerativeUIDSL": false
  }
}
```

---

## 📊 Comparison: Old vs New

| Aspect | Tool-Calling (Old) | JSON DSL (New) |
|--------|-------------------|----------------|
| **Response Format** | Separate SSE events per type | Unified JSON structure |
| **Intent Detection** | LLM matches to tool descriptions | LLM infers from query keywords |
| **Data Flow** | LLM → Tool Call → Execute → Return | Analyze → Fetch → Build Response |
| **Frontend Parsing** | Switch on ComponentType | Parse JSON DSL structure |
| **Thinking States** | Not included | Built into response |
| **Flexibility** | Fixed component types | Mix text + components freely |
| **Streaming** | Event-based | Progressive JSON |

---

## 🔧 Extending the System

### Adding a New Component Type

#### 1. Update GenerativeUIService

Add new query type detection in `InferQueryType`:
```csharp
else if (lower.Contains("custom") || lower.Contains("mycomponent"))
{
    return new QueryAnalysis { QueryType = "custom" };
}
```

#### 2. Add data fetching logic

In `FetchDataAsync`:
```csharp
case "custom":
    return await _dbContext.CustomData
        .Take(10)
        .ToListAsync();
```

#### 3. Add response builder

Create `BuildCustomResponseAsync`:
```csharp
private async Task BuildCustomResponseAsync(
    GenerativeUIResponseBuilder builder, 
    object? customData, 
    string userMessage)
{
    builder.AddText("Here's your custom data:");
    builder.AddComponent("custom", new
    {
        // Your component props
    });
}
```

#### 4. Wire it up in `BuildResponseContentAsync`

```csharp
case "custom":
    await BuildCustomResponseAsync(builder, data, userMessage);
    break;
```

---

## 🎨 Frontend Implementation (Next Steps)

### Create JSON DSL Parser Component

```tsx
// components/GenerativeUIRenderer.tsx
interface GenerativeUIRendererProps {
  jsonResponse: string;
  isStreaming?: boolean;
}

export const GenerativeUIRenderer: React.FC<GenerativeUIRendererProps> = ({
  jsonResponse,
  isStreaming
}) => {
  const parsed = JSON.parse(jsonResponse);
  
  return (
    <div>
      {/* Render thinking states */}
      {parsed.thinking?.map((item, i) => (
        <ThinkingIndicator key={i} {...item} />
      ))}
      
      {/* Render content blocks */}
      {parsed.content?.map((block, i) => {
        if (block.type === 'text') {
          return <TextBlock key={i} value={block.value} />;
        } else if (block.type === 'component') {
          return <DynamicComponent key={i} {...block} />;
        }
      })}
    </div>
  );
};
```

### Update SSE Handler

```tsx
// hooks/useSSEChat.ts
const handleSSEEvent = (event: MessageEvent) => {
  const eventType = event.type;
  
  if (eventType === 'generative-ui') {
    const data = JSON.parse(event.data);
    const response = JSON.parse(data.response);
    
    // Render using GenerativeUIRenderer
    setMessages(prev => [...prev, {
      id: generateId(),
      role: 'assistant',
      generativeUIResponse: response,
      isGenerativeUI: true
    }]);
  }
};
```

### Component Registry

```tsx
// components/ComponentRegistry.ts
const COMPONENT_MAP = {
  'weather': WeatherCard,
  'chart': ChartRenderer,
  'table': TableRenderer,
  // Add more as needed
};

export const DynamicComponent = ({ componentType, props }) => {
  const Component = COMPONENT_MAP[componentType];
  return Component ? <Component {...props} /> : null;
};
```

---

## 🧪 Testing

### Test with Feature Flag OFF (Existing Behavior)

```bash
# In appsettings.Development.json
"UseGenerativeUIDSL": false

# Start backend
dotnet run

# Query: "Show me weather in New York"
# Expected: Tool-calling approach with separate SSE events
```

### Test with Feature Flag ON (New Behavior)

```bash
# In appsettings.Development.json
"UseGenerativeUIDSL": true

# Start backend
dotnet run

# Query: "Show me weather in New York"
# Expected: Single generative-ui event with JSON DSL
```

---

## 📝 Example Responses

### Weather Query Response

```json
{
  "thinking": [
    {"status": "complete", "message": "Analyzing your query..."},
    {"status": "complete", "message": "Query type: weather"},
    {"status": "complete", "message": "Fetching data..."},
    {"status": "complete", "message": "Generating response..."}
  ],
  "content": [
    {
      "type": "text",
      "value": "Here's the latest weather data for New York:"
    },
    {
      "type": "component",
      "componentType": "weather",
      "props": {
        "location": "New York",
        "temperature": 72,
        "condition": "Sunny",
        "humidity": 45,
        "windSpeed": 10,
        "date": "Nov 21, 2025"
      }
    },
    {
      "type": "text",
      "value": "It's quite warm! Stay hydrated. 🌞"
    }
  ],
  "metadata": {
    "timestamp": "2025-11-21T10:00:00Z",
    "version": "1.0",
    "modelUsed": "AzureOpenAI",
    "queryType": "weather"
  }
}
```

### Sales Query Response

```json
{
  "thinking": [...],
  "content": [
    {
      "type": "text",
      "value": "Here are the latest 20 sales records:"
    },
    {
      "type": "component",
      "componentType": "table",
      "props": {
        "columns": ["Product", "Amount", "Region", "Date", "Salesperson"],
        "rows": [...]
      }
    },
    {
      "type": "text",
      "value": "Total revenue: $125,430.00"
    }
  ],
  "metadata": {...}
}
```

---

## 🚦 Migration Strategy

### Phase 1: Parallel Running (Current State)
- Both implementations coexist
- Feature flag controls which is used
- Test thoroughly with flag ON and OFF

### Phase 2: A/B Testing (Optional)
- Run both in production
- Route 50% of traffic to each
- Compare performance, user satisfaction

### Phase 3: Frontend Implementation
- Build `GenerativeUIRenderer` component
- Update SSE handler
- Add component registry
- Test rendering

### Phase 4: Full Migration
- Set `UseGenerativeUIDSL: true` by default
- Monitor for issues
- Gradually deprecate old approach

### Phase 5: Cleanup (Future)
- Remove `AgentService` (old implementation)
- Remove old SSE event types
- Remove feature flag
- Keep only `GenerativeUIService`

---

## 🐛 Troubleshooting

### Backend not responding
- Check `Features:UseGenerativeUIDSL` is set correctly
- Verify `GenerativeUIService` is registered in DI
- Check logs for exceptions

### Frontend not rendering
- Verify SSE event type is `generative-ui`
- Check JSON parsing (use `JSON.parse(data.response)`)
- Ensure component registry has the componentType

### LLM not generating correct format
- Review system prompt in `AnalyzeQueryAsync`
- Add more examples to prompt
- Consider using structured output (JSON mode)

---

## 📚 Resources

### Inspiration
- [Thesys C1 Documentation](https://docs.thesys.dev/)
- [Vercel AI SDK](https://sdk.vercel.ai/)
- [Generative UI Concepts](https://sdk.vercel.ai/docs/ai-sdk-rsc)

### Related Files
- `GENERATIVE_UI_POC_ARCHITECTURE.md` - Original architecture
- `CLEAN_ARCHITECTURE_GUIDE.md` - Clean architecture principles

---

## ✅ Implementation Checklist

- [x] Add feature flag to configuration
- [x] Create GenerativeUI models
- [x] Implement `IGenerativeUIService` interface
- [x] Create `GenerativeUIResponseBuilder` helper
- [x] Implement `GenerativeUIService` 
- [x] Update `AgentController` with feature flag routing
- [x] Register service in DI container
- [x] Verify no compilation errors
- [ ] Test backend with flag OFF
- [ ] Test backend with flag ON
- [ ] Create frontend `GenerativeUIRenderer`
- [ ] Update SSE handler to parse JSON DSL
- [ ] Create component registry
- [ ] Test end-to-end flow
- [ ] Document for team
- [ ] Deploy to staging

---

**Implementation Date:** November 21, 2025  
**Status:** ✅ Backend Complete - Frontend Pending  
**Feature Flag:** `Features:UseGenerativeUIDSL` (default: `false`)

# Frontend Generative UI DSL Implementation

## 🎯 Overview

This document describes the frontend implementation for rendering JSON DSL-based Generative UI responses. The implementation supports **both** the legacy tool-calling format and the new JSON DSL format via a feature flag.

---

## 🚀 Feature Flag

### Environment Variables

**`.env` and `.env.docker`:**
```bash
VITE_USE_GENERATIVE_UI_DSL=false  # Set to 'true' to enable
```

### Usage

The feature flag is checked in `useSSEChat.ts`:
```typescript
const USE_GENERATIVE_UI = import.meta.env.VITE_USE_GENERATIVE_UI_DSL === 'true';
```

---

## 📁 Files Created/Modified

### New Files

1. **`src/components/renderers/ComponentRegistry.tsx`**
   - Maps component types to React components
   - Dynamic component rendering
   - Fallback for unknown components

2. **`src/components/renderers/ThinkingIndicator.tsx`**
   - Shows AI reasoning states
   - Animated loader for active states
   - Checkmark for completed states

3. **`src/components/renderers/GenerativeUIRenderer.tsx`**
   - Main renderer for JSON DSL responses
   - Handles thinking, content blocks, and metadata
   - Supports streaming

4. **`FRONTEND_GENERATIVE_UI_IMPLEMENTATION.md`**
   - This documentation file

### Modified Files

1. **`src/types/index.ts`**
   - Added `GenerativeUIResponse` type
   - Added `ThinkingItem`, `ContentBlock` types
   - Extended `ChatMessage` with `generativeUIResponse` and `isGenerativeUI`

2. **`src/hooks/useSSEChat.ts`**
   - Added support for `generative-ui` SSE events
   - Parses JSON DSL responses
   - Feature flag integration

3. **`src/components/chat/MessageList.tsx`**
   - Renders messages in both formats
   - Conditionally uses `GenerativeUIRenderer` or legacy rendering

4. **`.env` and `.env.docker`**
   - Added `VITE_USE_GENERATIVE_UI_DSL` flag

---

## 📐 Architecture

### Message Flow

```
User sends message
    ↓
useSSEChat hook
    ↓
SSE Event received
    ↓
┌─────────────────────────────────────────┐
│  Event type: generative-ui?             │
│  AND USE_GENERATIVE_UI = true?          │
└─────────────────────────────────────────┘
         │                    │
         YES                  NO
         │                    │
         ▼                    ▼
Parse JSON DSL        Use legacy format
    ↓                        ↓
Set generativeUIResponse    Set toolResult
    ↓                        ↓
ChatMessage.isGenerativeUI = true
         │
         ▼
    MessageList
         │
         ▼
┌─────────────────────────────────────────┐
│  message.isGenerativeUI === true?       │
└─────────────────────────────────────────┘
         │                    │
         YES                  NO
         │                    │
         ▼                    ▼
GenerativeUIRenderer    Legacy rendering
    ↓                   (ComponentRenderer)
    │
    ├─→ ThinkingList (thinking states)
    │
    ├─→ ContentBlocks (loop)
    │   ├─→ TextBlock (text)
    │   └─→ DynamicComponent (components)
    │       └─→ ComponentRegistry
    │           ├─→ WeatherCard
    │           ├─→ ChartRenderer
    │           └─→ TableRenderer
    │
    └─→ Metadata (dev only)
```

---

## 🎨 Components

### 1. GenerativeUIRenderer

**Purpose:** Main renderer for JSON DSL responses

**Props:**
```typescript
interface GenerativeUIRendererProps {
  response: GenerativeUIResponse;
  isStreaming?: boolean;
}
```

**Features:**
- Renders thinking states
- Renders content blocks (text + components)
- Shows error states
- Displays metadata in dev mode
- Streaming indicator

**Usage:**
```tsx
<GenerativeUIRenderer 
  response={message.generativeUIResponse} 
  isStreaming={message.isStreaming}
/>
```

---

### 2. ThinkingIndicator / ThinkingList

**Purpose:** Shows AI reasoning process

**Props:**
```typescript
interface ThinkingIndicatorProps {
  item: ThinkingItem;
}

interface ThinkingListProps {
  thinking: ThinkingItem[];
}
```

**Features:**
- Animated loader for active states
- Checkmark for completed states
- Collapsible thinking box
- Color-coded states

**Example:**
```tsx
<ThinkingList thinking={response.thinking} />
```

---

### 3. ComponentRegistry / DynamicComponent

**Purpose:** Dynamic component rendering based on type

**Registry:**
```typescript
const COMPONENT_REGISTRY = {
  weather: WeatherCard,
  chart: ChartRenderer,
  table: TableRenderer,
};
```

**Props:**
```typescript
interface DynamicComponentProps {
  block: ComponentBlock;
}
```

**Features:**
- Type-safe component mapping
- Fallback for unknown types
- Props forwarding

**Example:**
```tsx
<DynamicComponent block={componentBlock} />
```

---

## 🔄 Response Format

### GenerativeUIResponse Structure

```typescript
interface GenerativeUIResponse {
  thinking: ThinkingItem[];
  content: ContentBlock[];
  metadata: {
    timestamp?: string;
    version?: string;
    modelUsed?: string;
    queryType?: string;
    error?: boolean;
    [key: string]: any;
  };
}
```

### Example Response

```json
{
  "thinking": [
    {
      "status": "complete",
      "message": "Analyzing your query...",
      "timestamp": "2025-11-21T10:00:00Z"
    },
    {
      "status": "complete",
      "message": "Query type: weather"
    },
    {
      "status": "active",
      "message": "Fetching data..."
    }
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
    "timestamp": "2025-11-21T10:00:05Z",
    "version": "1.0",
    "modelUsed": "AzureOpenAI",
    "queryType": "weather"
  }
}
```

---

## 🧪 Testing

### Test with Feature Flag OFF (Legacy)

1. Set `.env`:
   ```bash
   VITE_USE_GENERATIVE_UI_DSL=false
   ```

2. Restart dev server:
   ```bash
   npm run dev
   ```

3. Test query:
   ```
   "Show me weather in New York"
   ```

4. **Expected:** Legacy ComponentRenderer with tool-result format

### Test with Feature Flag ON (New)

1. Set `.env`:
   ```bash
   VITE_USE_GENERATIVE_UI_DSL=true
   ```

2. **Also update backend** `appsettings.Development.json`:
   ```json
   {
     "Features": {
       "UseGenerativeUIDSL": true
     }
   }
   ```

3. Restart both servers

4. Test query:
   ```
   "Show me weather in New York"
   ```

5. **Expected:** 
   - Thinking states appear progressively
   - Text and weather component render
   - Natural language explanations mixed with components

---

## 🎯 Extending

### Adding a New Component Type

1. **Create the component** (if not exists):
   ```tsx
   // components/renderers/MyNewComponent.tsx
   export const MyNewComponent = ({ data }: { data: MyDataType }) => {
     return <div>...</div>;
   };
   ```

2. **Register in ComponentRegistry**:
   ```tsx
   const COMPONENT_REGISTRY = {
     weather: WeatherCard,
     chart: ChartRenderer,
     table: TableRenderer,
     mynew: MyNewComponent, // Add here
   };
   ```

3. **Backend returns the new type**:
   ```csharp
   builder.AddComponent("mynew", new { /* props */ });
   ```

4. **Done!** The component will render automatically.

---

## 🐛 Troubleshooting

### Component not rendering

**Check:**
1. Is `VITE_USE_GENERATIVE_UI_DSL=true`?
2. Is backend also set to `UseGenerativeUIDSL: true`?
3. Is the SSE event type `generative-ui`?
4. Check browser console for parsing errors

### Thinking states not showing

**Check:**
1. Response has `thinking` array with items
2. Items have `status` and `message` fields
3. ThinkingList is imported in GenerativeUIRenderer

### Unknown component type error

**Check:**
1. Component type is registered in `COMPONENT_REGISTRY`
2. Component type matches between backend and frontend
3. Check console for component type name

---

## 📊 Feature Comparison

| Feature | Legacy Format | Generative UI DSL |
|---------|--------------|-------------------|
| **Thinking States** | ❌ Not supported | ✅ Built-in |
| **Text + Components** | ❌ Separate messages | ✅ Mixed freely |
| **Streaming** | ✅ Supported | ✅ Supported |
| **Error Handling** | ✅ Basic | ✅ Rich metadata |
| **Extensibility** | ⚠️ Add switch cases | ✅ Registry-based |
| **Type Safety** | ⚠️ Loose types | ✅ Strong types |

---

## 📚 Related Files

### Backend
- `GENERATIVE_UI_DSL_IMPLEMENTATION.md` - Backend documentation
- `Services/GenerativeUIService.cs` - Backend service
- `Controllers/AgentController.cs` - Feature flag routing

### Frontend
- `src/components/renderers/GenerativeUIRenderer.tsx` - Main renderer
- `src/components/renderers/ComponentRegistry.tsx` - Component mapping
- `src/components/renderers/ThinkingIndicator.tsx` - Thinking states
- `src/hooks/useSSEChat.ts` - SSE handling
- `src/types/index.ts` - Type definitions

---

## ✅ Implementation Checklist

- [x] Add feature flag to `.env` files
- [x] Create TypeScript types for JSON DSL
- [x] Create ComponentRegistry
- [x] Create ThinkingIndicator component
- [x] Create GenerativeUIRenderer
- [x] Update useSSEChat hook
- [x] Update MessageList component
- [x] Update ChatInterface (no changes needed)
- [ ] Test with feature flag OFF
- [ ] Test with feature flag ON
- [ ] Test all component types (weather, chart, table)
- [ ] Test error handling
- [ ] Test streaming behavior
- [ ] Document for team

---

**Implementation Date:** November 21, 2025  
**Status:** ✅ Complete - Ready for Testing  
**Feature Flag:** `VITE_USE_GENERATIVE_UI_DSL` (default: `false`)

---

## 🎉 Usage Example

```tsx
// The frontend automatically handles both formats!

// User sends: "Show me weather in New York"

// With VITE_USE_GENERATIVE_UI_DSL=false:
// → Renders using ComponentRenderer (legacy)

// With VITE_USE_GENERATIVE_UI_DSL=true:
// → Renders using GenerativeUIRenderer (new)
// → Shows thinking: "Analyzing query..."
// → Shows thinking: "Query type: weather"
// → Renders text: "Here's the weather..."
// → Renders WeatherCard component
// → Renders text: "Stay hydrated! 🌞"
```

No code changes needed in ChatInterface - it just works! 🚀

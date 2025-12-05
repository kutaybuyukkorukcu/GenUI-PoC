# GenUI Platform - Vision & Progress

> **Last Updated:** December 6, 2025

## 🎯 Vision

Build a **Generative UI middleware platform** - similar to [Thesys C1](https://thesys.dev) - that transforms LLM responses into beautiful, interactive UI components. Users bring their own LLM API keys (BYOK model), and we provide the "GenUI layer" that handles component selection, structured output, and rendering.

## 💼 Business Model: BYOK (Bring Your Own Key)

```
┌─────────────────────────────────────────────────────────────────────┐
│                         USER'S APPLICATION                          │
│  ┌─────────────┐                                                    │
│  │  Frontend   │  React/Vue/etc with our UI Component SDK          │
│  │  (React)    │  Renders: Card, Table, Chart, Form, List, etc.    │
│  └──────┬──────┘                                                    │
│         │                                                            │
│         ▼                                                            │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  GenUI API Middleware (THIS PROJECT)                        │    │
│  │  POST /v1/chat/completions (OpenAI-compatible)              │    │
│  │  ─────────────────────────────────────────────────────────  │    │
│  │  • Receives user's LLM API key via header                   │    │
│  │  • Injects system prompts for UI component selection        │    │
│  │  • Parses structured <genui> JSON from LLM response         │    │
│  │  • Returns SSE stream with component data                   │    │
│  └──────┬──────────────────────────────────────────────────────┘    │
│         │                                                            │
│         │ User's API Key (X-LLM-API-Key header)                     │
│         ▼                                                            │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  LLM Provider (User's account, User pays directly)          │    │
│  │  • OpenAI (GPT-4, etc.)                                     │    │
│  │  • Azure OpenAI                                              │    │
│  │  • Anthropic Claude (future)                                 │    │
│  │  • Google Gemini (future)                                    │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

### Revenue Model Options
1. **Subscription** - Monthly fee for API access (like Thesys)
2. **Usage-based** - Per-request pricing for GenUI processing
3. **Freemium** - Free tier with limits, paid for higher usage
4. **Enterprise** - Self-hosted licenses

### Why BYOK?
- **No LLM costs for us** - User manages their own API spend
- **Trust** - User's data goes directly to their LLM provider
- **Flexibility** - User can use any model/provider they prefer
- **Value prop** - We charge for the GenUI layer, not LLM access

---

## ✅ Current Implementation Status

### Phase 1: Core API (COMPLETE)

| Component | Status | Description |
|-----------|--------|-------------|
| OpenAI-compatible endpoint | ✅ Done | `POST /v1/chat/completions` |
| BYOK KernelFactory | ✅ Done | Creates Kernel from user's API key at runtime |
| Request/Response models | ✅ Done | `ChatCompletionRequest`, `ChatCompletionResponse` |
| SSE Streaming | ✅ Done | Real-time token streaming to frontend |
| System prompts | ✅ Done | `UIComponentPrompts.cs` - teaches LLM to output GenUI |
| Response parser | ✅ Done | `UIResponseParser.cs` - extracts `<genui>` JSON |
| Fallback config | ✅ Done | Uses `.env` Azure OpenAI if no header key |

### Phase 2: Frontend SDK (COMPLETE for PoC)

| Component | Status | Description |
|-----------|--------|-------------|
| SSE Hook | ✅ Done | `useSSEChat.ts` - handles streaming |
| Component Registry | ✅ Done | Maps componentType → React component |
| Card Renderer | ✅ Done | Generic card with title, data, description |
| Table Renderer | ✅ Done | Dynamic columns, auto-derive from data |
| Chart Renderer | ✅ Done | Recharts integration |
| Thinking Indicator | ✅ Done | Shows LLM reasoning steps |
| Chat Interface | ✅ Done | Full chat UI with message history |

### Phase 3: Docker & DevEx (COMPLETE)

| Component | Status | Description |
|-----------|--------|-------------|
| Docker Compose | ✅ Done | Backend + Frontend containers |
| Hot Reload | ✅ Done | `dotnet watch` + Vite HMR |
| Health check | ✅ Done | `/health` endpoint |
| nginx proxy | ✅ Done | `/v1/*` → backend |

---

## 🚧 What's Left to Build

### Phase 4: Production Readiness

| Component | Priority | Description |
|-----------|----------|-------------|
| Platform API Key middleware | High | Authenticate GenUI users (not LLM users) |
| Usage tracking/billing | High | Track requests per user for billing |
| Rate limiting | High | Prevent abuse |
| Redis session store | Medium | Persist conversation history |
| Multi-tenant isolation | Medium | User data separation |
| Error handling/retries | Medium | Graceful LLM failure handling |

### Phase 5: SDK Distribution

| Component | Priority | Description |
|-----------|----------|-------------|
| React SDK package | High | `@genui/react` npm package |
| Vue SDK | Medium | `@genui/vue` npm package |
| .NET client | Low | For .NET frontend apps |
| Documentation site | High | API docs, component gallery |

### Phase 6: Advanced Features

| Component | Priority | Description |
|-----------|----------|-------------|
| MCP (Model Context Protocol) | High | Connect to external tools/databases |
| Custom component schemas | Medium | User-defined components |
| Component theming | Medium | Custom styling/branding |
| Form submissions | Medium | Handle user input from forms |
| Multi-modal (images) | Low | Image generation/display |

---

## 🏗️ Architecture

### Backend (.NET 9)
```
Controllers/
  └── ChatCompletionsController.cs   # OpenAI-compatible API
Services/
  ├── KernelFactory.cs               # BYOK - creates kernels per request
  └── GenerativeUI/
      ├── UIComponentPrompts.cs      # System prompts for LLM
      ├── UIResponseParser.cs        # Parses <genui> from response
      └── GenerativeUIModels.cs      # Component prop types
Models/
  └── OpenAI/                        # OpenAI-compatible DTOs
```

### Frontend (React + Vite)
```
client/src/
  ├── components/
  │   ├── chat/                      # Chat interface
  │   ├── renderers/                 # Component renderers
  │   │   ├── ComponentRegistry.tsx  # componentType → Component map
  │   │   ├── CardRenderer.tsx
  │   │   ├── TableRenderer.tsx
  │   │   ├── ChartRenderer.tsx
  │   │   └── ThinkingIndicator.tsx
  │   └── ui/                        # shadcn/ui primitives
  ├── hooks/
  │   └── useSSEChat.ts              # SSE streaming hook
  └── services/
      └── api.ts                     # API client
```

---

## 🔑 Key Files

| File | Purpose |
|------|---------|
| `Controllers/ChatCompletionsController.cs` | Main API - OpenAI-compatible with GenUI |
| `Services/KernelFactory.cs` | Creates Semantic Kernel from user's API key |
| `Services/GenerativeUI/UIComponentPrompts.cs` | System prompts that teach LLM to output UI |
| `Services/GenerativeUI/UIResponseParser.cs` | Extracts structured JSON from LLM response |
| `client/src/components/renderers/ComponentRegistry.tsx` | Maps component types to React renderers |
| `client/src/hooks/useSSEChat.ts` | Handles SSE streaming from backend |

---

## 📡 API Reference

### Chat Completions
```http
POST /v1/chat/completions
Content-Type: application/json
X-LLM-API-Key: sk-xxx (optional, falls back to .env)

{
  "model": "gpt-4",
  "messages": [
    {"role": "user", "content": "Show me weather in NYC"}
  ],
  "stream": true
}
```

**Response (SSE):**
```
data: {"id":"...","choices":[{"delta":{"content":"token"}}]}
data: {"id":"...","choices":[{"delta":{"content":"token"}}]}
...
event: genui
data: {"thinking":[...],"content":[{"type":"component","componentType":"card","props":{...}}]}
data: [DONE]
```

### Health Check
```http
GET /health

{"status":"healthy","version":"1.0.0"}
```

---

## 🎮 How to Run

```bash
# Start everything
docker compose up -d --build

# Frontend: http://localhost:5173
# Backend:  http://localhost:5001
# Health:   http://localhost:5001/health
```

---

## 🎯 Success Criteria for MVP

1. ✅ User can send chat message via frontend
2. ✅ Backend uses user's LLM key (or fallback) to call LLM
3. ✅ LLM uses internet search to get real data
4. ✅ LLM outputs structured GenUI JSON
5. ✅ Backend parses and streams response
6. ✅ Frontend renders appropriate component (card, table, chart)
7. ⏳ Platform API key for authenticating GenUI users
8. ⏳ Usage tracking for billing

---

## 📊 Comparison with Thesys C1

| Feature | Thesys C1 | GenUI (This Project) |
|---------|-----------|----------------------|
| API Style | OpenAI-compatible | ✅ OpenAI-compatible |
| BYOK | Yes | ✅ Yes |
| Component Types | Card, List, Table, Chart, etc. | ✅ Same |
| SSE Streaming | Yes | ✅ Yes |
| React SDK | Yes (`@thesys/c1-react`) | ⏳ To package |
| MCP Support | Yes | ⏳ Planned |
| Multi-provider | OpenAI, Anthropic, etc. | ✅ OpenAI, Azure (more planned) |

---

## 🗺️ Roadmap

### Q1 2025
- [x] Core BYOK API
- [x] React component renderers
- [x] Docker deployment
- [ ] Platform API key middleware
- [ ] Usage tracking

### Q2 2025
- [ ] Publish `@genui/react` npm package
- [ ] MCP integration for external tools
- [ ] Documentation site
- [ ] Production deployment (Azure/AWS)

### Q3 2025
- [ ] Vue SDK
- [ ] Custom component schemas
- [ ] Enterprise self-hosted option

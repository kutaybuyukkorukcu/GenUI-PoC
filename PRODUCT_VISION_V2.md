# GenUI: Product Vision & Strategy

> **Last Updated:** December 27, 2025

---

## 🎯 Vision

Build a **Generative UI library** that transforms LLM responses into beautiful, interactive UI components—while adapting to the customer's existing design system.

**Tagline:** *"The Generative UI layer that adapts to YOUR design system."*

---

## 💼 Business Model: Library-First, B2B Enterprise

Unlike hosted services (Thesys C1), we provide an **npm library** that runs in the customer's infrastructure. Their API keys never leave their servers.

### Why Library-First?
| Concern | Hosted Service | Library (Us) |
|---------|---------------|--------------|
| **Trust** | "Share my OpenAI key with you?" ❌ | Runs in YOUR code ✅ |
| **Compliance** | SOC2/HIPAA burden on us | Customer handles it ✅ |
| **Customization** | Limited themes | Full design system adapters ✅ |
| **Vendor Lock-in** | High | Low (swap renderers anytime) ✅ |

---

## 🏗️ Architecture: Headless Core + Design System Adapters

```
┌────────────────────────────────────────────────────┐
│                   @genui/core                      │
│           (Headless: prompts, parser, types)       │
└─────────────────────┬──────────────────────────────┘
                      │ JSON DSL
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ @genui/react │ │ @genui/mui   │ │ @genui/ant   │
│ (Default)    │ │ (Material)   │ │ (Ant Design) │
└──────────────┘ └──────────────┘ └──────────────┘
```

**Key Packages:**
| Package | Description | Status |
|---------|-------------|--------|
| `@genui/core` | Headless logic: prompts, parser, types | MVP |
| `@genui/react` | Default Tailwind-based renderers | MVP |
| `@genui/mui` | Material UI adapter | v1.1 |
| `@genui/ant` | Ant Design adapter | v1.2 |
| `@genui/headless` | BYOC (Bring Your Own Components) | Enterprise |

---

## 🎯 Target Market

### Primary: Companies with Existing RAG/Chat Implementations
> *"Thousands of companies built text-only RAG chat UIs in 2023-2024. They're functional but ugly. Upgrading to rich UI is expensive."*

**We are the UI upgrade path.**

### Segments
| Segment | Pain Point | Our Solution |
|---------|-----------|--------------|
| **Startups with internal AI tools** | Text-only "chat with docs" | Rich cards, tables, charts |
| **SaaS with AI features** | Can't visualize structured data | Automatic component selection |
| **Enterprise POCs** | "Looks like a demo, not production" | Enterprise-grade UI quality |

---

## 🔌 Integration: Drop-In Simplicity

```typescript
import { createGenUIClient } from '@genui/core';
import { GenUIRenderer } from '@genui/mui'; // or @genui/react

// 1. Create client (uses THEIR API key)
const genui = createGenUIClient({
  provider: 'openai',
  apiKey: process.env.OPENAI_API_KEY,
});

// 2. Call LLM with GenUI enhancement
const result = await genui.chat({
  messages: [{ role: 'user', content: 'Show me Q3 sales' }],
});

// 3. Render the UI
<GenUIRenderer response={result} />
```

---

## 💰 Pricing Model

| Tier | Price | Includes |
|------|-------|----------|
| **Open Source** | Free | `@genui/core` + `@genui/react` |
| **Pro** | $X/month | Pre-built DS adapters (MUI, Ant, BaseUI) |
| **Enterprise** | Custom | Custom adapter development, SLA, support |

---

## 🏆 Competitive Landscape

| Competitor | Type | Differentiator |
|------------|------|----------------|
| **Thesys C1** | Hosted API | We're a library, not a service |
| **Vercel AI SDK** | SDK | We have component rendering built-in |
| **Open WebUI** | Application | We're embeddable, they're standalone |
| **Chainlit** | Python SDK | We're JS/TS native |

---

## ✅ Enterprise Buying Criteria

| Criteria | Our Answer |
|----------|------------|
| **Security** | Library runs in your infrastructure. No data sent to us. |
| **Compliance** | We're code, not a service. Compliance is on your side. |
| **Vendor Lock-in** | Open JSON DSL protocol. Swap adapters anytime. |
| **Customization** | Full design system adapter support. |
| **Support** | Enterprise tier with SLA. |

---

## 🗺️ Roadmap

### Phase 1: MVP (Q1 2025)
- [x] `@genui/core` with Bun/TypeScript
- [ ] Default React renderer (`@genui/react`)
- [ ] JSON DSL specification
- [ ] Basic components: Card, Table, Chart, List, Form

### Phase 2: Design System Adapters (Q2 2025)
- [ ] `@genui/mui` (Material UI)
- [ ] `@genui/ant` (Ant Design)
- [ ] Documentation site

### Phase 3: Enterprise (Q3 2025)
- [ ] `@genui/headless` (BYOC)
- [ ] Custom adapter consulting
- [ ] Production SLA

---

## 📝 Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| Dec 27, 2025 | Library-first, not hosted service | Trust barrier too high for BYOK hosted model |
| Dec 27, 2025 | Use Bun for development | Native TypeScript, faster builds |
| Dec 27, 2025 | B2B Enterprise focus | Design system adapter is the premium value |
| Dec 27, 2025 | Headless + Adapter architecture | Allows customers to keep their existing DS |

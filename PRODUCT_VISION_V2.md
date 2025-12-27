# GenUI: Product Vision & Strategy

> **Last Updated:** December 27, 2025

---

## 🎯 Vision

Build a **Generative UI platform** that transforms LLM responses into beautiful, interactive UI components—adapting to any design system, without requiring API key sharing.

**Tagline:** *"Turn any LLM response into production-ready UI. Your keys stay with you."*

---

## 💼 Business Model: Platform + Open SDK (Hybrid)

We offer **two paths** to maximize adoption and revenue:

| Tier | What They Get | Price |
|------|---------------|-------|
| **Open Source SDK** | `@genui/react` + CLI adapters | Free |
| **Managed Platform** | Hosted API (`POST /transform`) | Usage-based |
| **Enterprise** | Self-hosted platform + Custom adapters | Custom |

### Why Hybrid?

| Concern | Platform-Only (Thesys) | Library-Only | **Hybrid (Us)** |
|---------|------------------------|--------------|-----------------|
| **Adoption** | Slow (trust barrier) | Fast (open source) | **Fast** ✅ |
| **Revenue** | Easy (API fees) | Hard (support only) | **Balanced** ✅ |
| **Privacy** | Low (sees everything) | High (self-hosted) | **High** ✅ |

---

## 🏗️ Architecture: Platform as Middleware

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│  Customer App   │         │  GenUI Platform │         │  Customer Chat  │
│  (Their Backend)│         │  (Our Service)  │         │   (Frontend)    │
└────────┬────────┘         └────────┬────────┘         └────────┬────────┘
         │                           │                           │
   1. User Query ────────────────────────────────────────────────│
         │                           │                           │
   2. Call LLM (THEIR Key)           │                           │
         │                           │                           │
   3. Get Raw Response               │                           │
         │                           │                           │
   4. POST /genui/transform ─────────▶                           │
         │  { response: "...", context?: "..." }                 │
         │                           │                           │
         │    5. AI transforms to UI DSL                         │
         │                           │                           │
         │◀───── 6. Structured UI JSON ───────                   │
         │  { content: [...], components: [...] }                │
         │                           │                           │
   7. Forward to Frontend ──────────────────────────────────────▶│
         │                           │                   8. Render with SDK
```

**Key Differentiator vs Thesys:**
- Thesys: Customer gives API key → Thesys calls LLM → Privacy risk
- **GenUI:** Customer calls their own LLM → Sends only output → We never see keys

---

## 🎨 Frontend SDK: CLI + Adapters (Shadcn-Style)

Instead of shipping a "black box" library, we scaffold code into their project.

### The CLI Experience

```bash
$ npx genui@latest init

? Detected TypeScript & Tailwind. Proceed? (Y/n) > Yes
? Which Design System are you using?
  > Shadcn / Radix (Detected)
    Material UI
    Ant Design
    Custom (Empty Adapter)
? Where should we put the components? > ./src/components/genui

✔ Created src/components/genui/GenUIRenderer.tsx
✔ Created src/components/genui/adapters/shadcn-adapter.tsx
```

### The Generated Adapter (User Owns This Code)

```tsx
// src/components/genui/adapters/shadcn-adapter.tsx
import { Card, CardHeader, CardContent } from "@/components/ui/card";

export const ShadcnAdapter = {
  Card: ({ title, children }) => (
    <Card>
      <CardHeader>{title}</CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  ),
  // ... more mappings
};
```

### Why This Wins

1. **Zero Runtime Blackbox:** They own and can edit the adapter code.
2. **Perfect Visual Consistency:** Uses *their* buttons, *their* fonts.
3. **Supports Custom DS:** Enterprise can plug in `MyCorpUI` components.

---

## 🎯 Target Market

### Primary: Companies with Existing RAG/Chat

> *"Thousands of companies built text-only RAG chat UIs in 2023-2024. Upgrading to rich UI is expensive."*

**We are the UI upgrade path.**

| Segment | Pain Point | Our Solution |
|---------|------------|--------------|
| **Startups** | Text-only "chat with docs" | Rich cards, tables, charts |
| **SaaS** | Can't visualize structured data | Automatic component selection |
| **Enterprise** | Must use internal design system | CLI adapters + Custom DS support |

---

## 🏆 Competitive Landscape

| Competitor | Model | Our Advantage |
|------------|-------|---------------|
| **Thesys C1** | Hosted (sees API keys) | Privacy-first: keys never leave customer |
| **Vercel AI SDK** | Deep Next.js coupling | Framework-agnostic: works anywhere |
| **Chainlit** | Python + Standalone app | JS/TS native + Embeddable |

---

## 💰 Pricing Model

| Tier | Price | Includes |
|------|-------|----------|
| **Open Source** | Free | `@genui/react`, CLI, Adapters |
| **Platform** | $29/mo + usage | Hosted `/transform` API, Dashboard |
| **Enterprise** | Custom | Self-hosted, Custom adapter dev, SLA |

---

## 🗺️ Roadmap

### Phase 1: Platform MVP (Current)
- [x] Backend API (C# / .NET)
- [x] SSE streaming
- [x] Basic UI components (Card, Table, Chart)
- [ ] Platform API endpoint (`/transform`)
- [ ] Dashboard (usage analytics)

### Phase 2: Open SDK (Q1 2025)
- [ ] Extract `@genui/react` package
- [ ] Build `@genui/cli` (init, add adapter)
- [ ] Shadcn adapter template
- [ ] MUI adapter template

### Phase 3: Enterprise (Q2 2025)
- [ ] Self-hosted platform option
- [ ] Custom adapter consulting
- [ ] SOC2 / GDPR documentation

---

## 📝 Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| Dec 27, 2025 | Library-first → Hybrid model | Platform makes $, SDK builds trust |
| Dec 27, 2025 | Middleware (post-LLM) | Privacy: never need customer API keys |
| Dec 27, 2025 | CLI adapters (shadcn-style) | Enterprise wants to own code, use their DS |
| Dec 27, 2025 | Bun for development | Native TypeScript, faster builds |

---

## 🚀 The Pitch

> **For developers:** *"Add Generative UI to your RAG chat in 10 minutes. No lock-in."*
> 
> **For enterprises:** *"Your LLM, your keys, your design system. We just make it look good."*

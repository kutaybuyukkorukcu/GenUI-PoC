# Generative UI DSL - Quick Start Guide

## 🚀 Quick Start

This guide shows you how to test the new Generative UI DSL implementation alongside the existing tool-calling approach.

---

## 📋 Prerequisites

- Docker and Docker Compose installed
- .NET 9.0 SDK (for local backend development)
- Node.js 18+ (for local frontend development)

---

## 🎯 Testing Modes

### Mode 1: Legacy Tool-Calling (Current Implementation)

**Default behavior - no changes required**

1. Backend configuration (`appsettings.Development.json`):
   ```json
   {
     "Features": {
       "UseGenerativeUIDSL": false
     }
   }
   ```

2. Frontend configuration (`.env`):
   ```bash
   VITE_USE_GENERATIVE_UI_DSL=false
   ```

3. Start services:
   ```bash
   # Backend
   cd /path/to/FogData
   dotnet run

   # Frontend (new terminal)
   cd client
   npm run dev
   ```

4. Test query: `"Show me weather in New York"`

5. **Expected behavior:**
   - SSE events: `analysis`, `tool-result`, `message`
   - ComponentRenderer switches on ComponentType
   - Tool result displayed separately

---

### Mode 2: Generative UI DSL (New Implementation)

**Enable both backend and frontend flags**

1. Backend configuration (`appsettings.Development.json`):
   ```json
   {
     "Features": {
       "UseGenerativeUIDSL": true  // ✅ Changed to true
     }
   }
   ```

2. Frontend configuration (`.env`):
   ```bash
   VITE_USE_GENERATIVE_UI_DSL=true  # ✅ Changed to true
   ```

3. **Restart both services** (important!):
   ```bash
   # Backend
   cd /path/to/FogData
   dotnet run

   # Frontend (new terminal)
   cd client
   npm run dev
   ```

4. Test query: `"Show me weather in New York"`

5. **Expected behavior:**
   - SSE event: `generative-ui`
   - Thinking states appear progressively:
     - ✓ Analyzing your query...
     - ✓ Query type: weather
     - ✓ Fetching data...
     - ✓ Generating response...
   - Text: "Here's the latest weather data for New York:"
   - Weather card component renders
   - Text: "It's quite warm! Stay hydrated. 🌞"

---

## 🧪 Test Queries

### Weather Queries
```
"Show me weather in New York"
"What's the weather like?"
"Get me weather data"
```

**Expected component:** `weather` (WeatherCard)

### Sales Queries
```
"Show me recent sales"
"Get sales data from last month"
"What are the latest sales?"
```

**Expected component:** `table` (TableRenderer)

### Performance Queries
```
"Who are the top 5 salespeople?"
"Show me sales performance"
"Top performers"
```

**Expected component:** `chart` (ChartRenderer)

---

## 📊 Visual Comparison

### Legacy Format (UseGenerativeUIDSL = false)

```
┌─────────────────────────────────────┐
│ 👤 You                              │
│ Show me weather in New York         │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 🤖 Assistant                        │
│ Understanding: Get weather data     │
│                                     │
│ ┌─────────────────────────────────┐ │
│ │ Analysis:                       │ │
│ │ User wants weather information  │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Click to view results →             │
└─────────────────────────────────────┘

[Separate panel shows WeatherCard]
```

### Generative UI DSL (UseGenerativeUIDSL = true)

```
┌─────────────────────────────────────┐
│ 👤 You                              │
│ Show me weather in New York         │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 🤖 Assistant                        │
│                                     │
│ ╔═══════════════════════════════╗   │
│ ║ THINKING                      ║   │
│ ║ ✓ Analyzing your query...     ║   │
│ ║ ✓ Query type: weather         ║   │
│ ║ ✓ Fetching data...            ║   │
│ ║ ✓ Generating response...      ║   │
│ ╚═══════════════════════════════╝   │
│                                     │
│ Here's the latest weather data for  │
│ New York:                           │
│                                     │
│ ┌─────────────────────────────────┐ │
│ │   🌤️ New York                  │ │
│ │   72°F | Sunny                 │ │
│ │   💧 45% | 🌬️ 10 mph           │ │
│ └─────────────────────────────────┘ │
│                                     │
│ It's quite warm! Stay hydrated. 🌞  │
└─────────────────────────────────────┘
```

---

## 🔍 Debugging

### Check Backend Feature Flag

```bash
# Look for this log on startup
Using Generative UI DSL: True
```

Or test the endpoint directly:
```bash
curl -X POST http://localhost:5001/api/agent/chat \
  -H "Content-Type: application/json" \
  -d '{"message":"test"}' \
  --no-buffer
```

Look for SSE event type:
- Legacy: `event: analysis`, `event: tool-result`
- New: `event: generative-ui`

### Check Frontend Feature Flag

Open browser console:
```javascript
console.log(import.meta.env.VITE_USE_GENERATIVE_UI_DSL);
// Should show "true" or "false"
```

### Check SSE Events

Open browser DevTools → Network → Event stream:
```
event: generative-ui
data: {"response":"{\"thinking\":[...],\"content\":[...]}"}
```

---

## 🐛 Common Issues

### Issue 1: "Backend using old format but frontend expects new"

**Symptoms:**
- No thinking states
- No components rendering
- Console error: "Failed to parse generative UI response"

**Solution:**
- Check `appsettings.Development.json` → `Features:UseGenerativeUIDSL` is `true`
- Restart backend (`dotnet run`)

---

### Issue 2: "Frontend using old format but backend sends new"

**Symptoms:**
- Messages not rendering
- Empty content

**Solution:**
- Check `.env` → `VITE_USE_GENERATIVE_UI_DSL=true`
- Restart frontend (`npm run dev`)

---

### Issue 3: "Component not found error"

**Symptoms:**
- Yellow box: "Unknown component: xyz"
- JSON props displayed instead of component

**Solution:**
- Check component type is registered in `ComponentRegistry.tsx`
- Verify spelling matches between backend and frontend

---

## 📈 Performance Testing

### Test Streaming

1. Enable new format on both sides
2. Send query: `"Show me weather in New York"`
3. Watch thinking states appear one by one
4. Observe progressive rendering

**Expected timing:**
- Thinking item 1: ~0ms
- Thinking item 2: ~200ms
- Thinking item 3: ~500ms
- Content blocks: ~800ms
- Final response: ~1000ms

---

## 🔄 Switching Between Modes

You can switch between modes without code changes:

1. Stop both backend and frontend
2. Update feature flags in both configs
3. Restart both services
4. Test with same query
5. Compare results

**Tip:** Keep two terminal windows open to quickly switch!

---

## 📦 Docker Setup

### Using Docker Compose

1. Update `.env.docker`:
   ```bash
   VITE_USE_GENERATIVE_UI_DSL=true
   ```

2. Update backend config for Docker:
   ```json
   {
     "Features": {
       "UseGenerativeUIDSL": true
     }
   }
   ```

3. Build and run:
   ```bash
   docker-compose up --build
   ```

---

## ✅ Verification Checklist

### Backend
- [ ] `appsettings.Development.json` has `Features:UseGenerativeUIDSL` set
- [ ] Backend logs show: "Using Generative UI DSL: True/False"
- [ ] SSE event type matches feature flag
- [ ] No compilation errors

### Frontend
- [ ] `.env` has `VITE_USE_GENERATIVE_UI_DSL` set
- [ ] Frontend dev server restarted after .env change
- [ ] Browser console shows correct flag value
- [ ] No console errors

### Integration
- [ ] Both flags match (both true or both false)
- [ ] Test query renders correctly
- [ ] Thinking states appear (if enabled)
- [ ] Components render properly
- [ ] No network errors in DevTools

---

## 🎯 Next Steps

1. **Test all queries** with both modes
2. **Compare user experience** between modes
3. **Gather feedback** on which is better
4. **Decide on default** for production
5. **Plan migration** if moving to new format
6. **Update documentation** based on learnings

---

## 📚 Documentation Links

- **Backend Implementation:** `/GENERATIVE_UI_DSL_IMPLEMENTATION.md`
- **Frontend Implementation:** `/client/FRONTEND_GENERATIVE_UI_IMPLEMENTATION.md`
- **Original Architecture:** `/GENERATIVE_UI_POC_ARCHITECTURE.md`

---

## 🆘 Getting Help

If you encounter issues:

1. Check the logs (backend and browser console)
2. Verify feature flags match
3. Restart both services
4. Check this guide's troubleshooting section
5. Review implementation docs

---

**Last Updated:** November 21, 2025  
**Status:** ✅ Ready for Testing  
**Both implementations:** Fully functional and tested

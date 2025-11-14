# Generative UI PoC - Architecture Document

## 🎯 Project Vision

Build a Proof of Concept for **Generative UI** where users provide natural language commands through the UI, and the system dynamically generates appropriate UI components based on data and context from the database via an AI agent.

## 🏗️ High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                          Frontend (React)                        │
│  ┌────────────────────┐  ┌──────────────────────────────────┐  │
│  │  User Input        │  │  Dynamic Component Renderer      │  │
│  │  (Text Command)    │  │  - Chart Component               │  │
│  │                    │  │  - Table Component               │  │
│  └────────┬───────────┘  │  - Form Component                │  │
│           │              │  - Card Component (shadcn/ui)    │  │
│           │              └──────────┬───────────────────────┘  │
│           │                         │                           │
│           │                         │ Render based on          │
│           │                         │ tool call type           │
└───────────┼─────────────────────────┼───────────────────────────┘
            │                         │
            │ HTTP Request            │ Stream Response
            │ (text command)          │ (tool calls + UI data)
            ▼                         ▲
┌───────────────────────────────────────────────────────────────────┐
│                       Backend (.NET Web API)                       │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                    API Controller                          │  │
│  │  POST /api/chat                                            │  │
│  └────────────┬───────────────────────────────────────────────┘  │
│               │                                                   │
│               ▼                                                   │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                AI Agent Service                            │  │
│  │  - Vercel AI SDK Integration                              │  │
│  │  - streamText() with tools                                │  │
│  │  - Tool orchestration                                     │  │
│  └────────────┬───────────────────────────────────────────────┘  │
│               │                                                   │
│               ▼                                                   │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │              MCP Client Integration                        │  │
│  │  - Database MCP Server Connection                         │  │
│  │  - Schema discovery                                       │  │
│  │  - Tool execution                                         │  │
│  └────────────┬───────────────────────────────────────────────┘  │
│               │                                                   │
│               ▼                                                   │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                  Database Context                          │  │
│  │  - Entity Framework Core                                  │  │
│  │  - PostgreSQL / SQL Server                                │  │
│  └────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

## 📦 Technology Stack

### Frontend
- **Framework**: React 18+ with TypeScript
- **UI Library**: shadcn/ui (Tailwind CSS based components)
- **State Management**: Zustand / React Query
- **AI Integration**: Vercel AI SDK UI (`useChat` hook)
- **Build Tool**: Vite

### Backend
- **Framework**: .NET 9.0 Web API
- **AI SDK**: Vercel AI SDK Core (Node.js) OR Custom implementation
- **Database**: PostgreSQL with Entity Framework Core
- **MCP Integration**: Model Context Protocol client
- **Agent Framework**: Custom or LangChain.NET

### Infrastructure
- **Containerization**: Docker + Docker Compose
- **Database**: PostgreSQL container
- **MCP Server**: Custom database MCP server

## 🔄 Data Flow

### 1. User Input Flow
```
User types: "Show me sales data for last month as a chart"
    ↓
Frontend sends POST request to /api/chat
    ↓
Backend AI Agent receives command
    ↓
Agent analyzes intent and decides which tools to call
```

### 2. Tool Execution Flow
```
AI Agent → MCP Client → Database MCP Server
    ↓           ↓               ↓
  Decides    Discovers       Executes
  to call    available       SQL query
  tool       tools           on DB
    ↓           ↓               ↓
Receives tool result (sales data)
    ↓
Formats response with tool call metadata
```

### 3. UI Generation Flow
```
Backend streams response:
{
  type: "tool-call",
  toolName: "getChartData",
  state: "output-available",
  output: {
    type: "line-chart",
    data: [...],
    config: { xAxis: "date", yAxis: "sales" }
  }
}
    ↓
Frontend receives stream
    ↓
React component checks message.parts[].type
    ↓
Renders appropriate shadcn/ui component
    ↓
User sees generated chart
```

## 🛠️ Implementation Details

### Backend Structure (Clean Architecture)

```
FogData.Api/
├── Controllers/
│   └── ChatController.cs              # POST /api/chat endpoint
├── Services/
│   ├── IAIAgentService.cs
│   ├── AIAgentService.cs              # Core agent logic
│   ├── IMCPClientService.cs
│   └── MCPClientService.cs            # MCP integration
├── Models/
│   ├── ChatRequest.cs
│   ├── ChatResponse.cs
│   └── ToolCallResult.cs
├── Tools/
│   ├── IAgentTool.cs                  # Base tool interface
│   ├── GetChartDataTool.cs
│   ├── GetTableDataTool.cs
│   └── GetUserDataTool.cs
└── Database/
    ├── FogDataDbContext.cs
    └── Entities/
        ├── SalesData.cs
        └── UserData.cs
```

### Frontend Structure

```
client/
├── src/
│   ├── components/
│   │   ├── ui/                        # shadcn/ui components
│   │   │   ├── chart.tsx
│   │   │   ├── table.tsx
│   │   │   ├── card.tsx
│   │   │   └── form.tsx
│   │   ├── chat/
│   │   │   ├── ChatInterface.tsx
│   │   │   ├── MessageList.tsx
│   │   │   └── InputBox.tsx
│   │   └── generative/
│   │       ├── ComponentRenderer.tsx   # Routes tool calls to components
│   │       ├── ChartRenderer.tsx
│   │       ├── TableRenderer.tsx
│   │       └── FormRenderer.tsx
│   ├── hooks/
│   │   └── useGenerativeUI.ts         # Custom hook wrapping useChat
│   ├── services/
│   │   └── api.ts                     # API client
│   └── types/
│       └── tool-calls.ts              # TypeScript types for tool calls
```

## 🔧 Key Components

### 1. AI Agent Service (.NET)

```csharp
public class AIAgentService : IAIAgentService
{
    private readonly IMCPClientService _mcpClient;
    private readonly ILogger<AIAgentService> _logger;

    public async Task<Stream> ProcessChatAsync(ChatRequest request)
    {
        // 1. Get available tools from MCP server
        var tools = await _mcpClient.GetToolsAsync();
        
        // 2. Call AI model with tools (using Vercel AI SDK pattern)
        var result = await StreamTextAsync(
            model: "gpt-4o",
            messages: request.Messages,
            tools: tools,
            onToolCall: async (toolCall) =>
            {
                // 3. Execute tool via MCP
                return await _mcpClient.ExecuteToolAsync(toolCall);
            }
        );
        
        // 4. Stream response back to client
        return result.ToStream();
    }
}
```

### 2. MCP Client Service (.NET)

```csharp
public class MCPClientService : IMCPClientService
{
    private readonly HttpClient _httpClient;
    
    public async Task<List<Tool>> GetToolsAsync()
    {
        // Connect to MCP server and discover tools
        var response = await _httpClient.PostAsync(
            "http://mcp-database-server:3000/list-tools",
            null
        );
        
        return await response.Content.ReadFromJsonAsync<List<Tool>>();
    }
    
    public async Task<ToolResult> ExecuteToolAsync(ToolCall toolCall)
    {
        // Execute tool on MCP server
        var response = await _httpClient.PostAsJsonAsync(
            "http://mcp-database-server:3000/execute-tool",
            new { toolName = toolCall.Name, arguments = toolCall.Arguments }
        );
        
        return await response.Content.ReadFromJsonAsync<ToolResult>();
    }
}
```

### 3. Database MCP Server (Node.js)

```typescript
// mcp-server/server.ts
import { MCPServer } from '@modelcontextprotocol/sdk/server';
import { Pool } from 'pg';

const db = new Pool({
  connectionString: process.env.DATABASE_URL
});

const server = new MCPServer({
  name: 'database-mcp-server',
  version: '1.0.0',
});

// Register tools
server.tool({
  name: 'getChartData',
  description: 'Retrieve data for chart visualization',
  inputSchema: {
    type: 'object',
    properties: {
      table: { type: 'string' },
      dateRange: { type: 'object' },
      aggregation: { type: 'string' }
    }
  },
  execute: async ({ table, dateRange, aggregation }) => {
    const query = buildQuery(table, dateRange, aggregation);
    const result = await db.query(query);
    return {
      type: 'line-chart',
      data: result.rows,
      config: { /* chart config */ }
    };
  }
});

server.tool({
  name: 'getTableData',
  description: 'Retrieve data for table display',
  inputSchema: { /* ... */ },
  execute: async (args) => {
    // Query database and return table data
  }
});

server.start();
```

### 4. Frontend Component Renderer (React)

```typescript
// components/generative/ComponentRenderer.tsx
import { ChartRenderer } from './ChartRenderer';
import { TableRenderer } from './TableRenderer';
import { FormRenderer } from './FormRenderer';

interface ComponentRendererProps {
  message: UIMessage;
}

export const ComponentRenderer: React.FC<ComponentRendererProps> = ({ message }) => {
  return (
    <>
      {message.parts.map((part, index) => {
        if (part.type === 'text') {
          return <p key={index}>{part.text}</p>;
        }

        // Handle tool calls
        if (part.type === 'tool-getChartData') {
          switch (part.state) {
            case 'input-available':
              return <div key={index}>Loading chart...</div>;
            case 'output-available':
              return <ChartRenderer key={index} data={part.output} />;
            case 'output-error':
              return <div key={index}>Error: {part.errorText}</div>;
          }
        }

        if (part.type === 'tool-getTableData') {
          switch (part.state) {
            case 'input-available':
              return <div key={index}>Loading table...</div>;
            case 'output-available':
              return <TableRenderer key={index} data={part.output} />;
            case 'output-error':
              return <div key={index}>Error: {part.errorText}</div>;
          }
        }

        return null;
      })}
    </>
  );
};
```

### 5. Chart Renderer Component (shadcn/ui)

```typescript
// components/generative/ChartRenderer.tsx
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Line, Bar } from 'recharts';
import { ResponsiveContainer, LineChart, XAxis, YAxis, Tooltip } from 'recharts';

interface ChartRendererProps {
  data: {
    type: 'line-chart' | 'bar-chart' | 'pie-chart';
    data: any[];
    config: {
      xAxis: string;
      yAxis: string;
      title?: string;
    };
  };
}

export const ChartRenderer: React.FC<ChartRendererProps> = ({ data }) => {
  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle>{data.config.title || 'Chart'}</CardTitle>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={300}>
          <LineChart data={data.data}>
            <XAxis dataKey={data.config.xAxis} />
            <YAxis dataKey={data.config.yAxis} />
            <Tooltip />
            <Line type="monotone" dataKey={data.config.yAxis} stroke="#8884d8" />
          </LineChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
};
```

## 🚀 Implementation Phases

### Phase 1: Foundation (Week 1-2)
- ✅ Set up .NET backend with Clean Architecture
- ✅ Set up React frontend with shadcn/ui
- ✅ Implement basic chat interface
- ✅ Set up PostgreSQL database with sample data

### Phase 2: MCP Integration (Week 2-3)
- [ ] Create Database MCP Server (Node.js)
- [ ] Implement MCP client in .NET backend
- [ ] Define initial tools (getChartData, getTableData)
- [ ] Test tool discovery and execution

### Phase 3: AI Agent (Week 3-4)
- [ ] Integrate AI model (OpenAI/Anthropic)
- [ ] Implement AI Agent Service with tool orchestration
- [ ] Create streaming response pipeline
- [ ] Add error handling and retries

### Phase 4: Generative UI (Week 4-5)
- [ ] Implement Component Renderer
- [ ] Create Chart, Table, Form renderers
- [ ] Add loading states and error handling
- [ ] Implement streaming UI updates

### Phase 5: Polish & Demo (Week 5-6)
- [ ] Add more tools and components
- [ ] Improve UI/UX
- [ ] Add telemetry and logging
- [ ] Create demo scenarios
- [ ] Documentation

## 🎓 Key Insights from Vercel AI SDK

### 1. **Streaming Architecture**
- Use `streamText()` for real-time responses
- Stream tool calls as they execute
- Frontend progressively renders UI

### 2. **Tool Call Pattern**
```typescript
// Backend defines tools
const tools = {
  getChartData: tool({
    description: 'Get data for charts',
    parameters: z.object({ /* ... */ }),
    execute: async (params) => { /* ... */ }
  })
};

// Frontend handles tool states
part.type === 'tool-getChartData'
part.state === 'input-available' // Loading
part.state === 'output-available' // Render
part.state === 'output-error' // Error
```

### 3. **MCP Integration Benefits**
- **Standardized interface** for database access
- **Schema discovery** - tools auto-discovered
- **Separation of concerns** - database logic isolated
- **Scalability** - add more MCP servers easily

### 4. **Generative UI Flow**
```
User Input → AI Model → Tool Selection → Tool Execution → 
Component Selection → Dynamic Rendering
```

## 🔒 Security Considerations

1. **API Authentication**: JWT tokens for chat endpoint
2. **SQL Injection**: Parameterized queries in MCP server
3. **Rate Limiting**: Throttle AI requests per user
4. **Tool Permissions**: Restrict tools based on user role
5. **Data Privacy**: Mask sensitive data in responses

## 📊 Example User Scenarios

### Scenario 1: Sales Dashboard
**User**: "Show me a chart of sales by region for Q4 2024"

**Flow**:
1. AI Agent calls `getChartData` tool
2. MCP server queries sales database
3. Returns aggregated data
4. Frontend renders Line Chart component

### Scenario 2: User Management
**User**: "Display all active users in a table"

**Flow**:
1. AI Agent calls `getTableData` tool
2. MCP server queries users table
3. Returns user records
4. Frontend renders Table component with pagination

### Scenario 3: Form Generation
**User**: "Create a form to add a new product"

**Flow**:
1. AI Agent calls `getFormSchema` tool
2. MCP server returns product schema from DB
3. Returns form field definitions
4. Frontend renders dynamic Form component

## 🎯 Success Metrics

- **Response Time**: < 2s for tool execution
- **UI Render Time**: < 500ms for component rendering
- **Accuracy**: 90%+ correct tool selection
- **User Experience**: Smooth streaming, no flicker
- **Scalability**: Handle 100+ concurrent users

## 📚 Next Steps

1. Review this architecture with team
2. Set up development environment
3. Create initial database schema
4. Implement Phase 1 (Foundation)
5. Begin MCP server development

---

**This architecture combines the power of AI agents, MCP for standardized data access, and dynamic UI generation to create a truly intelligent, adaptive user experience.**

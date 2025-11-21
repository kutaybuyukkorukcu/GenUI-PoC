export interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

export interface AgentAnalysis {
  Intent?: string;
  ToolToCall?: string;
  Parameters?: Record<string, any>;
  Reasoning?: string;
  // Support both PascalCase and camelCase
  intent?: string;
  toolToCall?: string;
  parameters?: Record<string, any>;
  reasoning?: string;
}

export interface ToolCallResult {
  Success?: boolean;
  Data?: any;
  Error?: string;
  ComponentType?: 'weather' | 'chart' | 'table' | 'error';
  // Support both PascalCase and camelCase
  success?: boolean;
  data?: any;
  error?: string;
  componentType?: 'weather' | 'chart' | 'table' | 'error';
}

export interface WeatherData {
  // Support both PascalCase and camelCase
  Id?: number;
  id?: number;
  Location?: string;
  location?: string;
  Temperature?: number;
  temperature?: number;
  Humidity?: number;
  humidity?: number;
  WindSpeed?: number;
  windSpeed?: number;
  Condition?: string;
  condition?: string;
  Date?: string;
  date?: string;
}

export interface SalesData {
  // Support both PascalCase (from backend) and camelCase
  Id?: number;
  id?: number;
  Product?: string;
  product?: string;
  Amount?: number;
  amount?: number;
  Region?: string;
  region?: string;
  SaleDate?: string;
  saleDate?: string;
  SalesPerson?: {
    Id?: number;
    FirstName?: string;
    LastName?: string;
    Email?: string;
    Region?: string;
    Role?: string;
  };
  salesPerson?: {
    id?: number;
    firstName?: string;
    lastName?: string;
    email?: string;
    region?: string;
    role?: string;
  };
}

export interface SalesPersonPerformance {
  // Support both PascalCase and camelCase
  SalesPersonName?: string;
  salesPersonName?: string;
  TotalSales?: number;
  totalSales?: number;
  SalesCount?: number;
  salesCount?: number;
  Region?: string;
  region?: string;
}

export interface ChatMessage extends Message {
  analysis?: AgentAnalysis;
  toolResult?: ToolCallResult;
  isStreaming?: boolean;
  // New: Generative UI DSL format
  generativeUIResponse?: GenerativeUIResponse;
  isGenerativeUI?: boolean;
}

// ============================================
// Generative UI DSL Types
// ============================================

export interface ThinkingItem {
  status: 'active' | 'complete';
  message: string;
  timestamp?: string;
}

export type ContentBlock = TextBlock | ComponentBlock;

export interface TextBlock {
  type: 'text';
  value: string;
}

export interface ComponentBlock {
  type: 'component';
  componentType: 'weather' | 'chart' | 'table' | string;
  props: Record<string, any>;
}

export interface GenerativeUIResponse {
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

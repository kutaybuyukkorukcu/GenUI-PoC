export interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
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
  isStreaming?: boolean;
  // Generative UI DSL format
  generativeUIResponse?: GenerativeUIResponse;
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
  componentType: string;
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

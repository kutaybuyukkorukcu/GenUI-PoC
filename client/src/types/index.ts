export interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

export interface ChatMessage extends Message {
  isStreaming?: boolean;
  // Generative UI DSL format
  generativeUIResponse?: GenerativeUIResponse;
}

// ============================================
// Generative UI DSL Types
// Aligned with backend GenerativeUIModels.cs
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
  props: Record<string, unknown>;
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
    [key: string]: unknown;
  };
}

// ============================================
// Component Props Types - aligned with backend
// ============================================

export interface CardProps {
  title?: string;
  description?: string;
  data?: Record<string, unknown>;
}

export interface ListProps {
  title?: string;
  items: unknown[];
  layout?: 'list' | 'grid' | 'compact';
}

export interface TableProps {
  columns: string[];
  rows: Record<string, unknown>[];
}

export interface ChartProps {
  title?: string;
  chartData: Record<string, unknown>[];
}

export interface FormFieldProps {
  name: string;
  label: string;
  type: 'text' | 'number' | 'email' | 'date' | 'select' | 'textarea';
  placeholder?: string;
  required?: boolean;
  options?: string[];
  defaultValue?: string | number;
}

export interface FormProps {
  title: string;
  description?: string;
  fields: FormFieldProps[];
  submitText?: string;
}

export interface ConfirmationProps {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'info' | 'warning' | 'danger';
  data?: unknown;
}

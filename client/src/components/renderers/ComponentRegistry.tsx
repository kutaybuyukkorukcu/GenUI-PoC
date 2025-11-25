import { CardRenderer } from './CardRenderer';
import { ChartRenderer } from './ChartRenderer';
import type { ComponentBlock } from '../../types';
import { ConfirmationDialog } from './ConfirmationDialog';
import { FormRenderer } from './FormRenderer';
import { ListRenderer } from './ListRenderer';
import { TableRenderer } from './TableRenderer';
import { WeatherCard } from './WeatherCard';

/**
 * Registry mapping component types to their React implementations.
 * This enables dynamic component rendering based on componentType from the backend.
 * 
 * Generic components (card, list, table, chart) work with any data domain.
 * Domain-specific components (weather) are kept for backward compatibility.
 */
const COMPONENT_REGISTRY: Record<string, React.ComponentType<unknown>> = {
  // Generic components - work with any data
  card: CardRenderer as React.ComponentType<unknown>,
  list: ListRenderer as React.ComponentType<unknown>,
  table: TableRenderer as React.ComponentType<unknown>,
  chart: ChartRenderer as React.ComponentType<unknown>,
  
  // Interactive components
  form: FormRenderer as React.ComponentType<unknown>,
  confirmation: ConfirmationDialog as React.ComponentType<unknown>,
  
  // Legacy/domain-specific components (backward compatibility)
  weather: WeatherCard as React.ComponentType<unknown>,
};

interface DynamicComponentProps {
  block: ComponentBlock;
  sendMessage?: (message: string) => void;
}

/**
 * DynamicComponent - Renders a component based on componentType from the registry.
 * Falls back to JSON display if component type is not found.
 */
export const DynamicComponent = ({ block, sendMessage }: DynamicComponentProps) => {
  const { componentType, props } = block;
  const Component = COMPONENT_REGISTRY[componentType];

  if (!Component) {
    console.warn(`Unknown component type: ${componentType}`);
    return (
      <div className="rounded border border-yellow-500 bg-yellow-50 p-4 text-sm">
        <p className="font-semibold text-yellow-800">Unknown component: {componentType}</p>
        <pre className="mt-2 overflow-auto text-xs text-yellow-700">
          {JSON.stringify(props, null, 2)}
        </pre>
      </div>
    );
  }

  // Pass the entire props object to the component along with sendMessage
  // Our components will receive the props as their data
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const componentProps = { ...(props as Record<string, unknown>), sendMessage } as any;
  return <Component {...componentProps} />;
};

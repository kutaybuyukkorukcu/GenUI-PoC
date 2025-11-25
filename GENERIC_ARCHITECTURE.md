# Generic Generative UI Architecture

## Overview

This document describes the **domain-agnostic** architecture of the FogData Generative UI SDK. The system is designed to work with ANY data domain (finance, healthcare, e-commerce, education, etc.) by analyzing **user intent** and **data structure** rather than hardcoded business logic.

## Core Philosophy

### Before (Domain-Specific PoC):
```
User Query → Domain Detection (weather/sales) → Hardcoded Component
```
- ❌ Tied to specific domains (weather, sales, salesperson)
- ❌ New domain = new code
- ❌ Not suitable for SaaS

### After (Generic SDK):
```
User Query → Intent Analysis → Data Structure Analysis → Component Decision
```
- ✅ Works with ANY data domain
- ✅ No code changes for new domains
- ✅ Suitable for multi-tenant SaaS

## Architecture Components

### 1. Query Analyzer (`QueryAnalyzer.cs`)

**Purpose**: Determines what the user wants to do (intent) without knowing the domain.

**Intent Types**:
- `VIEW`: Retrieve and display data
- `ANALYZE`: Visualize trends, patterns, comparisons
- `COMPARE`: Side-by-side comparison
- `SEARCH`: Filter/search data
- `CREATE`: Add new data (shows form)
- `UPDATE`: Modify existing data (shows form)
- `DELETE`: Remove data (shows confirmation)

**How it works**:
1. LLM analyzes the query
2. Classifies into intent type
3. Extracts parameters (filters, dates, etc.)
4. Determines if input is required

**Example**:
```
"Show me top performers" → ANALYZE intent
"Add a new customer" → CREATE intent (requires input)
"Compare Q1 vs Q2" → COMPARE intent
```

### 2. Data Structure Analyzer (`QueryAnalyzer.cs`)

**Purpose**: Understands the shape of data to select the right visualization.

**Structure Types**:
- `SingleRecord`: One object (person, product, order)
- `Collection`: Array of similar items
- `TimeSeries`: Data with temporal dimension
- `Aggregated`: Grouped/summarized data (totals, averages)
- `Hierarchical`: Nested/tree structure

**Analysis Logic**:
```csharp
// Checks for:
- Is it a collection or single object?
- Does it have time fields? (date, timestamp)
- Is it aggregated? (total, sum, count, avg)
- How many columns/properties?
```

### 3. Component Decision Engine (`ComponentDecisionEngine.cs`)

**Purpose**: Maps intent + data structure → appropriate UI component.

**Decision Matrix**:

| Data Structure | Intent | Component |
|---|---|---|
| SingleRecord | Any | **Card** |
| TimeSeries | Analyze/Compare | **Chart (line)** |
| Aggregated | Analyze/Compare | **Chart (bar/pie)** |
| Collection (many columns) | View/Search | **Table** |
| Collection (few columns) | View/Search | **List** |
| Any | Create/Update | **Form** |

**Example Decisions**:
```typescript
// User profile → Card
{ name: "John", email: "..." } + VIEW → Card Component

// Sales over time → Line Chart
[{ date: "2024-01", sales: 100 }, ...] + ANALYZE → Chart (line)

// Top performers → Bar Chart
[{ name: "Alice", total: 5000 }, ...] + COMPARE → Chart (bar)

// Search results → Table
[{ id, name, email, phone, region }] + SEARCH → Table

// Product catalog → List/Grid
[{ name, price, image }] + VIEW → List (grid)
```

### 4. Generic Components (Frontend)

All components are **domain-agnostic** and work with any data:

#### CardRenderer
- Displays any single object
- Auto-formats fields (dates, numbers, booleans)
- Supports click actions

#### ListRenderer
- Displays any collection
- Layouts: grid, list, compact
- Extracts labels from first fields
- Supports item click actions

#### TableRenderer
- Displays tabular data
- Auto-extracts columns from data
- Sortable, filterable
- Row click actions

#### ChartRenderer
- Visualizes numeric data
- Types: line, bar, pie, area
- Auto-detects x/y axes
- Works with time-series and aggregated data

#### FormRenderer
- Generic form builder
- Field types: text, number, select, date, email
- Validation
- Stateless submit actions

## Generic vs Domain-Specific Tools

### Generic Data Access Tools

The service now uses **generic tools** that work with any entity:

```csharp
QueryData(entityType: "SalesData", filters: "...")
QueryData(entityType: "Customer", filters: "...")
QueryData(entityType: "Invoice", filters: "...")

AggregateData(entityType: "SalesData", groupBy: "region")
AggregateData(entityType: "Orders", groupBy: "status")
```

These replace domain-specific tools like:
- ❌ `GetWeatherData()` 
- ❌ `GetSalesData()`
- ❌ `GetTopSalesPeople()`

### Domain-Specific Business Logic

Domain-specific logic is kept in:
1. **Database Entities** (`Person`, `SalesData`, `WeatherData`)
2. **Action Handlers** (`CreateSale`, `CreatePerson`)
3. **Helper Methods** (for complex queries specific to the domain)

This is normal and expected - every app has business logic. The key is that the **UI generation** is generic.

## Data Flow

### View/Analyze Flow
```
1. User: "Show me sales trends"
2. QueryAnalyzer → Intent: ANALYZE
3. LLM Tool Calling → Fetches sales data
4. DataAnalyzer → Structure: TimeSeries (has date field)
5. ComponentEngine → Decision: Chart (line)
6. Response: Chart component with data
```

### Create Flow
```
1. User: "Add a new customer"
2. QueryAnalyzer → Intent: CREATE (requiresInput: true)
3. Form Builder → Analyzes entity, creates form fields
4. Response: Form component
5. User submits → ActionsController handles business logic
```

## Benefits of Generic Architecture

### For SaaS Product:
- ✅ **Multi-tenant**: Same code serves different industries
- ✅ **Scalable**: Add new data types without code changes
- ✅ **Maintainable**: One component library for all use cases
- ✅ **Flexible**: Customers can query any data

### For SDK Users:
- ✅ **Simple**: Just connect your database entities
- ✅ **Powerful**: Automatic UI generation for any query
- ✅ **Customizable**: Override decisions when needed
- ✅ **Fast**: No frontend code needed

### For Development:
- ✅ **DRY**: No duplicate code for each domain
- ✅ **Testable**: Generic components are easier to test
- ✅ **Extensible**: Add new component types easily

## Example Use Cases

### E-commerce Platform
```
"Show me best-selling products" → Bar Chart
"Search for orders in California" → Table
"Show customer profile for john@example.com" → Card
"Add a new product" → Form
```

### Healthcare System
```
"Show patient vital trends" → Line Chart
"List all appointments today" → List
"Compare treatment outcomes" → Bar Chart
"Register new patient" → Form
```

### Financial Dashboard
```
"Show revenue over time" → Line Chart
"List all transactions" → Table
"Compare expense categories" → Pie Chart
"Add new invoice" → Form
```

### Education Platform
```
"Show student performance" → Bar Chart
"List all courses" → Grid List
"View student details" → Card
"Enroll new student" → Form
```

## Migration Path

If you have domain-specific code in your PoC:

1. **Keep domain entities** - They're specific to your app
2. **Keep action handlers** - Business logic is domain-specific
3. **Replace UI generation** - Use generic component system
4. **Update prompts** - Remove domain-specific instructions
5. **Test with different domains** - Ensure it generalizes

## Configuration

The generic system can be configured:

```csharp
// Customize component decisions
ComponentDecisionEngine.Configure(config => {
    config.PreferTableOverListWhenColumnsExceed = 5;
    config.PreferChartForTimeSeriesWithMinimumPoints = 3;
    config.DefaultChartType = "bar";
});

// Add custom component mappings
ComponentRegistry.Register("custom-widget", CustomWidgetRenderer);

// Override intent classification
QueryAnalyzer.AddCustomIntentRule("schedule", QueryIntentType.Create);
```

## Best Practices

1. **Let the system decide**: Trust the generic decision engine
2. **Override when needed**: Custom components for special cases
3. **Test across domains**: Ensure your data works generically
4. **Provide good data**: Clean, structured data = better UI
5. **Use semantic naming**: Field names like `createdAt` vs `dt` help analysis

## Conclusion

This architecture transforms FogData from a **domain-specific PoC** into a **generic SaaS SDK** that can serve any industry. The system analyzes intent and data structure to make intelligent UI decisions without hardcoded business logic.

**Key Principle**: *"The SDK understands WHAT to show (card, chart, table) by analyzing HOW users want to interact with data, not WHAT the data represents (sales, weather, patients)."*

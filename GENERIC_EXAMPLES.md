# Generic UI Examples - Before & After

This document shows how the same user queries are handled differently in the generic architecture.

## Example 1: View Data

### Query: "Show me recent sales"

**Before (Domain-Specific):**
```
1. Keyword match: "sales" → QueryType = "sales"
2. Hardcoded: FetchSalesData()
3. Hardcoded: BuildSalesResponseAsync() → Table
4. Result: Always a table for sales data
```

**After (Generic):**
```
1. Intent Analysis: "show me" → QueryIntentType.View
2. LLM Tool Call: QueryData(entityType: "SalesData")
3. Data Structure: Collection with 5+ columns
4. Component Decision: Collection + View → Table
5. Result: Generic table component with sales data
```

**Benefit**: Same logic works for "Show me recent orders", "Show me customers", "Show me appointments"

---

## Example 2: Analyze Performance

### Query: "Who are the top performers?"

**Before (Domain-Specific):**
```
1. Keyword match: "top" + "performers" → QueryType = "performance"
2. Hardcoded: GetTopSalesPeople()
3. Hardcoded: BuildPerformanceResponseAsync() → Bar Chart
4. Result: Always a bar chart for performance
```

**After (Generic):**
```
1. Intent Analysis: "top" → QueryIntentType.Analyze
2. LLM Tool Call: AggregateData(entityType: "SalesData", groupBy: "salesperson")
3. Data Structure: Aggregated data (has Total, Count fields)
4. Component Decision: Aggregated + Analyze → Chart (bar)
5. Result: Generic chart component with performance data
```

**Benefit**: Same logic works for "top products", "best customers", "highest revenues by region"

---

## Example 3: View Single Record

### Query: "Show me details for customer john@example.com"

**Before (Domain-Specific):**
```
❌ Not handled - would need new domain-specific code
```

**After (Generic):**
```
1. Intent Analysis: "show details" → QueryIntentType.View
2. LLM Tool Call: QueryData(entityType: "Person", filters: {email: "john@..."})
3. Data Structure: SingleRecord
4. Component Decision: SingleRecord + Any → Card
5. Result: Generic card component with customer details
```

**Benefit**: Works for ANY single record query across all domains

---

## Example 4: Time Series Analysis

### Query: "Show me sales trends over the last 6 months"

**Before (Domain-Specific):**
```
1. Keyword match: "sales" → QueryType = "sales"
2. Hardcoded: GetSalesData() with date filters
3. Hardcoded: BuildSalesResponseAsync() → Table (wrong for trends!)
4. Result: Table when user wanted a chart
```

**After (Generic):**
```
1. Intent Analysis: "trends" → QueryIntentType.Analyze
2. LLM Tool Call: QueryData(entityType: "SalesData", filters: {last6Months: true})
3. Data Structure: TimeSeries (has date field, multiple records)
4. Component Decision: TimeSeries + Analyze → Chart (line)
5. Result: Generic line chart showing trends
```

**Benefit**: Automatically selects chart for time-series, works for "revenue trends", "patient visits over time", etc.

---

## Example 5: Create New Record

### Query: "Add a new customer"

**Before (Domain-Specific):**
```
1. Keyword match: "add" + "person" → QueryType = "add-data"
2. Keyword check: if "person" → BuildAddPersonFormAsync()
3. Hardcoded form fields for Person entity
4. Result: Person-specific form
```

**After (Generic):**
```
1. Intent Analysis: "add new" → QueryIntentType.Create (requiresInput: true)
2. Entity Detection: "customer" → Person entity
3. Form Builder: Analyzes Person entity, generates fields
4. Component Decision: Create + RequiresInput → Form
5. Result: Generic form component with Person fields
```

**Benefit**: Same logic generates forms for customers, products, orders, invoices, etc.

---

## Example 6: Search/Filter

### Query: "Find all sales in California over $1000"

**Before (Domain-Specific):**
```
1. Keyword match: "sales" → QueryType = "sales"
2. Hardcoded: GetSalesData(region: "California", amount > 1000)
3. Hardcoded: BuildSalesResponseAsync() → Table
4. Result: Table with filtered sales
```

**After (Generic):**
```
1. Intent Analysis: "find all" → QueryIntentType.Search
2. LLM Tool Call: QueryData(entityType: "SalesData", filters: {region: "CA", amount: {gt: 1000}})
3. Data Structure: Collection with many columns
4. Component Decision: Collection + Search → Table (sortable, filterable)
5. Result: Generic table with built-in search
```

**Benefit**: Works for ANY search query: "Find orders shipped to NY", "Find patients with condition X"

---

## Example 7: Compare Items

### Query: "Compare Q1 vs Q2 revenue"

**Before (Domain-Specific):**
```
❌ Not explicitly handled - would show table
```

**After (Generic):**
```
1. Intent Analysis: "compare" → QueryIntentType.Compare
2. LLM Tool Call: AggregateData(entityType: "SalesData", groupBy: "quarter")
3. Data Structure: Aggregated with 2 groups
4. Component Decision: Aggregated + Compare → Chart (bar)
5. Result: Side-by-side bar chart
```

**Benefit**: Works for ANY comparison: "Compare products", "North vs South sales", "Treatment outcomes"

---

## Example 8: List View

### Query: "Show me all products"

**Before (Domain-Specific):**
```
❌ Not handled - products not in PoC
❌ Would need new domain-specific code
```

**After (Generic):**
```
1. Intent Analysis: "show all" → QueryIntentType.View
2. LLM Tool Call: QueryData(entityType: "Product")
3. Data Structure: Collection with 3-4 columns (name, price, category)
4. Component Decision: Collection (few columns) + View → List (grid)
5. Result: Generic grid list component
```

**Benefit**: Automatically uses grid for visual items, table for data-heavy items

---

## Cross-Domain Examples

### Healthcare: "Show patient vital trends"
```
Intent: Analyze
Data: TimeSeries (date + vital measurements)
Component: Chart (line)
Result: Line chart showing temperature/BP/heart rate over time
```

### E-commerce: "Best selling products this month"
```
Intent: Analyze
Data: Aggregated (product + total sales)
Component: Chart (bar)
Result: Bar chart of products ranked by sales
```

### Finance: "List all pending invoices"
```
Intent: View
Data: Collection (invoice details, 6+ columns)
Component: Table
Result: Sortable table of invoices
```

### Education: "Show student performance"
```
Intent: Analyze
Data: Aggregated (student + grade/score)
Component: Chart (bar)
Result: Performance bar chart
```

### HR System: "View employee profile"
```
Intent: View
Data: SingleRecord (employee object)
Component: Card
Result: Card showing name, role, department, contact
```

---

## Key Differences

| Aspect | Before (PoC) | After (Generic) |
|--------|--------------|-----------------|
| **Query Routing** | Keyword matching | LLM intent analysis |
| **Data Fetching** | Domain-specific methods | Generic tools + LLM |
| **Component Selection** | Hardcoded per domain | Algorithm based on structure |
| **Extensibility** | New code per domain | Works automatically |
| **Maintainability** | Many similar methods | Single generic flow |
| **Flexibility** | Limited to weather/sales | Any data domain |

---

## The Generic Promise

**One Architecture, Infinite Domains**

No matter what data you have:
- 📊 **Analytics platform** → Automatic charts for metrics
- 🛒 **E-commerce** → Cards for products, tables for orders
- 🏥 **Healthcare** → Charts for vitals, cards for patients
- 💰 **Finance** → Tables for transactions, charts for trends
- 📚 **Education** → Lists for courses, charts for grades

**The system understands:**
- ✅ What users want to DO (view, analyze, create)
- ✅ What shape the DATA has (single, list, time-series)
- ✅ Which COMPONENT fits best (card, list, table, chart)

**Without knowing:**
- ❌ Your business domain
- ❌ Your entity names
- ❌ Your industry

This is the power of generic Generative UI.

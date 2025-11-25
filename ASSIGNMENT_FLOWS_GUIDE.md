# Assignment Flows - Interactive UI Components Guide

## Overview

This guide explains how to use the new interactive UI components for assignment flows: **ConfirmationDialog** and **FormRenderer**. These components enable the LLM to request user input, collect additional context, and confirm actions before executing them.

---

## 🎯 Use Cases

### 1. ConfirmationDialog
- Confirm before adding/editing/deleting data
- Display a summary of changes before applying
- Show warnings before potentially destructive actions
- Get user approval for LLM-suggested assignments

### 2. FormRenderer
- Collect additional information needed to complete a task
- Ask clarifying questions when context is insufficient
- Gather required fields for database operations
- Multi-step workflows where LLM needs user input

---

## 📦 Component Types

### ConfirmationDialog

**Component Type:** `confirmation`

**Props:**
```typescript
{
  title: string;              // Dialog title
  message: string;            // Confirmation message
  confirmText?: string;       // Text for confirm button (default: "Confirm")
  cancelText?: string;        // Text for cancel button (default: "Cancel")
  variant?: 'info' | 'warning' | 'danger';  // Visual style
  data?: Record<string, unknown>;  // Key-value pairs to display
}
```

**Example C# Backend Code:**

```csharp
// Example: Confirm adding a new sales record
var response = builder
    .AddThinking("Analyzing your request...", ThinkingStatus.Complete)
    .AddThinking("Found all required information", ThinkingStatus.Complete)
    .AddText("I found the following information for the new sales record:")
    .AddComponent("confirmation", new
    {
        title = "Add New Sale",
        message = "Are you sure you want to add this sales record to the database?",
        confirmText = "Yes, Add Sale",
        cancelText = "Cancel",
        variant = "info",
        data = new Dictionary<string, object>
        {
            { "Product", "Laptop Pro 15" },
            { "Amount", 1299.99 },
            { "Region", "North America" },
            { "Salesperson", "John Smith" },
            { "Date", "Nov 24, 2025" }
        }
    })
    .Build();
```

**Example: Warning before deletion**
```csharp
var response = builder
    .AddThinking("Understanding request...", ThinkingStatus.Complete)
    .AddText("⚠️ This action cannot be undone.")
    .AddComponent("confirmation", new
    {
        title = "Delete Sales Record",
        message = "You are about to permanently delete this sales record. This action cannot be reversed.",
        confirmText = "Yes, Delete",
        cancelText = "Keep Record",
        variant = "danger",
        data = new Dictionary<string, object>
        {
            { "Record ID", 12345 },
            { "Product", "Laptop Pro 15" },
            { "Amount", "$1,299.99" },
            { "Date", "Nov 15, 2025" }
        }
    })
    .Build();
```

---

### FormRenderer

**Component Type:** `form`

**Props:**
```typescript
{
  title: string;
  description?: string;
  fields: Array<{
    name: string;              // Field identifier
    label: string;             // Display label
    type: 'text' | 'number' | 'email' | 'date' | 'select' | 'textarea';
    placeholder?: string;
    required?: boolean;
    options?: string[];        // For select type
    defaultValue?: string | number;
  }>;
  submitText?: string;         // Submit button text (default: "Submit")
}
```

**Example C# Backend Code:**

```csharp
// Example: Requesting additional info for project assignment
var response = builder
    .AddThinking("Analyzing project requirements...", ThinkingStatus.Complete)
    .AddThinking("Need more context about the assignment", ThinkingStatus.Complete)
    .AddText("To assign the best person to this project, I need some additional information:")
    .AddComponent("form", new
    {
        title = "Project Assignment Details",
        description = "Please provide the following information to help me find the best match",
        submitText = "Find Best Match",
        fields = new[]
        {
            new
            {
                name = "projectName",
                label = "Project Name",
                type = "text",
                placeholder = "e.g., Mobile App Redesign",
                required = true
            },
            new
            {
                name = "skills",
                label = "Required Skills",
                type = "select",
                required = true,
                options = new[] { "Frontend", "Backend", "Full Stack", "DevOps", "Design" }
            },
            new
            {
                name = "duration",
                label = "Project Duration (weeks)",
                type = "number",
                placeholder = "e.g., 8",
                required = true
            },
            new
            {
                name = "priority",
                label = "Priority Level",
                type = "select",
                required = true,
                options = new[] { "Low", "Medium", "High", "Critical" }
            },
            new
            {
                name = "additionalNotes",
                label = "Additional Requirements",
                type = "textarea",
                placeholder = "Any specific requirements or preferences...",
                required = false
            }
        }
    })
    .Build();
```

**Example: Missing information for sales record**
```csharp
var response = builder
    .AddThinking("Analyzing sales data request...", ThinkingStatus.Complete)
    .AddText("I need a few more details to create this sales record:")
    .AddComponent("form", new
    {
        title = "Complete Sales Information",
        description = "Please fill in the missing information",
        submitText = "Add Sale",
        fields = new[]
        {
            new
            {
                name = "product",
                label = "Product Name",
                type = "select",
                required = true,
                options = new[] 
                { 
                    "Laptop Pro 15", 
                    "Mechanical Keyboard", 
                    "Wireless Mouse", 
                    "Webcam HD", 
                    "USB-C Hub" 
                }
            },
            new
            {
                name = "amount",
                label = "Sale Amount ($)",
                type = "number",
                placeholder = "e.g., 1299.99",
                required = true
            },
            new
            {
                name = "region",
                label = "Sales Region",
                type = "select",
                required = true,
                options = new[] 
                { 
                    "North America", 
                    "Europe", 
                    "Asia Pacific", 
                    "South America" 
                }
            },
            new
            {
                name = "salesperson",
                label = "Salesperson Name",
                type = "text",
                placeholder = "e.g., John Smith",
                required = true
            },
            new
            {
                name = "saleDate",
                label = "Sale Date",
                type = "date",
                required = true,
                defaultValue = DateTime.Today.ToString("yyyy-MM-dd")
            }
        }
    })
    .Build();
```

---

## 🔄 Complete Flow Example: Project Assignment

### Step 1: User Request
```
User: "Assign someone to the new mobile app project"
```

### Step 2: LLM Requests Details
```csharp
var response = builder
    .AddThinking("Understanding assignment request...", ThinkingStatus.Complete)
    .AddThinking("Need project details", ThinkingStatus.Complete)
    .AddText("I'll help you find the best person for this project. First, I need some details:")
    .AddComponent("form", new
    {
        title = "Project Information",
        description = "Tell me about the project requirements",
        fields = new[]
        {
            new
            {
                name = "skillLevel",
                label = "Required Skill Level",
                type = "select",
                required = true,
                options = new[] { "Junior", "Mid-level", "Senior", "Lead" }
            },
            new
            {
                name = "primarySkill",
                label = "Primary Skill Needed",
                type = "select",
                required = true,
                options = new[] { "React Native", "Flutter", "iOS Native", "Android Native" }
            },
            new
            {
                name = "startDate",
                label = "Project Start Date",
                type = "date",
                required = true
            }
        }
    })
    .Build();
```

### Step 3: User Submits Form
(Frontend sends form data back to backend in the next message)

### Step 4: LLM Analyzes and Suggests
```csharp
// Backend receives: skillLevel="Senior", primarySkill="React Native", startDate="2025-12-01"
// LLM queries database and finds best match

var response = builder
    .AddThinking("Analyzing team availability...", ThinkingStatus.Complete)
    .AddThinking("Matching skills with project needs...", ThinkingStatus.Complete)
    .AddThinking("Found best candidate", ThinkingStatus.Complete)
    .AddText("Based on your requirements, I found the perfect match:")
    .AddComponent("table", new
    {
        columns = new[] { "Name", "Skill", "Level", "Availability", "Match Score" },
        rows = new[]
        {
            new
            {
                name = "Sarah Chen",
                skill = "React Native",
                level = "Senior",
                availability = "Available",
                matchScore = "98%"
            }
        }
    })
    .AddComponent("confirmation", new
    {
        title = "Assign Sarah Chen to Project",
        message = "Sarah Chen has 5 years of React Native experience and is currently available. Assign her to this project?",
        confirmText = "Yes, Assign",
        cancelText = "Find Another",
        variant = "info",
        data = new Dictionary<string, object>
        {
            { "Developer", "Sarah Chen" },
            { "Primary Skill", "React Native" },
            { "Experience", "5 years" },
            { "Current Projects", "1 (Low priority)" },
            { "Availability", "Immediate" }
        }
    })
    .Build();
```

### Step 5: User Confirms
(Frontend handles confirmation, sends confirmation back to backend)

### Step 6: LLM Executes Assignment
```csharp
// Backend receives confirmation
// Execute database update
await AssignDeveloperToProject("Sarah Chen", projectId);

var response = builder
    .AddThinking("Creating assignment...", ThinkingStatus.Complete)
    .AddThinking("Updating project records...", ThinkingStatus.Complete)
    .AddThinking("Notifying team members...", ThinkingStatus.Complete)
    .AddText("✅ Assignment complete! Sarah Chen has been assigned to the mobile app project.")
    .AddText("A notification has been sent to Sarah and the project manager.")
    .Build();
```

---

## 🎨 Visual Variants

### ConfirmationDialog Variants

**Info (default):**
- Blue icon
- Used for normal confirmations
- Example: "Add this record?"

**Warning:**
- Yellow icon  
- Used for potentially risky actions
- Example: "This will modify 50 records"

**Danger:**
- Red icon
- Red confirm button
- Used for destructive actions
- Example: "Delete permanently?"

---

## 🛠️ Implementation Notes

### Frontend
- Both components are registered in `ComponentRegistry.tsx`
- Component type `confirmation` → `ConfirmationDialog`
- Component type `form` → `FormRenderer`
- Forms automatically validate required fields
- Form data is collected and can be sent back to the backend

### Backend
- Use `GenerativeUIResponseBuilder` to construct responses
- Component props should match the TypeScript interfaces
- Use anonymous objects for clean JSON serialization
- Combine components with text and thinking blocks for better UX

---

## ✅ Best Practices

1. **Always provide context**: Use text blocks before forms/confirmations to explain why you're asking

2. **Show thinking states**: Let users see the AI's reasoning process

3. **Validate on both sides**: 
   - Frontend validates required fields
   - Backend should also validate submitted data

4. **Use appropriate variants**:
   - `info` for normal operations
   - `warning` for caution needed
   - `danger` for destructive actions

5. **Combine components**: Mix tables, forms, and confirmations for rich workflows

6. **Provide defaults**: Use `defaultValue` in forms when possible to reduce user effort

7. **Clear labels**: Make field labels descriptive and user-friendly

---

## 📝 Example Queries to Try

**Sales Assignment:**
```
"Add a new sale for John Smith in the North America region"
→ LLM asks for missing details via form
→ User fills product, amount, date
→ LLM shows confirmation with summary
→ User confirms
→ Record added
```

**Staff Scheduling:**
```
"Schedule the best developer for the Q1 mobile project"
→ LLM asks for project details via form
→ User provides skill requirements, timeline
→ LLM queries database and shows top match
→ Shows confirmation with candidate details
→ User confirms assignment
```

**Data Modification:**
```
"Update the sales amount for record #12345 to $2000"
→ LLM shows current vs new values
→ Displays confirmation dialog with both values
→ User confirms
→ Record updated
```

---

**Created:** November 24, 2025  
**Components:** ConfirmationDialog, FormRenderer  
**Status:** ✅ Ready to use

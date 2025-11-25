# Backend Examples - Assignment Flow Components

This file contains C# code examples for using the new assignment flow components with your existing `GenerativeUIResponseBuilder`.

---

## Quick Reference

```csharp
// Import
using FogData.Services.GenerativeUI;

// Initialize
var builder = new GenerativeUIResponseBuilder();

// Add components
builder.AddComponent("confirmation", new { ... });
builder.AddComponent("form", new { ... });

// Build response
var jsonResponse = builder.Build();
```

---

## Example 1: Simple Confirmation Before Adding Data

```csharp
private async Task<string> ConfirmAddSaleAsync(string userMessage)
{
    var builder = new GenerativeUIResponseBuilder();
    
    // Show thinking process
    builder.AddThinkingItem("Analyzing your request...", "complete");
    builder.AddThinkingItem("Parsing sale details...", "complete");
    
    // Add explanatory text
    builder.AddText("I've extracted the following information from your request:");
    
    // Show confirmation dialog
    builder.AddComponent("confirmation", new
    {
        title = "Add New Sale",
        message = "Would you like to add this sale to the database?",
        confirmText = "Yes, Add Sale",
        cancelText = "Cancel",
        variant = "info",
        data = new Dictionary<string, object>
        {
            { "Product", "Laptop Pro 15" },
            { "Amount", "$1,299.99" },
            { "Region", "North America" },
            { "Salesperson", "John Smith" },
            { "Date", DateTime.Today.ToString("MMM dd, yyyy") }
        }
    });
    
    builder.AddMetadata("queryType", "sales");
    builder.AddMetadata("modelUsed", "Ollama");
    
    return builder.Build();
}
```

---

## Example 2: Form to Collect Missing Information

```csharp
private async Task<string> RequestMissingSalesInfoAsync(string userMessage)
{
    var builder = new GenerativeUIResponseBuilder();
    
    builder.AddThinkingItem("Understanding your request...", "complete");
    builder.AddThinkingItem("Identifying missing information...", "complete");
    
    builder.AddText("I need a few more details to create this sales record:");
    
    builder.AddComponent("form", new
    {
        title = "Complete Sales Information",
        description = "Please provide the missing details",
        submitText = "Create Sale",
        fields = new[]
        {
            new
            {
                name = "product",
                label = "Product",
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
                placeholder = "1299.99",
                required = true
            },
            new
            {
                name = "region",
                label = "Region",
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
                label = "Salesperson",
                type = "text",
                placeholder = "e.g., John Smith",
                required = true
            },
            new
            {
                name = "date",
                label = "Sale Date",
                type = "date",
                required = true,
                defaultValue = DateTime.Today.ToString("yyyy-MM-dd")
            }
        }
    });
    
    builder.AddMetadata("queryType", "sales");
    builder.AddMetadata("awaitingUserInput", true);
    
    return builder.Build();
}
```

---

## Example 3: Multi-Step Flow - Project Assignment

```csharp
// Step 1: Ask for project details
private async Task<string> RequestProjectDetailsAsync()
{
    var builder = new GenerativeUIResponseBuilder();
    
    builder.AddThinkingItem("Understanding assignment request...", "complete");
    builder.AddThinkingItem("Preparing to collect requirements...", "complete");
    
    builder.AddText("I'll help you assign the best person to this project. First, let me gather some details:");
    
    builder.AddComponent("form", new
    {
        title = "Project Requirements",
        description = "Tell me about what you need",
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
                name = "skillLevel",
                label = "Required Skill Level",
                type = "select",
                required = true,
                options = new[] { "Junior", "Mid-level", "Senior", "Lead" }
            },
            new
            {
                name = "primarySkill",
                label = "Primary Skill",
                type = "select",
                required = true,
                options = new[] 
                { 
                    "React", 
                    "Vue", 
                    "Angular", 
                    ".NET", 
                    "Node.js", 
                    "Python" 
                }
            },
            new
            {
                name = "duration",
                label = "Project Duration (weeks)",
                type = "number",
                placeholder = "8",
                required = true
            },
            new
            {
                name = "startDate",
                label = "Start Date",
                type = "date",
                required = true,
                defaultValue = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd")
            }
        }
    });
    
    builder.AddMetadata("queryType", "assignment");
    builder.AddMetadata("step", "collectRequirements");
    
    return builder.Build();
}

// Step 2: Show matches and ask for confirmation
private async Task<string> ShowMatchAndConfirmAsync(
    string projectName, 
    string skillLevel, 
    string primarySkill)
{
    // Query database for best match
    var bestMatch = await FindBestDeveloperAsync(skillLevel, primarySkill);
    
    var builder = new GenerativeUIResponseBuilder();
    
    builder.AddThinkingItem("Analyzing team skills...", "complete");
    builder.AddThinkingItem("Checking availability...", "complete");
    builder.AddThinkingItem("Found perfect match!", "complete");
    
    builder.AddText($"Based on your requirements for {projectName}, here's my recommendation:");
    
    // Show candidate details in a table
    builder.AddComponent("table", new
    {
        columns = new[] { "Name", "Skill", "Level", "Current Load", "Match Score" },
        rows = new[]
        {
            new
            {
                name = bestMatch.Name,
                skill = bestMatch.PrimarySkill,
                level = bestMatch.Level,
                currentLoad = $"{bestMatch.CurrentProjects} projects",
                matchScore = $"{bestMatch.MatchScore}%"
            }
        }
    });
    
    // Add confirmation dialog
    builder.AddComponent("confirmation", new
    {
        title = $"Assign {bestMatch.Name} to {projectName}",
        message = $"{bestMatch.Name} has extensive {primarySkill} experience and is currently available. Would you like to proceed with this assignment?",
        confirmText = "Yes, Assign",
        cancelText = "Find Another",
        variant = "info",
        data = new Dictionary<string, object>
        {
            { "Developer", bestMatch.Name },
            { "Primary Skill", bestMatch.PrimarySkill },
            { "Years of Experience", bestMatch.YearsExperience },
            { "Current Projects", bestMatch.CurrentProjects },
            { "Availability", bestMatch.IsAvailable ? "Immediate" : "2 weeks" },
            { "Match Confidence", $"{bestMatch.MatchScore}%" }
        }
    });
    
    builder.AddMetadata("queryType", "assignment");
    builder.AddMetadata("step", "confirmation");
    builder.AddMetadata("candidateId", bestMatch.Id);
    
    return builder.Build();
}
```

---

## Example 4: Warning Before Deletion

```csharp
private async Task<string> WarnBeforeDeleteAsync(int recordId)
{
    // Fetch record details
    var record = await _dbContext.SalesData
        .Include(s => s.SalesPerson)
        .FirstOrDefaultAsync(s => s.Id == recordId);
    
    if (record == null)
    {
        // Handle not found
        return BuildErrorResponse("Record not found");
    }
    
    var builder = new GenerativeUIResponseBuilder();
    
    builder.AddThinkingItem("Locating record...", "complete");
    builder.AddThinkingItem("Validating permissions...", "complete");
    
    builder.AddText("⚠️ **Warning:** This action cannot be undone!");
    
    builder.AddComponent("confirmation", new
    {
        title = "Delete Sales Record",
        message = "You are about to permanently delete this sales record. All associated data will be lost. Are you absolutely sure?",
        confirmText = "Yes, Delete Permanently",
        cancelText = "Cancel",
        variant = "danger", // Red styling for destructive action
        data = new Dictionary<string, object>
        {
            { "Record ID", record.Id },
            { "Product", record.Product },
            { "Amount", $"${record.Amount:N2}" },
            { "Salesperson", $"{record.SalesPerson.FirstName} {record.SalesPerson.LastName}" },
            { "Region", record.Region },
            { "Date", record.SaleDate.ToString("MMM dd, yyyy") }
        }
    });
    
    builder.AddMetadata("queryType", "delete");
    builder.AddMetadata("recordId", recordId);
    builder.AddMetadata("requiresConfirmation", true);
    
    return builder.Build();
}
```

---

## Example 5: Combining Multiple Components

```csharp
private async Task<string> CompleteAssignmentFlowAsync(string userMessage)
{
    var builder = new GenerativeUIResponseBuilder();
    
    // Thinking process
    builder.AddThinkingItem("Analyzing request...", "complete");
    builder.AddThinkingItem("Checking current assignments...", "complete");
    builder.AddThinkingItem("Preparing recommendation...", "complete");
    
    // Introduction
    builder.AddText("Here's a summary of the current team workload:");
    
    // Show current state with a table
    builder.AddComponent("table", new
    {
        columns = new[] { "Developer", "Current Projects", "Skill", "Availability" },
        rows = new[]
        {
            new { developer = "Sarah Chen", currentProjects = 2, skill = "React", availability = "High" },
            new { developer = "John Smith", currentProjects = 3, skill = ".NET", availability = "Medium" },
            new { developer = "Emma Johnson", currentProjects = 1, skill = "Vue", availability = "High" }
        }
    });
    
    builder.AddText("Based on the team's current capacity, I recommend Sarah Chen for this project.");
    
    // Ask for additional context if needed
    builder.AddComponent("form", new
    {
        title = "Project Preferences",
        description = "Any specific requirements?",
        submitText = "Confirm Assignment",
        fields = new[]
        {
            new
            {
                name = "urgency",
                label = "How urgent is this?",
                type = "select",
                required = true,
                options = new[] { "Low", "Medium", "High", "Critical" }
            },
            new
            {
                name = "notes",
                label = "Additional Notes",
                type = "textarea",
                placeholder = "Any specific requirements or constraints...",
                required = false
            }
        }
    });
    
    builder.AddMetadata("queryType", "assignment");
    builder.AddMetadata("recommendedDeveloper", "Sarah Chen");
    
    return builder.Build();
}
```

---

## Integration with GenerativeUIService

Add these methods to your `GenerativeUIService.cs`:

```csharp
private async Task BuildAssignmentResponseAsync(
    GenerativeUIResponseBuilder builder, 
    string userMessage)
{
    // Detect intent
    var intent = DetermineAssignmentIntent(userMessage);
    
    switch (intent)
    {
        case "needsForm":
            // User didn't provide enough info
            await AddAssignmentFormAsync(builder, userMessage);
            break;
            
        case "readyToConfirm":
            // All info available, show confirmation
            await AddAssignmentConfirmationAsync(builder, userMessage);
            break;
            
        case "execute":
            // User confirmed, execute the assignment
            await ExecuteAssignmentAsync(builder, userMessage);
            break;
    }
}

private async Task AddAssignmentFormAsync(
    GenerativeUIResponseBuilder builder,
    string userMessage)
{
    builder.AddThinkingItem("Understanding assignment request...", "complete");
    builder.AddText("I need more details to make the best assignment:");
    
    builder.AddComponent("form", new
    {
        title = "Assignment Details",
        description = "Help me find the perfect match",
        fields = new[]
        {
            new
            {
                name = "skill",
                label = "Required Skill",
                type = "select",
                required = true,
                options = new[] { "React", "Vue", ".NET", "Python", "Node.js" }
            },
            new
            {
                name = "duration",
                label = "Duration (weeks)",
                type = "number",
                required = true
            }
        }
    });
}
```

---

## Handling User Responses

When the user submits a form or clicks confirm/cancel, they send a new message. You can detect this:

```csharp
// In your message handler
if (IsFormSubmission(userMessage))
{
    var formData = ParseFormData(userMessage);
    // Use formData to proceed with next step
    return await ProcessFormSubmissionAsync(formData);
}

if (IsConfirmation(userMessage))
{
    // User clicked "Confirm"
    return await ExecuteConfirmedActionAsync();
}

if (IsCancellation(userMessage))
{
    // User clicked "Cancel"
    return await HandleCancellationAsync();
}
```

---

## Best Practices

1. **Always show thinking states** - Users like transparency
2. **Validate inputs** - Both client and server side
3. **Provide defaults** - Makes forms easier to fill
4. **Use appropriate variants** - danger for deletions, warning for risky operations
5. **Combine components** - Tables + Confirmations work great together
6. **Add metadata** - Track conversation state for multi-step flows

---

**Note:** These are examples. Adapt them to your specific domain models and business logic.

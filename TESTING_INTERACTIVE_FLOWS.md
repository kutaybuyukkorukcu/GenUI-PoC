# Testing Interactive Assignment Flows

## Overview
Your app now supports interactive forms and confirmations that collect user input and update the database.

---

## How It Works

### 3-Step Flow:
1. **Form** - User fills out details
2. **Confirmation** - User reviews and confirms
3. **Execution** - Data is saved to database

### Communication Pattern:
```
User: "add a sale"
  ↓
Backend: Shows form component
  ↓
User: Fills form and clicks "Preview Sale"
  ↓
Frontend: Sends "FORM_SUBMIT:{json data}"
  ↓
Backend: Shows confirmation component
  ↓
User: Clicks "Yes, Add Sale"
  ↓
Frontend: Sends "Confirm"
  ↓
Backend: Saves to database and shows success
```

---

## Test Queries

### 1. Add a Sale (Full Flow)

**Step 1:** Type this in chat:
```
add a sale
```

**Expected:** Form appears with fields:
- Product (dropdown)
- Sale Amount ($)
- Region (dropdown)
- Salesperson Email
- Sale Date

**Step 2:** Fill out the form:
- Product: `Laptop Pro 15`
- Amount: `1299.99`
- Region: `North America`
- Salesperson: (use an existing email from your database)
- Date: Today's date (pre-filled)

Click **"Preview Sale"**

**Expected:** Confirmation dialog appears showing all the details

**Step 3:** Click **"Yes, Add Sale"**

**Expected:** 
- Success message: "✅ Sale added successfully!"
- Table showing the newly added sale

---

### 2. Add a Salesperson

**Step 1:** Type:
```
add a salesperson
```

**Expected:** Form with fields:
- First Name
- Last Name
- Email
- Region

**Step 2:** Fill and submit

**Expected:** Confirmation → Database insert → Success message

---

## Checking the Database

After adding data, verify it was saved:

```
show me recent sales
```

Should show your newly added sale in the table.

---

## Backend Flow Explanation

The backend (`GenerativeUIService.cs`) detects keywords:
- `"add sale"` → Triggers sale form flow
- `"FORM_SUBMIT:"` → Parses data and shows confirmation
- `"Confirm"` → Executes database insert

The flow uses chat history to store pending data between steps.

---

## Current Limitations

1. **Salesperson Email Must Exist**: When adding a sale, the salesperson email must already exist in the database. If not found, you'll get a warning.

2. **No Validation Yet**: Form validation is basic (required fields only)

3. **Cancel Not Implemented**: Clicking "Cancel" sends the message but doesn't have special handling yet

---

## Next Steps to Enhance

### Add More Entities
Create forms for:
- Weather data entry
- Edit existing records
- Delete with confirmation (using `variant="danger"`)

### Add Validation
- Email format validation
- Amount range validation
- Date range validation

### Multi-Step Workflows
Create flows like:
```
"Assign John to Project X"
  ↓ Form: Collect project details
  ↓ Analysis: Find best match
  ↓ Confirmation: Show recommendation
  ↓ Execute: Update assignments
```

### Error Handling
- Handle database errors gracefully
- Show user-friendly error messages
- Retry mechanisms

---

## Example: Full Session

```
User: add a sale
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FogData.Database;
using FogData.Database.Entities;
using System.Text.Json;

namespace FogData.Controllers;

/// <summary>
/// Controller for executing actions from the Generative UI SDK.
/// This is the stateless action execution endpoint that components call directly.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ActionsController : ControllerBase
{
    private readonly FogDataDbContext _dbContext;
    private readonly ILogger<ActionsController> _logger;

    public ActionsController(FogDataDbContext dbContext, ILogger<ActionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Execute a generic action - this is the main endpoint the SDK calls
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteAction([FromBody] ActionRequest request)
    {
        _logger.LogInformation("Executing action: {ActionType}", request.ActionType);
        
        try
        {
            var result = request.ActionType switch
            {
                "create-sale" => await CreateSaleAsync(request.Payload),
                "create-person" => await CreatePersonAsync(request.Payload),
                "update-sale" => await UpdateSaleAsync(request.Payload),
                "delete-sale" => await DeleteSaleAsync(request.Payload),
                _ => new ActionResult { Success = false, Error = $"Unknown action type: {request.ActionType}" }
            };
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action {ActionType}", request.ActionType);
            return StatusCode(500, new ActionResult 
            { 
                Success = false, 
                Error = ex.Message 
            });
        }
    }

    #region Sales Actions
    
    /// <summary>
    /// POST /api/actions/sales - Create a new sale
    /// </summary>
    [HttpPost("sales")]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request)
    {
        var result = await CreateSaleAsync(JsonSerializer.SerializeToElement(request));
        return result.Success ? Ok(result) : BadRequest(result);
    }
    
    private async Task<ActionResult> CreateSaleAsync(JsonElement payload)
    {
        var salespersonEmail = payload.GetProperty("salespersonEmail").GetString();
        var salesperson = await _dbContext.People
            .FirstOrDefaultAsync(p => p.Email == salespersonEmail);
        
        if (salesperson == null)
        {
            return new ActionResult 
            { 
                Success = false, 
                Error = $"Salesperson with email '{salespersonEmail}' not found" 
            };
        }
        
        var sale = new SalesData
        {
            Product = payload.GetProperty("product").GetString() ?? "",
            Amount = payload.GetProperty("amount").GetDecimal(),
            Region = payload.GetProperty("region").GetString() ?? "",
            SaleDate = DateTime.Parse(payload.GetProperty("date").GetString() ?? DateTime.Today.ToString("yyyy-MM-dd")),
            SalesPersonId = salesperson.Id
        };
        
        _dbContext.SalesData.Add(sale);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Created sale: {Product} for ${Amount}", sale.Product, sale.Amount);
        
        return new ActionResult
        {
            Success = true,
            Message = "Sale created successfully",
            Data = new
            {
                id = sale.Id,
                product = sale.Product,
                amount = sale.Amount,
                region = sale.Region,
                date = sale.SaleDate.ToString("yyyy-MM-dd"),
                salesperson = $"{salesperson.FirstName} {salesperson.LastName}"
            }
        };
    }
    
    private async Task<ActionResult> UpdateSaleAsync(JsonElement payload)
    {
        var id = payload.GetProperty("id").GetInt32();
        var sale = await _dbContext.SalesData.FindAsync(id);
        
        if (sale == null)
        {
            return new ActionResult { Success = false, Error = $"Sale with ID {id} not found" };
        }
        
        if (payload.TryGetProperty("product", out var product))
            sale.Product = product.GetString() ?? sale.Product;
        if (payload.TryGetProperty("amount", out var amount))
            sale.Amount = amount.GetDecimal();
        if (payload.TryGetProperty("region", out var region))
            sale.Region = region.GetString() ?? sale.Region;
        
        await _dbContext.SaveChangesAsync();
        
        return new ActionResult
        {
            Success = true,
            Message = "Sale updated successfully",
            Data = sale
        };
    }
    
    private async Task<ActionResult> DeleteSaleAsync(JsonElement payload)
    {
        var id = payload.GetProperty("id").GetInt32();
        var sale = await _dbContext.SalesData.FindAsync(id);
        
        if (sale == null)
        {
            return new ActionResult { Success = false, Error = $"Sale with ID {id} not found" };
        }
        
        _dbContext.SalesData.Remove(sale);
        await _dbContext.SaveChangesAsync();
        
        return new ActionResult
        {
            Success = true,
            Message = "Sale deleted successfully"
        };
    }
    
    #endregion
    
    #region Person Actions
    
    /// <summary>
    /// POST /api/actions/people - Create a new person
    /// </summary>
    [HttpPost("people")]
    public async Task<IActionResult> CreatePerson([FromBody] CreatePersonRequest request)
    {
        var result = await CreatePersonAsync(JsonSerializer.SerializeToElement(request));
        return result.Success ? Ok(result) : BadRequest(result);
    }
    
    private async Task<ActionResult> CreatePersonAsync(JsonElement payload)
    {
        var email = payload.GetProperty("email").GetString();
        
        // Check if person already exists
        var existing = await _dbContext.People.FirstOrDefaultAsync(p => p.Email == email);
        if (existing != null)
        {
            return new ActionResult 
            { 
                Success = false, 
                Error = $"Person with email '{email}' already exists" 
            };
        }
        
        var person = new Person
        {
            FirstName = payload.GetProperty("firstName").GetString() ?? "",
            LastName = payload.GetProperty("lastName").GetString() ?? "",
            Email = email ?? "",
            Region = payload.GetProperty("region").GetString() ?? ""
        };
        
        _dbContext.People.Add(person);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Created person: {FirstName} {LastName}", person.FirstName, person.LastName);
        
        return new ActionResult
        {
            Success = true,
            Message = "Person created successfully",
            Data = new
            {
                id = person.Id,
                firstName = person.FirstName,
                lastName = person.LastName,
                email = person.Email,
                region = person.Region
            }
        };
    }
    
    #endregion
}

#region Request/Response Models

public record ActionRequest
{
    public string ActionType { get; init; } = string.Empty;
    public JsonElement Payload { get; init; }
}

public record CreateSaleRequest
{
    public string Product { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Region { get; init; } = string.Empty;
    public string SalespersonEmail { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
}

public record CreatePersonRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
}

public record ActionResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
    public object? Data { get; init; }
}

#endregion

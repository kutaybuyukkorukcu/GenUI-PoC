namespace FogData.Database.Entities;

public class SalesData
{
    public int Id { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign key and navigation property
    public int SalesPersonId { get; set; }
    public virtual Person SalesPerson { get; set; } = null!;
}

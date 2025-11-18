using System.Text.Json.Serialization;

namespace FogData.Database.Entities;

public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property - ignored during JSON serialization to prevent cycles
    [JsonIgnore]
    public virtual ICollection<SalesData> Sales { get; set; } = new List<SalesData>();
}

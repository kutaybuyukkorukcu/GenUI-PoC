using FogData.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace FogData.Database;

/// <summary>
/// Data seeder for development/demo purposes.
/// In a production SDK, this would be removed or made optional.
/// </summary>
public class DataSeeder
{
    public static async Task SeedAsync(FogDataDbContext context)
    {
        // No-op for generic SDK - customers bring their own data
        // This method is kept for backward compatibility
        await Task.CompletedTask;
    }
}

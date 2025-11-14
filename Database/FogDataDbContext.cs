using Microsoft.EntityFrameworkCore;
using FogData.Database.Entities;

namespace FogData.Database;

public class FogDataDbContext : DbContext
{
    public FogDataDbContext(DbContextOptions<FogDataDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherData> WeatherData { get; set; }
    public DbSet<SalesData> SalesData { get; set; }
    public DbSet<Person> People { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure WeatherData
        modelBuilder.Entity<WeatherData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Condition).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Location);
        });

        // Configure SalesData
        modelBuilder.Entity<SalesData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Region).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Product).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => e.SaleDate);
            entity.HasIndex(e => e.Region);
            
            // Configure relationship with Person
            entity.HasOne(e => e.SalesPerson)
                .WithMany(p => p.Sales)
                .HasForeignKey(e => e.SalesPersonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Person
        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Region).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Region);
        });
    }
}

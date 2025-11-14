using FogData.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace FogData.Database;

public class DataSeeder
{
    public static async Task SeedAsync(FogDataDbContext context)
    {
        // Seed People (Sales Representatives)
        if (!context.People.Any())
        {
            var people = new List<Person>
            {
                new Person
                {
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "john.smith@fogdata.com",
                    Region = "North America",
                    Role = "Senior Sales Representative",
                    HireDate = DateTime.UtcNow.AddYears(-3).AddMonths(-3),
                    IsActive = true
                },
                new Person
                {
                    FirstName = "Emma",
                    LastName = "Johnson",
                    Email = "emma.johnson@fogdata.com",
                    Region = "Europe",
                    Role = "Sales Representative",
                    HireDate = DateTime.UtcNow.AddYears(-2).AddMonths(-6),
                    IsActive = true
                },
                new Person
                {
                    FirstName = "Yuki",
                    LastName = "Tanaka",
                    Email = "yuki.tanaka@fogdata.com",
                    Region = "Asia Pacific",
                    Role = "Regional Sales Manager",
                    HireDate = DateTime.UtcNow.AddYears(-5).AddMonths(-9),
                    IsActive = true
                },
                new Person
                {
                    FirstName = "Carlos",
                    LastName = "Rodriguez",
                    Email = "carlos.rodriguez@fogdata.com",
                    Region = "South America",
                    Role = "Sales Representative",
                    HireDate = DateTime.UtcNow.AddYears(-2).AddMonths(-1),
                    IsActive = true
                },
                new Person
                {
                    FirstName = "Sarah",
                    LastName = "Chen",
                    Email = "sarah.chen@fogdata.com",
                    Region = "Asia Pacific",
                    Role = "Sales Representative",
                    HireDate = DateTime.UtcNow.AddYears(-2).AddMonths(-11),
                    IsActive = true
                }
            };

            await context.People.AddRangeAsync(people);
            await context.SaveChangesAsync();
        }
        // Seed Weather Data
        if (!context.WeatherData.Any())
        {
            var weatherData = new List<WeatherData>
            {
                new WeatherData
                {
                    Location = "New York",
                    Date = DateTime.UtcNow.AddDays(-7),
                    Temperature = 22,
                    Condition = "Sunny",
                    Humidity = 45,
                    WindSpeed = 15
                },
                new WeatherData
                {
                    Location = "New York",
                    Date = DateTime.UtcNow.AddDays(-6),
                    Temperature = 18,
                    Condition = "Cloudy",
                    Humidity = 60,
                    WindSpeed = 20
                },
                new WeatherData
                {
                    Location = "New York",
                    Date = DateTime.UtcNow.AddDays(-5),
                    Temperature = 15,
                    Condition = "Rainy",
                    Humidity = 80,
                    WindSpeed = 25
                },
                new WeatherData
                {
                    Location = "London",
                    Date = DateTime.UtcNow.AddDays(-7),
                    Temperature = 12,
                    Condition = "Cloudy",
                    Humidity = 70,
                    WindSpeed = 18
                },
                new WeatherData
                {
                    Location = "London",
                    Date = DateTime.UtcNow.AddDays(-6),
                    Temperature = 10,
                    Condition = "Rainy",
                    Humidity = 85,
                    WindSpeed = 22
                },
                new WeatherData
                {
                    Location = "Tokyo",
                    Date = DateTime.UtcNow.AddDays(-7),
                    Temperature = 28,
                    Condition = "Sunny",
                    Humidity = 50,
                    WindSpeed = 10
                },
                new WeatherData
                {
                    Location = "Tokyo",
                    Date = DateTime.UtcNow.AddDays(-6),
                    Temperature = 26,
                    Condition = "Partly Cloudy",
                    Humidity = 55,
                    WindSpeed = 12
                }
            };

            await context.WeatherData.AddRangeAsync(weatherData);
        }

        // Seed Sales Data
        if (!context.SalesData.Any())
        {
            // Get the seeded people (they should already be saved above)
            var people = await context.People.ToListAsync();
            var johnSmith = people.First(p => p.Email == "john.smith@fogdata.com");
            var emmaJohnson = people.First(p => p.Email == "emma.johnson@fogdata.com");
            var yukiTanaka = people.First(p => p.Email == "yuki.tanaka@fogdata.com");
            var carlosRodriguez = people.First(p => p.Email == "carlos.rodriguez@fogdata.com");
            var sarahChen = people.First(p => p.Email == "sarah.chen@fogdata.com");

            var salesData = new List<SalesData>
            {
                new SalesData
                {
                    Region = "North America",
                    Product = "Laptop Pro 15",
                    SaleDate = DateTime.UtcNow.AddDays(-10),
                    Amount = 1299.99m,
                    Quantity = 5,
                    SalesPersonId = johnSmith.Id
                },
                new SalesData
                {
                    Region = "North America",
                    Product = "Wireless Mouse",
                    SaleDate = DateTime.UtcNow.AddDays(-9),
                    Amount = 49.99m,
                    Quantity = 15,
                    SalesPersonId = johnSmith.Id
                },
                new SalesData
                {
                    Region = "Europe",
                    Product = "Laptop Pro 15",
                    SaleDate = DateTime.UtcNow.AddDays(-8),
                    Amount = 1299.99m,
                    Quantity = 3,
                    SalesPersonId = emmaJohnson.Id
                },
                new SalesData
                {
                    Region = "Europe",
                    Product = "Mechanical Keyboard",
                    SaleDate = DateTime.UtcNow.AddDays(-7),
                    Amount = 129.99m,
                    Quantity = 8,
                    SalesPersonId = emmaJohnson.Id
                },
                new SalesData
                {
                    Region = "Asia Pacific",
                    Product = "USB-C Hub",
                    SaleDate = DateTime.UtcNow.AddDays(-6),
                    Amount = 79.99m,
                    Quantity = 12,
                    SalesPersonId = yukiTanaka.Id
                },
                new SalesData
                {
                    Region = "Asia Pacific",
                    Product = "Laptop Pro 15",
                    SaleDate = DateTime.UtcNow.AddDays(-5),
                    Amount = 1299.99m,
                    Quantity = 7,
                    SalesPersonId = yukiTanaka.Id
                },
                new SalesData
                {
                    Region = "North America",
                    Product = "Webcam HD",
                    SaleDate = DateTime.UtcNow.AddDays(-4),
                    Amount = 89.99m,
                    Quantity = 10,
                    SalesPersonId = johnSmith.Id
                },
                new SalesData
                {
                    Region = "Europe",
                    Product = "Wireless Mouse",
                    SaleDate = DateTime.UtcNow.AddDays(-3),
                    Amount = 49.99m,
                    Quantity = 20,
                    SalesPersonId = emmaJohnson.Id
                },
                new SalesData
                {
                    Region = "Asia Pacific",
                    Product = "Mechanical Keyboard",
                    SaleDate = DateTime.UtcNow.AddDays(-2),
                    Amount = 129.99m,
                    Quantity = 6,
                    SalesPersonId = sarahChen.Id
                },
                new SalesData
                {
                    Region = "South America",
                    Product = "Laptop Pro 15",
                    SaleDate = DateTime.UtcNow.AddDays(-1),
                    Amount = 1299.99m,
                    Quantity = 4,
                    SalesPersonId = carlosRodriguez.Id
                },
                new SalesData
                {
                    Region = "Asia Pacific",
                    Product = "Webcam HD",
                    SaleDate = DateTime.UtcNow.AddDays(-10),
                    Amount = 89.99m,
                    Quantity = 8,
                    SalesPersonId = sarahChen.Id
                },
                new SalesData
                {
                    Region = "North America",
                    Product = "USB-C Hub",
                    SaleDate = DateTime.UtcNow.AddDays(-11),
                    Amount = 79.99m,
                    Quantity = 14,
                    SalesPersonId = johnSmith.Id
                }
            };

            await context.SalesData.AddRangeAsync(salesData);
        }

        await context.SaveChangesAsync();
    }
}

# Database Setup Guide

## Prerequisites
- PostgreSQL Docker container running
- EF Core tools installed
- Connection string configured in appsettings

## Entities Created
- **WeatherData**: Stores weather information (location, date, temperature, condition, humidity, wind speed)
- **SalesData**: Stores sales transactions (region, product, sale date, amount, quantity, sales rep)

## Steps

### 1. Verify PostgreSQL Container
```bash
docker ps | grep postgres
```

### 2. Create Initial Migration
Navigate to the project directory and create your first migration:
```bash
dotnet ef migrations add InitialCreate
```

### 3. Apply Migration to Database
This will create the tables in PostgreSQL:
```bash
dotnet ef database update
```

**Note**: The application will automatically:
- Run pending migrations on startup via `context.Database.MigrateAsync()`
- Seed sample data if tables are empty via `DataSeeder.SeedAsync()`

### 4. Verify Database
Connect to PostgreSQL to verify tables:
```bash
docker exec -it fogdata-postgres-1 psql -U fogdata_user -d fogdata
```

List tables:
```sql
\dt
```

Check data:
```sql
SELECT * FROM "WeatherData" LIMIT 5;
SELECT * FROM "SalesData" LIMIT 5;
```

### 5. Sample Data Seeded
- **Weather**: 7 records across New York, London, Tokyo with various conditions
- **Sales**: 8 records across North America, Europe, Asia Pacific regions

## Connection Details
- **Host**: localhost (from host) or postgres (from Docker)
- **Port**: 5432
- **Database**: fogdata
- **User**: fogdata_user
- **Password**: fogdata_password

## Troubleshooting

### Migration Command Not Found
If `dotnet ef` is not recognized, install the tools globally:
```bash
dotnet tool install --global dotnet-ef
```

### Connection Issues
Ensure the PostgreSQL container is running:
```bash
docker-compose up postgres -d
```

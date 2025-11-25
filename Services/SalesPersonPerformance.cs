namespace FogData.Services;

public record SalesPersonPerformance
{
    public string SalesPersonName { get; init; } = string.Empty;
    public decimal TotalSales { get; init; }
    public int SalesCount { get; init; }
    public string Region { get; init; } = string.Empty;
}

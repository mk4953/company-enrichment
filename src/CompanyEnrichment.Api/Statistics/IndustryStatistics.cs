namespace CompanyEnrichment.Api.Statistics;

/// <summary>
/// Industry statistics for one industry code and year from Statistics Denmark (table ERHV1).
/// </summary>
public class IndustryStatistics
{
    // The 6-digit DB07 industry code used in the lookup, e.g. "011100"
    public string IndustryCode { get; init; } = string.Empty;

    public int Year { get; init; }

    // Number of workplaces (ARBSTED), not number of companies
    public int Workplaces { get; init; }

    // Number of jobs (ANSATTE)
    public int Employees { get; init; }

    // Number of full-time employees (FULDBESK)
    public int FullTimeEmployees { get; init; }

    // Wage and salary costs in million DKK (LØNSUM)
    public int TotalSalarySumMillionDkk { get; init; }
}

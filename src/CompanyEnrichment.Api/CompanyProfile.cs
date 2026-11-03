using CompanyEnrichment.Api.Statistics;

namespace CompanyEnrichment.Api;

/// <summary>
/// The combined response: company master data enriched with industry statistics.
/// </summary>
public class CompanyProfile
{
    public CompanyDetails Company { get; init; } = new CompanyDetails();

    // Null when Statistics Denmark has no data for the industry code or year, or is unavailable
    public IndustryStatistics? IndustryStatistics { get; init; }
}

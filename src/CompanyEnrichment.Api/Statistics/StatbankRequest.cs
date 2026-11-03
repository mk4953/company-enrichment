namespace CompanyEnrichment.Api.Statistics;

/// <summary>
/// Request body for the Statistics Denmark data endpoint (POST /v1/data).
/// </summary>
public class StatbankRequest
{
    public string Table { get; init; } = string.Empty;

    public string Format { get; init; } = string.Empty;

    public string ValuePresentation { get; init; } = string.Empty;

    public List<StatbankVariable> Variables { get; init; } = new List<StatbankVariable>();
}

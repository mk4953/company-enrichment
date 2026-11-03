namespace CompanyEnrichment.Api.Statistics;

/// <summary>
/// One variable in a Statistics Denmark request, e.g. the industry code or the year.
/// </summary>
public class StatbankVariable
{
    public string Code { get; init; } = string.Empty;

    public List<string> Values { get; init; } = new List<string>();
}

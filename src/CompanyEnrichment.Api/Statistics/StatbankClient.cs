using System.Net;

namespace CompanyEnrichment.Api.Statistics;

/// <summary>
/// Fetches industry statistics from Statistics Denmark (api.statbank.dk, table ERHV1).
/// </summary>
public class StatbankClient
{
    // Table and response settings
    private const string TableName = "ERHV1";
    private const string ResponseFormat = "CSV";
    private const string ValuePresentation = "Code";

    // Variable codes in the request
    private const string IndustryVariable = "BRANCHE07";
    private const string MeasureVariable = "TAL";
    private const string YearVariable = "TID";

    // Measure codes in ERHV1
    private const string WorkplacesMeasure = "ARBSTED";
    private const string EmployeesMeasure = "ANSATTE";
    private const string FullTimeEmployeesMeasure = "FULDBESK";
    private const string TotalSalarySumMeasure = "LØNSUM";

    // Error code returned when the industry code or year does not exist
    private const string NotFoundErrorCode = "EXTRACT-NOTFOUND";

    private readonly HttpClient _httpClient;

    public StatbankClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Returns the statistics for an industry code and year, or null if Statistics Denmark has no data for them.
    /// </summary>
    public async Task<IndustryStatistics?> GetIndustryStatisticsAsync(int industryCode, int year, CancellationToken cancellationToken = default)
    {
        // DST uses 6-digit codes with leading zeros, e.g. 11100 becomes "011100"
        var formattedIndustryCode = industryCode.ToString("D6");
        var request = CreateRequest(formattedIndustryCode, year);

        using var response = await _httpClient.PostAsJsonAsync("data", request, cancellationToken);

        // DST returns 400 with "EXTRACT-NOTFOUND" when the industry code or year does not exist
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            if (error.Contains(NotFoundErrorCode))
            {
                return null;
            }
        }

        response.EnsureSuccessStatusCode();

        var csv = await response.Content.ReadAsStringAsync(cancellationToken);
        var values = ParseCsv(csv);

        return new IndustryStatistics
        {
            IndustryCode = formattedIndustryCode,
            Year = year,
            Workplaces = values[WorkplacesMeasure],
            Employees = values[EmployeesMeasure],
            FullTimeEmployees = values[FullTimeEmployeesMeasure],
            TotalSalarySumMillionDkk = values[TotalSalarySumMeasure]
        };
    }

    /// <summary>
    /// Parses the CSV response into a dictionary of measure code and value.
    /// </summary>
    /// <remarks>
    /// Example input:
    /// BRANCHE07;TAL;TID;INDHOLD
    /// 702200;ARBSTED;2024;9323
    /// </remarks>
    internal static Dictionary<string, int> ParseCsv(string csv)
    {
        var values = new Dictionary<string, int>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Start at 1 to skip the header line
        for (var i = 1; i < lines.Length; i++)
        {
            var columns = lines[i].Split(';');
            var measure = columns[1];
            var value = int.Parse(columns[3]);

            values[measure] = value;
        }

        return values;
    }

    private static StatbankRequest CreateRequest(string industryCode, int year)
    {
        return new StatbankRequest
        {
            Table = TableName,
            Format = ResponseFormat,
            // Return codes (e.g. "ARBSTED") instead of Danish labels, which makes the CSV easier to parse
            ValuePresentation = ValuePresentation,
            Variables = new List<StatbankVariable>
            {
                new StatbankVariable
                {
                    Code = IndustryVariable,
                    Values = new List<string> { industryCode }
                },
                new StatbankVariable
                {
                    Code = MeasureVariable,
                    Values = new List<string> { WorkplacesMeasure, EmployeesMeasure, FullTimeEmployeesMeasure, TotalSalarySumMeasure }
                },
                new StatbankVariable
                {
                    Code = YearVariable,
                    Values = new List<string> { year.ToString() }
                }
            }
        };
    }
}

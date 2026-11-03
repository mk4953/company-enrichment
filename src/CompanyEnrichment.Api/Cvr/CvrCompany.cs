using System.Text.Json.Serialization;

namespace CompanyEnrichment.Api.Cvr;

/// <summary>
/// A company as returned by the CVR API. JsonPropertyName maps the CVR field names to our property names.
/// </summary>
public class CvrCompany
{
    [JsonPropertyName("vat")]
    public int CvrNumber { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("zipcode")]
    public int? ZipCode { get; init; }

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("industrycode")]
    public int? IndustryCode { get; init; }

    [JsonPropertyName("industrydesc")]
    public string? IndustryDescription { get; init; }

    // Can be null, e.g. for small companies
    [JsonPropertyName("employees")]
    public int? Employees { get; init; }
}

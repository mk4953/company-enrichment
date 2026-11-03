namespace CompanyEnrichment.Api;

/// <summary>
/// The company master data returned by our API.
/// Kept separate from CvrCompany, so our response does not depend on the CVR API field names.
/// </summary>
public class CompanyDetails
{
    public int CvrNumber { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Address { get; init; }

    public int? ZipCode { get; init; }

    public string? City { get; init; }

    public int? IndustryCode { get; init; }

    public string? IndustryDescription { get; init; }

    public int? Employees { get; init; }
}

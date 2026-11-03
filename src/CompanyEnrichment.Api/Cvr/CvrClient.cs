using System.Net;

namespace CompanyEnrichment.Api.Cvr;

/// <summary>
/// Looks up companies in the CVR registry (apicvr.dk).
/// </summary>
public class CvrClient
{
    private readonly HttpClient _httpClient;

    public CvrClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Returns the company with the given CVR number, or null if it does not exist.
    /// </summary>
    public async Task<CvrCompany?> GetByCvrNumberAsync(int cvrNumber, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(cvrNumber.ToString(), cancellationToken);

        // A company that does not exist is not an error, so return null instead of throwing
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CvrCompany>(cancellationToken);
    }

    /// <summary>
    /// Searches for companies by name. The CVR search is fuzzy and can return several companies.
    /// </summary>
    public async Task<List<CvrCompany>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        // The CVR API cannot handle "/" in the search, so "Novo Nordisk A/S" is searched as "Novo Nordisk A"
        var searchTerm = name.Split('/')[0];
        var url = $"search/company/{Uri.EscapeDataString(searchTerm)}";

        var companies = await _httpClient.GetFromJsonAsync<List<CvrCompany>>(url, cancellationToken);
        if (companies == null)
        {
            return new List<CvrCompany>();
        }

        return companies;
    }
}

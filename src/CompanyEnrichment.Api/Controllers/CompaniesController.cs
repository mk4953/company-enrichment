using CompanyEnrichment.Api.Cvr;
using CompanyEnrichment.Api.Statistics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CompanyEnrichment.Api.Controllers;

/// <summary>
/// Looks up a company in CVR and enriches it with industry statistics from Statistics Denmark.
/// </summary>
[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly CvrClient _cvrClient;
    private readonly StatbankClient _statbankClient;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(CvrClient cvrClient, StatbankClient statbankClient, ILogger<CompaniesController> logger)
    {
        _cvrClient = cvrClient;
        _statbankClient = statbankClient;
        _logger = logger;
    }

    /// <summary>
    /// Lookup by CVR number, e.g. GET api/companies/10150817?year=2024
    /// </summary>
    [HttpGet("{cvrNumber:int}")]
    public async Task<ActionResult<CompanyProfile>> GetByCvrNumber(
        int cvrNumber, [FromQuery][BindRequired] int year, CancellationToken cancellationToken)
    {
        var company = await _cvrClient.GetByCvrNumberAsync(cvrNumber, cancellationToken);
        if (company == null)
        {
            return NotFound();
        }

        return await BuildProfileAsync(company, year, cancellationToken);
    }

    /// <summary>
    /// Lookup by name, e.g. GET api/companies?name=Erhvervsstyrelsen&amp;year=2024
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CompanyProfile>> GetByName(
        [FromQuery] string name, [FromQuery][BindRequired] int year, CancellationToken cancellationToken)
    {
        var companies = await _cvrClient.SearchByNameAsync(name, cancellationToken);

        var company = FindBestMatch(companies, name);
        if (company == null)
        {
            return NotFound();
        }

        return await BuildProfileAsync(company, year, cancellationToken);
    }

    private async Task<CompanyProfile> BuildProfileAsync(CvrCompany company, int year, CancellationToken cancellationToken)
    {
        var statistics = await TryGetStatisticsAsync(company.IndustryCode, year, cancellationToken);

        return new CompanyProfile
        {
            Company = ToCompanyDetails(company),
            IndustryStatistics = statistics
        };
    }

    // Statistics are optional: if Statistics Denmark fails, the company is returned without statistics
    private async Task<IndustryStatistics?> TryGetStatisticsAsync(int? industryCode, int year, CancellationToken cancellationToken)
    {
        if (industryCode == null)
        {
            return null;
        }

        try
        {
            return await _statbankClient.GetIndustryStatisticsAsync(industryCode.Value, year, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The caller cancelled the request, so stop instead of logging it as a Statistics Denmark failure
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to fetch industry statistics for {IndustryCode}", industryCode);
            return null;
        }
    }

    // The CVR search is fuzzy, so prefer an exact name match and otherwise use the first result
    private static CvrCompany? FindBestMatch(List<CvrCompany> companies, string name)
    {
        foreach (var company in companies)
        {
            if (string.Equals(company.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return company;
            }
        }

        return companies.FirstOrDefault();
    }

    private static CompanyDetails ToCompanyDetails(CvrCompany company)
    {
        return new CompanyDetails
        {
            CvrNumber = company.CvrNumber,
            Name = company.Name,
            Address = company.Address,
            ZipCode = company.ZipCode,
            City = company.City,
            IndustryCode = company.IndustryCode,
            IndustryDescription = company.IndustryDescription,
            Employees = company.Employees
        };
    }
}

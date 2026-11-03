using System.Net;
using CompanyEnrichment.Api.Controllers;
using CompanyEnrichment.Api.Cvr;
using CompanyEnrichment.Api.Statistics;
using Microsoft.Extensions.Logging.Abstractions;

namespace CompanyEnrichment.Api.Tests;

public class CompaniesControllerTests
{
    [Test]
    public async Task GetByCvrNumber_ReturnsCompanyWithoutStatistics_WhenDstIsDown()
    {
        // Arrange: CVR returns a company, Statistics Denmark fails
        var cvrHandler = new FakeHttpHandler(HttpStatusCode.OK, """{"vat":10150817,"name":"Erhvervsstyrelsen","industrycode":841300}""");
        var statbankHandler = new FakeHttpHandler(HttpStatusCode.InternalServerError);
        var controller = new CompaniesController(
            new CvrClient(new HttpClient(cvrHandler) { BaseAddress = new Uri("https://fake-cvr/") }),
            new StatbankClient(new HttpClient(statbankHandler) { BaseAddress = new Uri("https://fake-dst/") }),
            NullLogger<CompaniesController>.Instance);

        // Act
        var result = await controller.GetByCvrNumber(10150817, 2024, CancellationToken.None);

        // Assert: the company is still returned, only the statistics are missing
        Assert.That(result.Value!.Company.Name, Is.EqualTo("Erhvervsstyrelsen"));
        Assert.That(result.Value.IndustryStatistics, Is.Null);
    }
}

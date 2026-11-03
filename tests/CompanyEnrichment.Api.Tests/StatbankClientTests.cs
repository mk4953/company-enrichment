using System.Net;
using CompanyEnrichment.Api.Statistics;

namespace CompanyEnrichment.Api.Tests;

public class StatbankClientTests
{
    // A real response from Statistics Denmark for industry code 011100
    private const string ValidCsv = """
        BRANCHE07;TAL;TID;INDHOLD
        011100;ARBSTED;2024;8813
        011100;ANSATTE;2024;15308
        011100;FULDBESK;2024;4271
        011100;LØNSUM;2024;1751
        """;

    [Test]
    public void ParseCsv_ReadsValuePerMeasure()
    {
        // Act
        var values = StatbankClient.ParseCsv(ValidCsv);

        // Assert
        Assert.That(values["ARBSTED"], Is.EqualTo(8813));
        Assert.That(values["LØNSUM"], Is.EqualTo(1751));
    }

    [Test]
    public async Task GetIndustryStatistics_PadsIndustryCodeToSixDigits()
    {
        // Arrange
        var handler = new FakeHttpHandler(HttpStatusCode.OK, ValidCsv);
        var client = new StatbankClient(new HttpClient(handler) { BaseAddress = new Uri("https://fake/") });

        // Act
        await client.GetIndustryStatisticsAsync(11100, 2024);

        // Assert: the leading zero is added before calling Statistics Denmark
        Assert.That(handler.LastRequestBody, Does.Contain("\"011100\""));
    }

    [Test]
    public async Task GetIndustryStatistics_ReturnsNull_WhenDstDoesNotKnowTheCode()
    {
        // Arrange: Statistics Denmark does not know the industry code
        var handler = new FakeHttpHandler(HttpStatusCode.BadRequest, """{"errorTypeCode":"EXTRACT-NOTFOUND"}""");
        var client = new StatbankClient(new HttpClient(handler) { BaseAddress = new Uri("https://fake/") });

        // Act
        var result = await client.GetIndustryStatisticsAsync(702000, 2024);

        // Assert
        Assert.That(result, Is.Null);
    }
}

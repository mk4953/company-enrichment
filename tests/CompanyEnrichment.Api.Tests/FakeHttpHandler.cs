using System.Net;
using System.Text;

namespace CompanyEnrichment.Api.Tests;

/// <summary>
/// Fake HTTP handler that always returns the same response, so the tests do not call the real APIs.
/// </summary>
public class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;

    public FakeHttpHandler(HttpStatusCode statusCode, string content = "")
    {
        _statusCode = statusCode;
        _content = content;
    }

    // The body of the last request, so tests can check what was sent
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, Encoding.UTF8, "application/json")
        };
    }
}

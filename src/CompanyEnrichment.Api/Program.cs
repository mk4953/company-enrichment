using CompanyEnrichment.Api.Cvr;
using CompanyEnrichment.Api.Statistics;
using OpenTelemetry.Metrics;
using Polly.CircuitBreaker;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// One typed HttpClient per external source. The standard resilience handler adds retry, timeouts and a circuit breaker.
var cvrBaseUrl = GetRequiredSetting(builder.Configuration, "ExternalApis:CvrBaseUrl");
var statbankBaseUrl = GetRequiredSetting(builder.Configuration, "ExternalApis:StatbankBaseUrl");

builder.Services.AddHttpClient<CvrClient>(client =>
        client.BaseAddress = new Uri(cvrBaseUrl))
    .AddStandardResilienceHandler();
builder.Services.AddHttpClient<StatbankClient>(client =>
        client.BaseAddress = new Uri(statbankBaseUrl))
    .AddStandardResilienceHandler();

// .NET already measures response time and status code for incoming (AspNetCore) and outgoing (HttpClient) calls.
// We expose them on /metrics so Prometheus can calculate p95 and error rate.
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("Microsoft.AspNetCore.Hosting", "System.Net.Http")
        .AddPrometheusExporter());

var app = builder.Build();

// Failures in external sources return 503 instead of 500
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception =>
    {
        if (IsExternalSourceFailure(exception))
        {
            return StatusCodes.Status503ServiceUnavailable;
        }

        return StatusCodes.Status500InternalServerError;
    }
});

app.MapControllers();
app.MapPrometheusScrapingEndpoint();

app.Run();

static string GetRequiredSetting(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (string.IsNullOrEmpty(value))
    {
        throw new InvalidOperationException($"Missing configuration value '{key}'");
    }

    return value;
}

// Errors thrown when CVR or Statistics Denmark fails, is too slow, or the circuit breaker is open
static bool IsExternalSourceFailure(Exception exception)
{
    return exception is HttpRequestException
        || exception is TimeoutRejectedException
        || exception is BrokenCircuitException;
}

# CompanyEnrichment

A small ASP.NET Core service that looks up a Danish company in the CVR registry ([apicvr.dk](https://apicvr.dk)) and enriches it with industry statistics from Statistics Denmark ([api.statbank.dk](https://api.statbank.dk), table `ERHV1`) in a single response.

## Run locally

**.NET 10 SDK**

```bash
dotnet run --project src/CompanyEnrichment.Api    # http://localhost:5162
dotnet test
```

**Docker**

```bash
docker build -t company-enrichment .
docker run --rm -p 8080:8080 company-enrichment   # http://localhost:8080
```

## Try it

| Request | Description |
|---|---|
| `GET /api/companies/{cvrNumber}?year=2024` | Lookup by CVR number |
| `GET /api/companies?name={name}&year=2024` | Lookup by name (exact match preferred, otherwise first hit) |
| `GET /metrics` | Metrics in Prometheus format |

Example: `GET /api/companies/24256790?year=2024`

```json
{
  "company": {
    "cvrNumber": 24256790,
    "name": "NOVO NORDISK A/S",
    "address": "Novo Alle 1",
    "zipCode": 2880,
    "city": "Bagsværd",
    "industryCode": 212000,
    "industryDescription": "Fremstilling af farmaceutiske præparater",
    "employees": 27151
  },
  "industryStatistics": {
    "industryCode": "212000",
    "year": 2024,
    "workplaces": 135,
    "employees": 43347,
    "fullTimeEmployees": 39717,
    "totalSalarySumMillionDkk": 35508
  }
}
```

More examples, including error cases, are in [CompanyEnrichment.Api.http](src/CompanyEnrichment.Api/CompanyEnrichment.Api.http) (REST Client in VS Code, or Visual Studio).

| Situation | Response |
|---|---|
| Company not found | `404` |
| `year` missing | `400` |
| No statistics for the industry code or year | `200` with `industryStatistics: null` |
| Statistics Denmark unavailable | `200` with `industryStatistics: null` (logged as a warning) |
| CVR unavailable | `503` |

## Metrics

The built-in .NET metrics `http.server.request.duration` (the service) and `http.client.request.duration` (each external source, by `server_address`) are exported with OpenTelemetry on `/metrics`. Example Prometheus queries:

```promql
# Service: p95 response time and error rate
histogram_quantile(0.95, sum by (le) (rate(http_server_request_duration_seconds_bucket{http_route=~"api/.*"}[5m])))
sum(rate(http_server_request_duration_seconds_count{http_route=~"api/.*", http_response_status_code=~"5.."}[5m]))
  / sum(rate(http_server_request_duration_seconds_count{http_route=~"api/.*"}[5m]))

# External sources: p95 response time and error rate per source
histogram_quantile(0.95, sum by (le, server_address) (rate(http_client_request_duration_seconds_bucket[5m])))
sum by (server_address) (rate(http_client_request_duration_seconds_count{error_type!="", http_response_status_code!~"4.."}[5m]))
  / sum by (server_address) (rate(http_client_request_duration_seconds_count[5m]))
```

4xx responses from the sources (company or industry code not found) are not counted as errors.

## Design decisions

- **Controllers + typed HttpClients**: one client per external source, no extra layers. The response uses its own model (`CompanyDetails`), so it does not depend on the CVR field names.
- **Configuration**: the base URLs of the external APIs are in `appsettings.json` and can be overridden with environment variables, e.g. `ExternalApis__CvrBaseUrl`.
- **Resilience**: `AddStandardResilienceHandler()` (retry, timeouts, circuit breaker) on both clients. Statistics are optional, so a Statistics Denmark failure still returns the company. A CVR failure returns `503` instead of `500`.
- **Metrics**: OpenTelemetry reuses the metrics .NET already records, so no custom timing code. The Prometheus exporter can be swapped for another exporter (e.g. Azure Monitor).
- **Tests (NUnit)**: focus on the logic that can break: CSV parsing, industry code formatting, "not found" handling and the fallback when Statistics Denmark is down. External APIs are replaced by a fake `HttpMessageHandler`.
- **CI (GitHub Actions)**: builds and tests on every push and pull request, and publishes the image to `ghcr.io` on pushes to `main`.

## Known limitations and next steps

- **Industry codes**: CVR uses the new DB25 codes, while `ERHV1` uses DB07. Most codes match, but some do not (e.g. `471200`), and those return no statistics. Next step: a DB25 → DB07 mapping table from Statistics Denmark.
- **Employees** from CVR are the current count, not the count for the requested year.
- **`workplaces`** (`ARBSTED`) counts workplaces, not companies. `totalSalarySumMillionDkk` (`LØNSUM`) is in million DKK.
- **Name search** in the CVR API is fuzzy and does not support `/`, so the name is cut at `/` before searching.
- **Not included**: caching (the statistics change once a year), a dashboard (e.g. Grafana or the Aspire dashboard), authentication and rate limiting.

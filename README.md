# corvo-nfse-dotnet

**The .NET SDK for the Corvo NFS-e API — built for self-hosted installs, zero runtime dependencies, and errors that never throw.**

Issue, list, download, and reconcile Brazilian electronic service invoices (NFS-e) from any .NET 8+ app. No wrapper frameworks, no `try/catch` around every call.

## Why corvo-nfse-dotnet

- **Self-hosted first.** Point it at your own Corvo instance with `BaseUrl`; the default `https://api.corvo.tech` only applies when you don't.
- **Zero runtime dependencies.** `HttpClient` and `System.Text.Json` are in the box. The package pulls nothing.
- **Errors that never throw.** API, network, and timeout failures come back as `Result<T>` with a `CorvoApiError` carrying the server's stable `code`, HTTP `status`, `requestId`, and field-level `details`. Throw it yourself if you prefer.
- **Pagination you don't have to think about.** Every list returns `Paginated<T>` with `{ limit, hasMore, nextCursor }`. Need every page? `IterateAsync(...)` walks them for you and surfaces any page failure as an error result.
- **The same contract as every other Corvo SDK.** 28 API-key operations + 2 health checks, `X-API-Key` auth, `v1` prefix, identical wire format — nothing re-cased or renamed.

## Install

```bash
dotnet add package Corvo.Nfse
```

## SDK quickstart

```csharp
using Corvo.Nfse;

using var client = new CorvoClient(new CorvoOptions
{
    ApiKey = "corvo_live_xxx",          // or set CORVO_API_KEY
    BaseUrl = "https://api.corvo.tech", // or CORVO_BASE_URL for your self-hosted install
});

var page = await client.Companies.ListAsync(limit: 10);
if (page.Error is { } error)
{
    // CorvoApiError: status, code, requestId, details, retryAfter
    Console.WriteLine($"{error.Code} {error.Status} {error.RequestId}");
}
else
{
    foreach (var company in page.Data!.Data)
        Console.WriteLine(company.LegalName);
    Console.WriteLine($"{page.Data.Pagination.HasMore} {page.Data.Pagination.NextCursor}");
}
```

### Issue an invoice

```csharp
var created = await client.Invoices.CreateAsync(
    new InvoiceCreateParams
    {
        CompanyId = "cmp_...",
        Customer = new InvoiceCustomerInput { Name = "Customer", TaxId = "52998224725", Email = "a@b.com" },
        Service = new InvoiceServiceInput { Code = "0101", MunicipalityCode = "3170206", Description = "Services" },
        Amount = 150.5m,
        CompetenceDate = "2026-01-31",
    },
    idempotencyKey: "my-key-0001"); // optional Idempotency-Key header

if (created.IsSuccess)
    Console.WriteLine(created.Data!.Status); // "queued"
```

### Every page of a list

```csharp
await foreach (var item in client.Credits.IterateHistoryAsync(limit: 100))
{
    if (item.Error is { } error) { /* page failure — logged, never thrown */ break; }
    Console.WriteLine(item.Data!.Id);
}
```

### Downloads

```csharp
var pdf = await client.Invoices.PdfAsync("inv_...");
if (pdf.IsSuccess)
    await File.WriteAllBytesAsync("invoice.pdf", pdf.Data!.Buffer);
```

### Updating a company

```csharp
await client.Companies.UpdateAsync("cmp_...", new CompanyUpdateParams
{
    TradeName = "ACME",      // sent
    MunicipalReg = null,     // explicit null clears the field on the server
    // everything untouched is omitted from the request body
});
```

## Webhook verification

Verify signatures against the **raw** request body — never re-serialize first:

```csharp
// in your ASP.NET Core handler: await request.Body.ReadAsByteArrayAsync()
var valid = CorvoWebhooks.VerifySignature(rawBody, signatureHeader, "whsec_...");
if (!valid) return Results.Unauthorized();
```

HMAC-SHA256, lowercase hex, `sha256=<hex>` header, compared in constant time.

## API surface

| Namespace | Operations |
| --- | --- |
| `client.Companies` | `CreateAsync`, `GetAsync`, `UpdateAsync`, `RemoveAsync`, `ListAsync`, `IterateAsync` |
| `client.Certificates` | `UploadAsync` (multipart), `ListAsync`, `RemoveAsync` |
| `client.Invoices` | `CreateAsync`, `GetAsync`, `ListAsync`, `IterateAsync`, `EventsAsync`, `PdfAsync`, `XmlAsync`, `DownloadUrlAsync`, `SendEmailAsync` |
| `client.Webhooks` | `ListAsync`, `CreateAsync`, `RemoveAsync`, `TestAsync`, `Deliveries.ListAsync`, `Deliveries.IterateAsync`, `Deliveries.StatsAsync`, `Deliveries.RetryAsync` |
| `client.Credits` | `BalanceAsync`, `PlansAsync`, `HistoryAsync`, `IterateHistoryAsync` |
| `client.Billing` | `PlanAsync`, `UsageAsync` |
| `client.Health` | `LiveAsync`, `ReadyAsync` (public, raw JSON) |

Auth is always the `X-API-Key` header (never `Authorization: Bearer`). The path prefix defaults to `v1` and can be overridden with `CorvoOptions.Prefix` for installs with a custom `API_PREFIX`.

## CLI

This package is library-only. The multi-platform Corvo CLI ships with [`corvo-nfse-node`](https://github.com/corvo-technology/corvo-nfse-node) (`npm i -g corvo-nfse-node`).

## The contract (in-depth)

Nine SDKs, one behavior. This SDK implements the same analysis as every other Corvo SDK, mirrored from the canonical Node SDK.

**Auth.** Every request carries `X-API-Key` (never `Authorization: Bearer`). Keys are scoped per tenant; the base URL defaults to `https://api.corvo.tech` and is overridable for self-hosted deployments. All routes live under the `v1` prefix.

**Response envelope.** Endpoints return one of three shapes, and the SDK unwraps them idiomatically: `{ "data": ... }` (single resource), `{ "data": { "props": ... } }` (resource behind a `props` wrapper), or no envelope (health checks, raw downloads). List endpoints return `{ "data": [...], "pagination": { "limit", "hasMore", "nextCursor" } }`. A `204 No Content` maps to an empty result.

**Errors never throw.** Network failures, timeouts, non-2xx responses, and malformed JSON all resolve to a typed error carrying `status`, `code`, `message`, `details`, `requestId`, and `retryAfter` — returned alongside (or as) the result, never raised. `code` falls back to `http_<status>` when the body doesn't carry one; transport-level failures use `network_error` / `timeout` / `invalid_json`. The only operation that raises is the `iterate` helper, and only on the first page's failure.

**Pagination.** List endpoints are cursor-paginated. The SDK exposes both a `list` (single page) and an `iterate`/stream helper that walks `nextCursor` until `hasMore` is false.

**Idempotency.** Invoice create and send-email accept an idempotency key, sent as the `Idempotency-Key` header so retries are safe.

**Webhooks.** Deliveries are signed with HMAC-SHA256 over the raw body. The SDK exposes a timing-safe `verifySignature(rawBody, header, secret)`.

**Surface.** Exactly 28 operations across companies, certificates, invoices, webhooks (incl. nested deliveries), credits, and billing — plus `health/live` and `health/ready`.

## Development

```bash
dotnet restore
dotnet build
dotnet test
```

Tests run in three layers: unit tests over a mock `HttpMessageHandler` (no network), known-vector tests for webhook HMAC verification, and an optional contract test that skips itself unless `CORVO_API_KEY` and `CORVO_BASE_URL` are set.

## License

MIT © Corvo Technology 2026

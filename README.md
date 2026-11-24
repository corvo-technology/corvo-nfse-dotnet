# Corvo.Nfse

**The .NET SDK for the [Corvo](https://corvo.tech) NFS-e API.** Register companies, calculate taxes, issue Brazilian service invoices (NFS-e), follow them by webhook and download the PDF and XML, from .NET 8+.

- **Result-style errors.** API, network and timeout failures come back as `Result<T>` with a `CorvoApiError` carrying the server's stable `Code`, the HTTP `Status`, the `RequestId`, field-level `Details` and `RetryAfter`. Nothing throws for them.
- **Pagination built in.** Every list returns `Paginated<T>` with `{ Limit, HasMore, NextCursor }`, and `IterateAsync(...)` walks all pages and surfaces a page failure as an error result.
- **Safe retries.** Issuing an invoice and sending an email accept an idempotency key.
- **Webhook signature check** in constant time.
- **No package dependencies**: `HttpClient` and `System.Text.Json` come with .NET. Library only, no CLI.

Full API reference: <https://corvo.tech/en/docs>. The OpenAPI contract this SDK follows (0.3.0) is at <https://corvo.tech/openapi.json>.

## Install

```bash
dotnet add package Corvo.Nfse
```

## Quickstart

Start with a `corvo_test_...` key: invoices issued with it have no fiscal value.

```csharp
using Corvo.Nfse;

using var client = new CorvoClient(new CorvoOptions
{
    ApiKey = "corvo_test_...",            // or set CORVO_API_KEY
    // BaseUrl = "https://api.corvo.tech", // default; also read from CORVO_BASE_URL
});

var page = await client.Companies.ListAsync(limit: 10);
if (page.Error is { } error)
{
    // CorvoApiError: Status, Code, RequestId, Details, RetryAfter
    Console.WriteLine($"{error.Code} {error.Status} {error.RequestId}");
}
else
{
    foreach (var company in page.Data!.Data)
        Console.WriteLine(company.LegalName);
}
```

`new CorvoClient(...)` is the only call that throws (`ArgumentException`): it fails fast when no API key is configured or the base URL is invalid.

### Register a company

```csharp
var company = await client.Companies.CreateAsync(new CompanyCreateParams
{
    LegalName = "Empresa Exemplo Ltda",
    TaxId = "11222333000181",
    TaxRegime = TaxRegimes.SimplesNacional, // Mei or SimplesNacional: the only regimes Corvo serves
    SimplesRate = "6.54",              // required for Simples Nacional: effective DAS rate (%), ignored for MEI
    MunicipalityCode = "3550308",      // IBGE code, 7 digits
    DefaultServiceCode = "010101",     // optional: used when an invoice omits Service.Code
});
```

In `live`, also upload the A1 certificate with `client.Certificates.UploadAsync(...)`. The company is ready when `Status == "ready"` and `SetupGaps` is empty.

### Estimate the taxes before issuing

Nothing is issued, stored or charged.

```csharp
var estimate = await client.Invoices.CalculateAsync(new InvoiceCreateParams
{
    CompanyTaxId = "11222333000181",
    Service = new InvoiceServiceInput { Description = "Consultoria" },
    Amount = 1500m,
    CompetenceDate = "2026-10-16",
});
// estimate.Data!.Status is "ready" or "blocked": a fiscal gap is data, not an error.
```

### Issue an invoice

Issuing is asynchronous: a successful call means the request was accepted, not that the invoice is authorized. Follow the status with `GetAsync` or webhooks.

```csharp
var created = await client.Invoices.CreateAsync(
    new InvoiceCreateParams
    {
        CompanyTaxId = "11222333000181", // or CompanyId
        // Customer is optional; when sent, TaxId (CPF or CNPJ) is required.
        Customer = new InvoiceCustomerInput { TaxId = "52998224725", Name = "Cliente", Email = "cliente@example.com" },
        Service = new InvoiceServiceInput { Description = "Consultoria" }, // code and city default to the company's
        Amount = 150.5m,
        CompetenceDate = "2026-10-16",
        Options = new InvoiceOptionsInput { Email = new InvoiceEmailOptions { Send = true } },
    },
    idempotencyKey: "order-42"); // the same key never issues twice

if (created.IsSuccess)
    Console.WriteLine(created.Data!.Invoice!.Status); // "queued"
```

`CreateAsync` returns an `InvoiceCreateResult`: `.Invoice` for an immediate issuance, `.Schedule` when `Options.Type` is `Schedule` or `Recurring`.

### Schedule or repeat an invoice

`SCHEDULE` and `RECURRING` create a schedule (the note only exists when it runs, 09:00 on business days); `CompetenceDate` is the first run, from tomorrow on.

```csharp
var result = await client.Invoices.CreateAsync(new InvoiceCreateParams
{
    CompanyTaxId = "11222333000181",
    Service = new InvoiceServiceInput { Description = "Mensalidade" },
    Amount = 300m,
    CompetenceDate = "2026-12-01",
    Options = new InvoiceOptionsInput
    {
        Type = InvoiceTypes.Recurring,
        Recurrence = new InvoiceRecurrence { Frequency = ScheduleFrequencies.Monthly },
    },
});
var schedule = result.Data!.Schedule!;
var runs = await client.Schedules.ListRunsAsync(schedule.Id);
await client.Schedules.PauseAsync(schedule.Id);
```

### Look up a CNPJ, saved customers and favorite services

```csharp
var cnpj = await client.Cnpj.LookupAsync("11222333000181");        // legal name, situation, address
var customers = await client.Customers.SearchAsync(search: "Maria"); // saved customers, paginated
await client.Customers.UpdateAsync("52998224725", new CustomerUpdateParams { Email = "novo@example.com" });
await client.ServiceFavorites.CreateAsync("cmp_...", new ServiceFavoriteCreateParams
{
    Label = "Consultoria", ServiceCode = "010101", Description = "Consultoria",
});
```

### Accountant package

```csharp
await client.AccountantPackage.UpdateAsync("cmp_...", "contador@example.com"); // null turns the monthly send off
await client.AccountantPackage.SendAsync("cmp_...", "2026-10");
var zip = await client.InvoicePackage.DownloadAsync("cmp_...", "2026-10"); // zip.Data!.Buffer
```

### Import a spreadsheet

```csharp
var started = await client.Imports.StartAsync(await File.ReadAllBytesAsync("notas.csv"), "notas.csv", companyId: "cmp_...");
// poll until Status == "ready", fix invalid rows, then emit
var job = await client.Imports.GetAsync(started.Data!.Id);
await client.Imports.EmitAsync(started.Data.Id, sendEmail: true);
```

### Substitute an authorized invoice

```csharp
await client.Invoices.SubstituteAsync("inv_...", new InvoiceSubstituteParams
{
    Description = "Corrigir a descricao do servico",
    Service = new InvoiceSubstituteService { Description = "Consultoria em TI" },
}, idempotencyKey: "sub-42");
```

To declare a deduction from the ISS base, set `Deduction` (greater than 0 and less than `Amount` minus `Discount`). The invoice is still issued for the full `Amount`, and you are responsible for the declared value.

### Issue up to 50 invoices at once

`CreateBatchAsync` takes 1 to 50 items (they can belong to different companies). Items are checked one by one: valid ones are saved together, invalid ones come back `rejected` and are not saved. Set `IdempotencyKey` on each item, so repeating the batch after a timeout returns the saved ones with `Replayed`.

```csharp
var batch = await client.Invoices.CreateBatchAsync(new[]
{
    new InvoiceBatchItem
    {
        IdempotencyKey = "sheet-row-2",
        Invoice = new InvoiceCreateParams
        {
            CompanyTaxId = "11222333000181",
            Service = new InvoiceServiceInput { Description = "Consultoria" },
            Amount = 150.5m,
            CompetenceDate = "2026-10-16",
        },
    },
});

foreach (var item in batch.Data!.Results.Where(r => r.Status == "rejected"))
    Console.WriteLine($"{item.Index} {item.Error!.Code} {item.Error.Message}");
```

The route allows 10 batches per minute per tenant (`429` with `Retry-After`).

### Cancel an invoice

```csharp
var cancelled = await client.Invoices.CancelAsync("inv_...", new InvoiceCancelParams
{
    Reason = InvoiceCancellationReasons.IssueError, // or ServiceNotProvided, Other
    Description = "Wrong amount on the invoice",
});
```

- `queued`: cancelled at once, `Status` is `"cancelled"` with `CancelledAt` set.
- `authorized`: `Status` is `"cancelling"` while the cancellation is processed at the Sefin. The issuance credit is not refunded.
- `processing`: stays `"processing"` with `CancellationRequestedAt` set and is cancelled once authorized.

`Description` (15 to 255 characters) is required for an authorized or processing invoice and optional for a queued one. Read the invoice again (or listen to the `nfse.invoice.cancelled` and `nfse.invoice.cancellation_rejected` webhooks) to see the outcome: `cancelled`, or back to `authorized` with `LastErrorCode` when the cancellation was rejected.

### Every page of a list

```csharp
await foreach (var item in client.Invoices.IterateAsync(new InvoiceListParams { Status = InvoiceStatuses.Authorized }))
{
    if (item.Error is { } error) { /* a page failed: reported, never thrown */ break; }
    Console.WriteLine(item.Data!.NfseNumber);
}
```

### Download the PDF or XML

The invoice must be `authorized` and have its documents ready.

```csharp
var pdf = await client.Invoices.PdfAsync("inv_...");
if (pdf.IsSuccess)
    await File.WriteAllBytesAsync("nfse.pdf", pdf.Data!.Buffer); // also MimeType and FileName
```

### Update a company

```csharp
await client.Companies.UpdateAsync("cmp_...", new CompanyUpdateParams
{
    TradeName = "ACME",    // sent
    MunicipalReg = null,   // an explicit null clears the field on the server
    // everything untouched is omitted from the request body
});
```

### Verify webhook signatures

Verify against the raw request body, never a re-serialized one:

```csharp
// ASP.NET Core: read the raw bytes of the request body
var valid = CorvoWebhooks.VerifySignature(rawBody, request.Headers["X-Corvo-Signature"], webhookSecret);
if (!valid) return Results.BadRequest();
// Deduplicate by the event id: the same event can be delivered more than once.
```

`WebhookEvents.All` lists the 12 events you can subscribe to. The `webhook.test` event is only sent by `Webhooks.TestAsync`.

## Resources

| Namespace | Operations |
| --- | --- |
| `client.Companies` | `CreateAsync`, `GetAsync`, `UpdateAsync`, `RemoveAsync`, `ListAsync`, `IterateAsync` |
| `client.Certificates` | `UploadAsync`, `ListAsync`, `RemoveAsync` |
| `client.Invoices` | `CreateAsync`, `CreateBatchAsync`, `CalculateAsync`, `GetAsync`, `ListAsync`, `IterateAsync`, `EventsAsync`, `PdfAsync`, `XmlAsync`, `DownloadUrlAsync`, `SendEmailAsync`, `CancelAsync`, `SubstituteAsync`, `OverviewAsync`, `ReviewAsync` |
| `client.Customers` | `SearchAsync`, `IterateAsync`, `GetAsync`, `UpdateAsync`, `DeleteAsync` |
| `client.Cnpj` | `LookupAsync` |
| `client.ServiceFavorites` | `ListAsync`, `CreateAsync`, `DeleteAsync` |
| `client.AccountantPackage` | `GetAsync`, `UpdateAsync`, `SendAsync`, `ListSendsAsync` |
| `client.InvoicePackage` | `SummaryAsync`, `DownloadAsync` |
| `client.DpsSequence` | `GetAsync`, `SetAsync` |
| `client.FiscalProfiles` | `ListAsync`, `UpsertAsync`, `ApproveAsync` |
| `client.ServiceTaxProfiles` | `ListAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `ApproveAsync` |
| `client.FiscalPortal` | `StatusAsync` |
| `client.Schedules` | `ListAsync`, `IterateAsync`, `GetAsync`, `UpdateAsync`, `PauseAsync`, `ResumeAsync`, `CancelAsync`, `ListRunsAsync` |
| `client.Imports` | `StartAsync`, `ListAsync`, `GetAsync`, `EmitAsync`, `ListRowsAsync`, `IterateRowsAsync`, `UpdateRowAsync`, `DiscardAsync`, `DownloadTemplateAsync`, `DownloadErrorsAsync` |
| `client.Integrations` | `ListProvidersAsync` |
| `client.IntegrationConnections` | `ListAsync`, `CreateAsync`, `GetAsync`, `ValidateAsync`, `UpdateCredentialsAsync`, `ActivateAsync`, `PauseAsync`, `DisconnectAsync`, `ListRulesAsync`, `ReplaceRulesAsync`, `ListEventsAsync`, `ListOperationsAsync` |
| `client.IntegrationOperations` | `CompleteCustomerAsync`, `RetryAsync` |
| `client.Webhooks` | `ListAsync`, `CreateAsync`, `RemoveAsync`, `TestAsync`, `Deliveries.ListAsync/IterateAsync/StatsAsync/RetryAsync` |
| `client.Credits` | `BalanceAsync`, `PlansAsync`, `HistoryAsync`, `IterateHistoryAsync`, `PreviewPurchaseAsync` |
| `client.Billing` | `PlanAsync`, `UsageAsync` |
| `client.Plans` | `ListAsync`, `GetAsync`, `QuoteAsync` (public price catalog, outside the 0.3.0 contract) |
| `client.Health` | `LiveAsync`, `ReadyAsync` |

All 92 operations of the contract are covered (`tests/.../OpenApiCoverageTests.cs` checks it against `spec/openapi.json`). Plans are `free`, `solo`, `equipe`, `empresa`, `escala` (and `self-hosted`); there is no overage billing, the balance is the plan notes left plus loose notes bought separately (`Credits.PreviewPurchaseAsync` prices them). A test key cannot reach live webhooks, and webhook URLs on private networks are refused.

Changing plans and the billing portal exist only in the dashboard, so they are not in the SDK.

## Configuration

| Option | Environment | Default |
| --- | --- | --- |
| `ApiKey` | `CORVO_API_KEY` | required |
| `BaseUrl` | `CORVO_BASE_URL` | `https://api.corvo.tech` |
| `Prefix` | none | `v1` |
| `Timeout` | none | 30 seconds |
| `AllowInsecureHttp` | none | `false` (`http://` only for loopback and private hosts) |
| `HttpMessageHandler` | none | the default handler |

## Behavior

- **Auth.** Every request sends `X-API-Key`. Keys are `corvo_test_...` or `corvo_live_...` and belong to one account and environment; the key decides the environment.
- **Responses.** Single resources and list items come back as the API's `data`; lists add `Pagination`. Optional fields are nullable.
- **Errors.** `Code` is the API's `ERROR_*` code, falling back to `http_<status>`; transport failures use `network_error`, `timeout` and `invalid_json`. On `429`, `RetryAfter` holds the seconds to wait.

## Development

```bash
dotnet build
dotnet test   # the contract test is skipped without env
```

To follow a new API version, copy the new contract to `spec/openapi.json`: `OpenApiCoverageTests` fails until every operation has a method.

The contract test runs only when `CORVO_API_KEY` and `CORVO_BASE_URL` point to a reachable instance.

## License

MIT, see [LICENSE](LICENSE).

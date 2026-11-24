using System.Text.Json;
using System.Text.Json.Nodes;
using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>
/// Invoices: create (idempotent), createBatch, calculate, get, list, iterate, events, pdf, xml,
/// downloadUrl, sendEmail (idempotent), cancel, substitute (idempotent), overview, review.
/// </summary>
public sealed class InvoicesResource
{
    private readonly HttpTransport _http;

    internal InvoicesResource(HttpTransport http) => _http = http;

    /// <summary>
    /// Issues an invoice, or schedules one (<c>POST /v1/invoices</c>), optionally idempotent. With
    /// <c>Options.Type</c> <c>IMMEDIATE</c> (the default) the result has <see cref="InvoiceCreateResult.Invoice"/>;
    /// with <c>SCHEDULE</c> or <c>RECURRING</c> it has <see cref="InvoiceCreateResult.Schedule"/> instead
    /// (the note only exists when the schedule runs; follow it with <c>Schedules.ListRunsAsync</c>).
    /// </summary>
    /// <param name="parameters">The invoice payload.</param>
    /// <param name="idempotencyKey">Sent as the <c>Idempotency-Key</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Result<InvoiceCreateResult>> CreateAsync(
        InvoiceCreateParams parameters,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        var result = await _http
            .RequestAsync<JsonElement>(
                HttpMethod.Post,
                "/invoices",
                body: parameters,
                idempotencyKey: idempotencyKey,
                ct: ct)
            .ConfigureAwait(false);
        if (result.Error is not null)
        {
            return new Result<InvoiceCreateResult>(default, result.Error);
        }

        if (result.Data.ValueKind != JsonValueKind.Object)
        {
            return new Result<InvoiceCreateResult>(default, null);
        }

        try
        {
            // A schedule is the only answer with `anchor_on`; an invoice never has it.
            var created = result.Data.TryGetProperty("anchor_on", out _)
                ? new InvoiceCreateResult { Schedule = HttpTransport.Deserialize<Schedule>(result.Data) }
                : new InvoiceCreateResult { Invoice = HttpTransport.Deserialize<Invoice>(result.Data) };
            return new Result<InvoiceCreateResult>(created, null);
        }
        catch (JsonException)
        {
            return new Result<InvoiceCreateResult>(default, new CorvoApiError(202, "invalid_json", "Invalid JSON in response"));
        }
    }

    /// <summary>
    /// Issues 1 to 50 invoices in one call (<c>POST /v1/invoices/batches</c>), for one company or
    /// several. Items are checked one by one: valid ones are saved together, invalid ones come back
    /// <c>rejected</c> and are not saved. Match each result's <c>Index</c> to the item you sent, fix
    /// only the rejected ones and send them again. Set <c>IdempotencyKey</c> on each item; the
    /// <c>Idempotency-Key</c> header is not used by this route.
    /// </summary>
    public Task<Result<InvoiceBatchSummary>> CreateBatchAsync(
        IEnumerable<InvoiceBatchItem> items,
        CancellationToken ct = default)
    {
        var invoices = new JsonArray();
        foreach (var item in items)
        {
            var entry = JsonSerializer.SerializeToNode(item.Invoice, CorvoJson.Options)!.AsObject();
            if (item.IdempotencyKey is not null)
            {
                entry["idempotencyKey"] = item.IdempotencyKey;
            }

            invoices.Add(entry);
        }

        return _http.RequestAsync<InvoiceBatchSummary>(
            HttpMethod.Post,
            "/invoices/batches",
            body: new JsonObject { ["invoices"] = invoices },
            ct: ct);
    }

    /// <summary>
    /// Estimates the taxes of an invoice without issuing, storing or charging anything
    /// (<c>POST /v1/invoices/calculate</c>). A fiscal gap is not an error: the result has
    /// <c>Status == "blocked"</c> with <c>Blocking</c> and <c>PendingFields</c>.
    /// </summary>
    public Task<Result<InvoiceCalculation>> CalculateAsync(InvoiceCreateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<InvoiceCalculation>(
            HttpMethod.Post,
            "/invoices/calculate",
            body: parameters,
            ct: ct);

    /// <summary>Fetches an invoice (<c>GET /v1/invoices/{id}</c>).</summary>
    public Task<Result<Invoice>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<Invoice>(
            HttpMethod.Get,
            $"/invoices/{Uri.EscapeDataString(id)}",
            ct: ct);

    /// <summary>Lists invoices (<c>GET /v1/invoices</c>).</summary>
    public Task<Result<Paginated<Invoice>>> ListAsync(InvoiceListParams? parameters = null, CancellationToken ct = default)
    {
        parameters ??= new InvoiceListParams();
        return _http.ListAsync<Invoice>(
            "/invoices",
            HttpTransport.Query(
                ("companyId", parameters.CompanyId),
                ("status", parameters.Status),
                ("from", parameters.From),
                ("to", parameters.To),
                ("externalId", parameters.ExternalId),
                ("customerTaxId", parameters.CustomerTaxId),
                ("limit", parameters.Limit),
                ("cursor", parameters.Cursor)),
            HttpTransport.MapPlain<Invoice>(),
            ct);
    }

    /// <summary>Walks every page of invoices; a page failure is yielded as an error result.</summary>
    public IAsyncEnumerable<Result<Invoice>> IterateAsync(InvoiceListParams? parameters = null, CancellationToken ct = default)
    {
        parameters ??= new InvoiceListParams();
        return _http.IterateAsync<Invoice>(
            "/invoices",
            HttpTransport.Query(
                ("companyId", parameters.CompanyId),
                ("status", parameters.Status),
                ("from", parameters.From),
                ("to", parameters.To),
                ("externalId", parameters.ExternalId),
                ("customerTaxId", parameters.CustomerTaxId),
                ("limit", parameters.Limit),
                ("cursor", parameters.Cursor)),
            HttpTransport.MapPlain<Invoice>(),
            ct);
    }

    /// <summary>Lists an invoice's status events (<c>GET /v1/invoices/{id}/events</c>).</summary>
    public Task<Result<Paginated<InvoiceEvent>>> EventsAsync(
        string id,
        int? limit = null,
        string? cursor = null,
        CancellationToken ct = default)
        => _http.ListAsync<InvoiceEvent>(
            $"/invoices/{Uri.EscapeDataString(id)}/events",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapPlain<InvoiceEvent>(),
            ct);

    /// <summary>Downloads the rendered PDF (<c>GET /v1/invoices/{id}/pdf</c>).</summary>
    public Task<Result<DownloadedFile>> PdfAsync(string id, CancellationToken ct = default)
        => _http.RequestRawAsync(HttpMethod.Get, $"/invoices/{Uri.EscapeDataString(id)}/pdf", ct: ct);

    /// <summary>Downloads the raw XML fiscal document (<c>GET /v1/invoices/{id}/xml</c>).</summary>
    public Task<Result<DownloadedFile>> XmlAsync(string id, CancellationToken ct = default)
        => _http.RequestRawAsync(HttpMethod.Get, $"/invoices/{Uri.EscapeDataString(id)}/xml", ct: ct);

    /// <summary>Signs an expiring download URL (<c>POST /v1/invoices/{id}/download-url</c>).</summary>
    public Task<Result<InvoiceDownloadUrl>> DownloadUrlAsync(
        string id,
        InvoiceDownloadFormat format,
        CancellationToken ct = default)
        => _http.RequestAsync<InvoiceDownloadUrl>(
            HttpMethod.Post,
            $"/invoices/{Uri.EscapeDataString(id)}/download-url",
            body: new { format = format == InvoiceDownloadFormat.Xml ? InvoiceDownloadFormats.Xml : InvoiceDownloadFormats.Pdf },
            ct: ct);

    /// <summary>
    /// Cancels an invoice (<c>POST /v1/invoices/{id}/cancel</c>). A queued invoice is cancelled at
    /// once (<c>Status == "cancelled"</c>); an authorized one moves to <c>cancelling</c> while the
    /// cancellation is processed, and a processing one stays <c>processing</c> with
    /// <c>CancellationRequestedAt</c> set. Call <see cref="GetAsync"/> later: <c>cancelled</c> when
    /// done, or back to <c>authorized</c> with <c>LastErrorCode</c> when it was rejected.
    /// </summary>
    public Task<Result<Invoice>> CancelAsync(
        string id,
        InvoiceCancelParams parameters,
        CancellationToken ct = default)
        => _http.RequestAsync<Invoice>(
            HttpMethod.Post,
            $"/invoices/{Uri.EscapeDataString(id)}/cancel",
            body: parameters,
            ct: ct);

    /// <summary>
    /// Triggers the invoice email (<c>POST /v1/invoices/{id}/email</c>), optionally idempotent.
    /// <paramref name="email"/> sends it to that address instead of the customer's.
    /// </summary>
    public Task<Result<InvoiceEmailResult>> SendEmailAsync(
        string id,
        string? idempotencyKey = null,
        string? email = null,
        CancellationToken ct = default)
        => _http.RequestAsync<InvoiceEmailResult>(
            HttpMethod.Post,
            $"/invoices/{Uri.EscapeDataString(id)}/email",
            body: email is null ? null : new JsonObject { ["email"] = email },
            idempotencyKey: idempotencyKey,
            ct: ct);

    /// <summary>
    /// Replaces an authorized invoice with a corrected one (<c>POST /v1/invoices/{id}/substitute</c>,
    /// <c>202</c>), optionally idempotent. Only the description (why), <c>Service.Description</c>,
    /// <c>PurchaseOrder</c> and <c>Notes</c> can change. The new invoice has
    /// <see cref="Invoice.SubstitutesInvoiceId"/> set.
    /// </summary>
    public Task<Result<Invoice>> SubstituteAsync(
        string id,
        InvoiceSubstituteParams parameters,
        string? idempotencyKey = null,
        CancellationToken ct = default)
        => _http.RequestAsync<Invoice>(
            HttpMethod.Post,
            $"/invoices/{Uri.EscapeDataString(id)}/substitute",
            body: parameters,
            idempotencyKey: idempotencyKey,
            ct: ct);

    /// <summary>
    /// Counts and recent invoices for the dashboard (<c>GET /v1/invoices/overview</c>);
    /// <paramref name="period"/> is <c>7d</c> (default) or <c>30d</c>.
    /// </summary>
    public Task<Result<InvoiceOverview>> OverviewAsync(string? period = null, CancellationToken ct = default)
        => _http.RequestAsync<InvoiceOverview>(
            HttpMethod.Get,
            "/invoices/overview",
            query: HttpTransport.Query(("period", period)),
            ct: ct);

    /// <summary>
    /// Resolves invoices in <c>manual_review</c> (up to 100): <see cref="InvoiceReviewAction.Retry"/> sends
    /// them back to reconciliation, <see cref="InvoiceReviewAction.Discard"/> drops them
    /// (<c>POST /v1/invoices/review</c>, <c>202</c>). Each result is <c>accepted</c> or <c>skipped</c>.
    /// </summary>
    public Task<Result<InvoiceReviewResult>> ReviewAsync(
        InvoiceReviewAction action,
        IEnumerable<string> invoiceIds,
        CancellationToken ct = default)
        => _http.RequestAsync<InvoiceReviewResult>(
            HttpMethod.Post,
            "/invoices/review",
            body: new
            {
                action = action == InvoiceReviewAction.Retry ? "retry" : "discard",
                invoiceIds = invoiceIds.ToArray(),
            },
            ct: ct);
}

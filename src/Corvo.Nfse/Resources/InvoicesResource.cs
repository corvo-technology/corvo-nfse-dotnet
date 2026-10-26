using System.Text.Json;
using System.Text.Json.Nodes;
using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>
/// Invoices: create (idempotent), createBatch, calculate, get, list, iterate, events, pdf, xml,
/// downloadUrl, sendEmail (idempotent), cancel.
/// </summary>
public sealed class InvoicesResource
{
    private readonly HttpTransport _http;

    internal InvoicesResource(HttpTransport http) => _http = http;

    /// <summary>Issues an invoice (<c>POST /v1/invoices</c>), optionally idempotent.</summary>
    /// <param name="parameters">The invoice payload.</param>
    /// <param name="idempotencyKey">Sent as the <c>Idempotency-Key</c> header.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<Result<Invoice>> CreateAsync(
        InvoiceCreateParams parameters,
        string? idempotencyKey = null,
        CancellationToken ct = default)
        => _http.RequestAsync<Invoice>(
            HttpMethod.Post,
            "/invoices",
            body: parameters,
            idempotencyKey: idempotencyKey,
            ct: ct);

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

    /// <summary>Triggers the invoice email (<c>POST /v1/invoices/{id}/email</c>), optionally idempotent.</summary>
    public Task<Result<InvoiceEmailResult>> SendEmailAsync(
        string id,
        string? idempotencyKey = null,
        CancellationToken ct = default)
        => _http.RequestAsync<InvoiceEmailResult>(
            HttpMethod.Post,
            $"/invoices/{Uri.EscapeDataString(id)}/email",
            idempotencyKey: idempotencyKey,
            ct: ct);
}

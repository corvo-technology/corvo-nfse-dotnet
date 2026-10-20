using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>
/// Invoices: create (idempotent), calculate, get, list, iterate, events, pdf, xml, downloadUrl,
/// sendEmail (idempotent), cancel.
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

    /// <summary>
    /// Requests the cancellation of an invoice (<c>POST /v1/invoices/{id}/cancel</c>) and returns the updated invoice.
    /// </summary>
    /// <remarks>
    /// A <c>queued</c> invoice is cancelled right away (<c>Status == "cancelled"</c>). An authorized invoice goes to
    /// <c>cancelling</c> while the Sefin processes the cancellation event; an invoice still being issued stays
    /// <c>processing</c> with <c>CancellationRequestedAt</c> set. Follow the outcome with <see cref="GetAsync"/> or the
    /// <c>nfse.invoice.cancelled</c> / <c>nfse.invoice.cancellation_rejected</c> webhooks: if the Sefin rejects it, the
    /// invoice goes back to <c>authorized</c> with <c>LastErrorCode</c> and <c>LastErrorMessage</c>. Repeating the
    /// request returns the current state. <c>retry_pending</c>, <c>reconciliation_required</c> and
    /// <c>manual_review</c> invoices fail with <c>409 ERROR_INVOICE_NOT_CANCELLABLE</c>.
    /// </remarks>
    /// <param name="id">The invoice id.</param>
    /// <param name="parameters">The reason (one of <see cref="CancellationReasons"/>) and an optional description.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task<Result<Invoice>> CancelAsync(string id, InvoiceCancelParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<Invoice>(
            HttpMethod.Post,
            $"/invoices/{Uri.EscapeDataString(id)}/cancel",
            body: parameters,
            ct: ct);
}

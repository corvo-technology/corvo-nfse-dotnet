using System.Text.Json;

namespace Corvo.Nfse;

/// <summary>Invoice statuses (pass-through strings).</summary>
public static class InvoiceStatuses
{
    public const string Queued = "queued";
    public const string Processing = "processing";
    public const string Authorized = "authorized";
    public const string Rejected = "rejected";
    public const string RetryPending = "retry_pending";
    public const string ReconciliationRequired = "reconciliation_required";
    public const string ManualReview = "manual_review";
    public const string Failed = "failed";
    public const string Cancelling = "cancelling";
    public const string Cancelled = "cancelled";
}

/// <summary>Why an invoice is being cancelled (pass-through strings).</summary>
public static class InvoiceCancellationReasons
{
    public const string IssueError = "issue_error";
    public const string ServiceNotProvided = "service_not_provided";
    public const string Other = "other";
}

/// <summary>The body of <c>POST /v1/invoices/{id}/cancel</c>.</summary>
public sealed record InvoiceCancelParams
{
    /// <summary>One of <see cref="InvoiceCancellationReasons"/>.</summary>
    public required string Reason { get; init; }

    /// <summary>
    /// 15 to 255 characters. Required to cancel an authorized or processing invoice; optional for a
    /// queued one.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// One item of <c>Invoices.CreateBatchAsync</c>: the invoice to issue plus its own idempotency key.
/// </summary>
public sealed record InvoiceBatchItem
{
    public required InvoiceCreateParams Invoice { get; init; }

    /// <summary>
    /// Up to 255 characters. Repeating it with the same content returns the saved invoice with
    /// <c>Replayed = true</c> instead of issuing again. The <c>Idempotency-Key</c> header is not used
    /// by the batch route.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}

/// <summary>Why a batch item was rejected. Rejected items are not saved.</summary>
public sealed record InvoiceBatchError
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    /// <summary>Field-level issues, when the error has them.</summary>
    public JsonElement? Details { get; init; }
}

/// <summary>Outcome of one batch item.</summary>
public sealed record InvoiceBatchResult
{
    /// <summary>Position of the item in the request, starting at 0.</summary>
    public int Index { get; init; }

    /// <summary><c>accepted</c> (then <see cref="Invoice"/> is set) or <c>rejected</c> (then <see cref="Error"/> is set).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>True when the item repeated an idempotency key already saved with the same content.</summary>
    public bool? Replayed { get; init; }

    public Invoice? Invoice { get; init; }
    public InvoiceBatchError? Error { get; init; }
}

/// <summary>Result of <c>Invoices.CreateBatchAsync</c>: one result per item, in the order sent.</summary>
public sealed record InvoiceBatchSummary
{
    public int Total { get; init; }
    public int Accepted { get; init; }
    public int Rejected { get; init; }
    public IReadOnlyList<InvoiceBatchResult> Results { get; init; } = Array.Empty<InvoiceBatchResult>();
}

/// <summary>Invoice stages (pass-through strings).</summary>
public static class InvoiceStages
{
    public const string Pending = "pending";
    public const string Authorized = "authorized";
    public const string DocumentsReady = "documents_ready";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

/// <summary>
/// The customer (tomador) of an invoice. When a customer is sent, <see cref="TaxId"/> (CPF or CNPJ)
/// is required; leave <see cref="InvoiceCreateParams.Customer"/> null for an invoice without one.
/// </summary>
public sealed record InvoiceCustomerInput
{
    /// <summary>CPF or CNPJ.</summary>
    public required string TaxId { get; init; }

    /// <summary>Customer name.</summary>
    public string? Name { get; init; }

    /// <summary>Customer email; required when the invoice must be emailed.</summary>
    public string? Email { get; init; }

    /// <summary>Optional address.</summary>
    public InvoiceCustomerAddress? Address { get; init; }
}

/// <summary>Optional customer address; empty fields are omitted from the request.</summary>
public sealed record InvoiceCustomerAddress
{
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Zip { get; init; }
    public string? Country { get; init; }
}

/// <summary>
/// The service. Every field is optional: <see cref="Code"/> (national service code, 6 digits)
/// defaults to the company's and then to <c>100901</c>, <see cref="MunicipalityCode"/> (IBGE, 7 digits)
/// to the company's city, and <see cref="Description"/> to the name of the service code.
/// </summary>
public sealed record InvoiceServiceInput
{
    public string? Code { get; init; }
    public string? MunicipalityCode { get; init; }
    public string? Description { get; init; }
}

/// <summary>Email options of an invoice.</summary>
public sealed record InvoiceEmailOptions
{
    /// <summary>Whether to email the NFS-e to the customer on authorization.</summary>
    public bool? Send { get; init; }
}

/// <summary>Per-invoice options.</summary>
public sealed record InvoiceOptionsInput
{
    public InvoiceEmailOptions? Email { get; init; }

    /// <summary><c>IMMEDIATE</c> (default) or <c>SCHEDULE</c> with a future competence date (up to 15 days ahead by default).</summary>
    public string? Type { get; init; }
}

/// <summary>Values for <see cref="InvoiceOptionsInput.Type"/>.</summary>
public static class InvoiceTypes
{
    public const string Immediate = "IMMEDIATE";
    public const string Schedule = "SCHEDULE";
}

/// <summary>The body of <c>POST /v1/invoices</c> and <c>POST /v1/invoices/calculate</c>.</summary>
public sealed record InvoiceCreateParams
{
    /// <summary>Identify the company by <see cref="CompanyId"/> or <see cref="CompanyTaxId"/>.</summary>
    public string? CompanyId { get; init; }
    public string? CompanyTaxId { get; init; }
    public string? ExternalId { get; init; }

    /// <summary>Optional: omit it for an invoice without a customer.</summary>
    public InvoiceCustomerInput? Customer { get; init; }

    public required InvoiceServiceInput Service { get; init; }

    /// <summary>Amount in BRL: positive, up to 2 decimal places.</summary>
    public required decimal Amount { get; init; }

    /// <summary><c>YYYY-MM-DD</c>. A future date needs <c>Options.Type = "SCHEDULE"</c>.</summary>
    public required string CompetenceDate { get; init; }

    /// <summary>Unconditional discount in BRL, from 0 up to <see cref="Amount"/>, at most 2 decimal places.</summary>
    public decimal? Discount { get; init; }

    /// <summary>
    /// ISS base deduction declared by the issuer, in BRL: greater than 0 and less than
    /// <see cref="Amount"/> minus <see cref="Discount"/>, at most 2 decimal places (0 is the same as
    /// omitting it). The invoice is still issued for the full <see cref="Amount"/>; the issuer is
    /// responsible for the declared value.
    /// </summary>
    public decimal? Deduction { get; init; }

    /// <summary>Request the email notification (default false); <c>Options.Email.Send</c> wins when set.</summary>
    public bool? Email { get; init; }

    public InvoiceOptionsInput? Options { get; init; }
}

/// <summary>Filters for <c>GET /v1/invoices</c>.</summary>
public sealed record InvoiceListParams
{
    public string? CompanyId { get; init; }
    public string? Status { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
    public string? ExternalId { get; init; }
    public int? Limit { get; init; }
    public string? Cursor { get; init; }
}

/// <summary>Which document format to sign a download URL for.</summary>
public enum InvoiceDownloadFormat
{
    /// <summary>The rendered PDF document.</summary>
    Pdf,

    /// <summary>The raw XML fiscal document.</summary>
    Xml,
}

/// <summary>Download formats as wire strings.</summary>
public static class InvoiceDownloadFormats
{
    public const string Pdf = "pdf";
    public const string Xml = "xml";
}

/// <summary>A signed, expiring download URL for an invoice document.</summary>
public sealed record InvoiceDownloadUrl
{
    public string DownloadUrl { get; init; } = string.Empty;
    public string ExpiresAt { get; init; } = string.Empty;
    public string? FileName { get; init; }
    public string? MimeType { get; init; }
}

/// <summary>Result of triggering an invoice email.</summary>
public sealed record InvoiceEmailResult
{
    public string EmailId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

/// <summary>A status transition event in an invoice's history.</summary>
public sealed record InvoiceEvent
{
    public string Id { get; init; } = string.Empty;
    public string InvoiceId { get; init; } = string.Empty;
    public string? FromStatus { get; init; }
    public string? ToStatus { get; init; }
    public string? Reason { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>An NFS-e invoice.</summary>
public sealed record Invoice
{
    public string Id { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public int DocumentKeyVersion { get; init; }
    public InvoiceEmailSnapshot EmailSnapshot { get; init; } = new(Send: false);
    public string? ExternalId { get; init; }
    public string? IdempotencyKey { get; init; }
    public string Status { get; init; } = string.Empty;
    public InvoiceCustomerSnapshot CustomerSnapshot { get; init; } = new();
    public InvoiceServiceSnapshot ServiceSnapshot { get; init; } = new();

    /// <summary>Server-computed tax breakdown, passed through untouched.</summary>
    public JsonElement TaxSnapshot { get; init; }

    public decimal Amount { get; init; }

    /// <summary>Set only when a discount was declared.</summary>
    public decimal? Discount { get; init; }

    /// <summary>Set only when an ISS base deduction was declared.</summary>
    public decimal? Deduction { get; init; }

    public string Currency { get; init; } = string.Empty;
    public string CompetenceDate { get; init; } = string.Empty;
    public string? DpsId { get; init; }
    public string? DpsSeries { get; init; }
    public long? DpsNumber { get; init; }
    public string? AccessKey { get; init; }
    public long? NfseNumber { get; init; }
    public string? FiscalProvider { get; init; }
    public string? FiscalSchemaVersion { get; init; }
    public string? IssuedAt { get; init; }
    public string? AuthorizedAt { get; init; }
    public string? LastErrorCode { get; init; }
    public string? LastErrorMessage { get; init; }
    public string? LastErrorProvider { get; init; }
    public string? DocumentsReadyAt { get; init; }
    public string? LastEmailStatus { get; init; }
    public string? LastEmailSentAt { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public string DocumentsStatus { get; init; } = string.Empty;
    public string? EmailStatus { get; init; }
    public string Stage { get; init; } = string.Empty;

    /// <summary>When a scheduled invoice is released (ISO); <c>null</c> for an immediate one.</summary>
    public string? ScheduledFor { get; init; }

    /// <summary>One of <see cref="InvoiceCancellationReasons"/>; null until a cancellation is requested.</summary>
    public string? CancellationReason { get; init; }

    public string? CancellationDescription { get; init; }

    /// <summary>When the invoice reached <c>cancelled</c> (ISO), or null.</summary>
    public string? CancelledAt { get; init; }

    /// <summary>When a cancellation was requested (ISO), or null.</summary>
    public string? CancellationRequestedAt { get; init; }
}

/// <summary>Email settings snapshot embedded in an invoice.</summary>
public sealed record InvoiceEmailSnapshot(bool Send);

/// <summary>Customer data snapshotted at issue time.</summary>
public sealed record InvoiceCustomerSnapshot
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? TaxId { get; init; }
    public InvoiceCustomerAddress? Address { get; init; }
}

/// <summary>Service data snapshotted at issue time.</summary>
public sealed record InvoiceServiceSnapshot
{
    public string? Code { get; init; }
    public string? Description { get; init; }
    public string? MunicipalityCode { get; init; }
}

/// <summary>One tax of a pre-emission estimate. Amounts are decimal strings.</summary>
public sealed record CalculatedTaxAmount
{
    public string Base { get; init; } = string.Empty;

    /// <summary>Rate in percent; absent on <c>IbsTotal</c>, which sums the state and municipal IBS.</summary>
    public string? Rate { get; init; }

    public string Amount { get; init; } = string.Empty;
}

/// <summary>The taxes of a pre-emission estimate.</summary>
public sealed record CalculatedTaxes
{
    public CalculatedTaxAmount? Iss { get; init; }
    public CalculatedTaxAmount? Cbs { get; init; }
    public CalculatedTaxAmount? IbsState { get; init; }
    public CalculatedTaxAmount? IbsCity { get; init; }
    public CalculatedTaxAmount? IbsTotal { get; init; }
}

/// <summary>Why an estimate is blocked.</summary>
public sealed record CalculationBlocking(string Code, string Message, string Step);

/// <summary>A field missing for the estimate.</summary>
public sealed record CalculationPendingField(string Field, string Reason);

/// <summary>
/// The pre-emission estimate of <c>POST /v1/invoices/calculate</c>. <see cref="Status"/> is <c>ready</c> or
/// <c>blocked</c>: a fiscal gap is data, not an error.
/// </summary>
public sealed record InvoiceCalculation
{
    public bool PreEmission { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Disclaimer { get; init; }
    public string? CompetenceDate { get; init; }
    public string? TaxRegime { get; init; }
    public JsonElement Classification { get; init; }
    public JsonElement Applicability { get; init; }
    public CalculatedTaxes Taxes { get; init; } = new();
    public JsonElement Withholdings { get; init; }
    public string? NetReceivableAmount { get; init; }
    public bool? CollectionWaived { get; init; }
    public JsonElement Calculation { get; init; }
    public JsonElement Warnings { get; init; }
    public IReadOnlyList<CalculationPendingField> PendingFields { get; init; } = Array.Empty<CalculationPendingField>();
    public CalculationBlocking? Blocking { get; init; }
    public JsonElement Trace { get; init; }
}

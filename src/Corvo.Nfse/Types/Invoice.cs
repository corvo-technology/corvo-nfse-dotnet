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
    public const string Cancelled = "cancelled";
}

/// <summary>
/// The customer (tomador) of an invoice. Send <see cref="TaxId"/> (CPF or CNPJ) for a Brazilian
/// customer, or <see cref="Foreign"/> for one outside Brazil (never both); leave
/// <see cref="InvoiceCreateParams.Customer"/> null for an invoice without a customer.
/// </summary>
public sealed record InvoiceCustomerInput
{
    /// <summary>CPF (11 digits) or CNPJ (14 digits), punctuation optional. Not allowed with <see cref="Foreign"/>.</summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Customer name. May be omitted when the customer was already saved (it is filled from the
    /// CPF/CNPJ); required with <see cref="Foreign"/>.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>Customer email; required when the invoice must be emailed.</summary>
    public string? Email { get; init; }

    /// <summary>Optional address. Not allowed with <see cref="Foreign"/>.</summary>
    public InvoiceCustomerAddress? Address { get; init; }

    /// <summary>A customer outside Brazil. Cannot be combined with <see cref="TaxId"/> or <see cref="Address"/>.</summary>
    public InvoiceForeignCustomer? Foreign { get; init; }
}

/// <summary>Optional customer address; empty fields are omitted from the request.</summary>
public sealed record InvoiceCustomerAddress
{
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Zip { get; init; }
    public string? Country { get; init; }
    public string? MunicipalityCode { get; init; }
}

/// <summary>Why a foreign customer has no tax id (<see cref="InvoiceForeignCustomer.NoTaxIdReason"/>).</summary>
public static class ForeignNoTaxIdReasons
{
    public const string Exempt = "exempt";
    public const string NotRequired = "not_required";
}

/// <summary>A customer outside Brazil (<c>customer.foreign</c>). Send <see cref="TaxId"/> or <see cref="NoTaxIdReason"/>.</summary>
public sealed record InvoiceForeignCustomer
{
    public string? TaxId { get; init; }

    /// <summary>One of <see cref="ForeignNoTaxIdReasons"/>.</summary>
    public string? NoTaxIdReason { get; init; }

    public required string Country { get; init; }
    public required string PostalCode { get; init; }
    public required string City { get; init; }
    public required string Region { get; init; }
    public required string Street { get; init; }
    public required string Number { get; init; }
    public required string District { get; init; }
    public string? Complement { get; init; }
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

    /// <summary>Optional RTC fiscal classification, passed through to the fiscal engine.</summary>
    public InvoiceServiceFiscal? Fiscal { get; init; }

    /// <summary>Optional fiscal classification profile (UUID).</summary>
    public string? ServiceProfileId { get; init; }
}

/// <summary>RTC fiscal classification of a service.</summary>
public sealed record InvoiceServiceFiscal
{
    public string? ClassCode { get; init; }
    public string? Cst { get; init; }
    public string? NbsCode { get; init; }
    public string? OperationIndicator { get; init; }
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

    /// <summary>
    /// <c>IMMEDIATE</c> (default), <c>SCHEDULE</c> or <c>RECURRING</c>. The last two do not issue now:
    /// they create a <see cref="Schedule"/> (see <see cref="InvoiceCreateResult"/>) and
    /// <see cref="InvoiceCreateParams.CompetenceDate"/> is the date of the first run, from tomorrow on.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>Required when <see cref="Type"/> is <c>RECURRING</c>; not allowed otherwise.</summary>
    public InvoiceRecurrence? Recurrence { get; init; }
}

/// <summary>Values for <see cref="InvoiceOptionsInput.Type"/>.</summary>
public static class InvoiceTypes
{
    public const string Immediate = "IMMEDIATE";
    public const string Schedule = "SCHEDULE";
    public const string Recurring = "RECURRING";
}

/// <summary>How a recurring invoice repeats (<c>options.recurrence</c>).</summary>
public sealed record InvoiceRecurrence
{
    /// <summary>One of <see cref="ScheduleFrequencies"/>.</summary>
    public required string Frequency { get; init; }

    /// <summary>Last day (<c>YYYY-MM-DD</c>) a run may happen; open-ended when omitted.</summary>
    public string? EndsOn { get; init; }
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

    /// <summary>
    /// <c>YYYY-MM-DD</c>. A future date needs <c>Options.Type = "SCHEDULE"</c> (up to 15 days ahead)
    /// or <c>"RECURRING"</c> (up to 31); it is then the date of the first run, from tomorrow on.
    /// </summary>
    public required string CompetenceDate { get; init; }

    /// <summary>Customer purchase order (DPS <c>xPed</c>), up to 60 characters.</summary>
    public string? PurchaseOrder { get; init; }

    /// <summary>Complementary information printed on the invoice (DPS <c>xInfComp</c>), up to 2000 characters.</summary>
    public string? Notes { get; init; }

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

    /// <summary>CPF or CNPJ of the customer (digits).</summary>
    public string? CustomerTaxId { get; init; }

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

    /// <summary><c>portal</c> when the event came from the national portal; otherwise <c>null</c>.</summary>
    public string? Source { get; init; }

    public string? PortalEvent { get; init; }
    public string? PortalNsu { get; init; }
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

    /// <summary>On a substitute invoice, the id of the invoice it replaces.</summary>
    public string? SubstitutesInvoiceId { get; init; }

    /// <summary>On a substitute invoice, why it was issued.</summary>
    public string? SubstitutionDescription { get; init; }

    /// <summary><c>taker_rejected</c> or <c>blocked</c>, from the national portal; otherwise null.</summary>
    public string? PortalFlag { get; init; }
}

/// <summary>Values of <see cref="Invoice.PortalFlag"/>.</summary>
public static class InvoicePortalFlags
{
    public const string TakerRejected = "taker_rejected";
    public const string Blocked = "blocked";
}

/// <summary>
/// The answer of <c>POST /v1/invoices</c>: an <see cref="Invoice"/> for <c>IMMEDIATE</c>, a
/// <see cref="Schedule"/> for <c>SCHEDULE</c> and <c>RECURRING</c>. Exactly one of the two is set.
/// </summary>
public sealed record InvoiceCreateResult
{
    /// <summary>The queued invoice (immediate issuance); null when a schedule was created.</summary>
    public Invoice? Invoice { get; init; }

    /// <summary>The created schedule (<c>SCHEDULE</c> / <c>RECURRING</c>); null when an invoice was queued.</summary>
    public Schedule? Schedule { get; init; }

    /// <summary>True when <see cref="Schedule"/> is set instead of <see cref="Invoice"/>.</summary>
    public bool IsSchedule => Schedule is not null;
}

/// <summary>The body of <c>POST /v1/invoices/{id}/substitute</c>. Only these fields can change.</summary>
public sealed record InvoiceSubstituteParams
{
    /// <summary>Why the invoice is being substituted.</summary>
    public required string Description { get; init; }

    public string? Notes { get; init; }
    public string? PurchaseOrder { get; init; }

    /// <summary>New service description; the rest of the service cannot change.</summary>
    public InvoiceSubstituteService? Service { get; init; }
}

/// <summary>The service fields a substitute may change.</summary>
public sealed record InvoiceSubstituteService
{
    public string? Description { get; init; }
}

/// <summary>What <c>Invoices.ReviewAsync</c> does with invoices in <c>manual_review</c>.</summary>
public enum InvoiceReviewAction
{
    /// <summary>Send the invoices back to reconciliation.</summary>
    Retry,

    /// <summary>Discard them.</summary>
    Discard,
}

/// <summary>One result of <c>Invoices.ReviewAsync</c>.</summary>
public sealed record InvoiceReviewItem
{
    public string Id { get; init; } = string.Empty;

    /// <summary><c>accepted</c> or <c>skipped</c>.</summary>
    public string Outcome { get; init; } = string.Empty;

    /// <summary>When skipped: <c>not_found</c>, <c>not_in_manual_review</c> or <c>changed</c>.</summary>
    public string? Reason { get; init; }
}

/// <summary>Result of <c>Invoices.ReviewAsync</c>.</summary>
public sealed record InvoiceReviewResult
{
    public IReadOnlyList<InvoiceReviewItem> Results { get; init; } = Array.Empty<InvoiceReviewItem>();
}

/// <summary>Counts of the overview.</summary>
public sealed record InvoiceOverviewCounts
{
    public int Authorized { get; init; }
    public int Processing { get; init; }
    public int Rejected { get; init; }
}

/// <summary>One day of the overview chart.</summary>
public sealed record InvoiceOverviewDay
{
    public string Date { get; init; } = string.Empty;
    public int Count { get; init; }
}

/// <summary>Response of <c>GET /v1/invoices/overview</c>.</summary>
public sealed record InvoiceOverview
{
    public InvoiceOverviewCounts Counts { get; init; } = new();
    public IReadOnlyList<InvoiceOverviewDay> DailyActivity { get; init; } = Array.Empty<InvoiceOverviewDay>();

    /// <summary><c>7d</c> or <c>30d</c>.</summary>
    public string Period { get; init; } = string.Empty;

    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string Timezone { get; init; } = string.Empty;
    public IReadOnlyList<Invoice> RecentInvoices { get; init; } = Array.Empty<Invoice>();
}

/// <summary>Whether the national NFS-e portal is answering (<c>GET /v1/fiscal-portal/status</c>).</summary>
public sealed record PortalStatus
{
    public bool Available { get; init; }
    public string Environment { get; init; } = string.Empty;
    public string? RetryAt { get; init; }
    public string? Since { get; init; }
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

    /// <summary>Set for a customer outside Brazil.</summary>
    public JsonElement? Foreign { get; init; }
}

/// <summary>Service data snapshotted at issue time.</summary>
public sealed record InvoiceServiceSnapshot
{
    public string? Code { get; init; }
    public string? Description { get; init; }
    public string? MunicipalityCode { get; init; }
    public string? PurchaseOrder { get; init; }
    public string? Notes { get; init; }
    public string? ServiceProfileId { get; init; }

    /// <summary>RTC fiscal classification (<c>nbsCode</c>, <c>cst</c>, <c>classCode</c>, ...).</summary>
    public IReadOnlyDictionary<string, string>? Fiscal { get; init; }
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

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

/// <summary>Customer payload for invoice creation.</summary>
public sealed record InvoiceCustomerInput
{
    public required string Name { get; init; }
    public required string TaxId { get; init; }
    public string? Email { get; init; }
    public InvoiceCustomerAddress? Address { get; init; }
}

/// <summary>Customer address (all fields optional on the wire).</summary>
public sealed record InvoiceCustomerAddress
{
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Zip { get; init; }
    public string? Country { get; init; }
}

/// <summary>Service description for invoice creation.</summary>
public sealed record InvoiceServiceInput
{
    public required string Code { get; init; }
    public string? MunicipalityCode { get; init; }
    public string? Description { get; init; }
}

/// <summary>Email options for invoice creation (<c>options.email.send</c>).</summary>
public sealed record InvoiceEmailOptions
{
    public bool? Send { get; init; }
}

/// <summary>Email behavior options for invoice creation.</summary>
public sealed record InvoiceOptionsInput
{
    public InvoiceEmailOptions? Email { get; init; }
}

/// <summary>Body for <c>POST /v1/invoices</c>.</summary>
public sealed record InvoiceCreateParams
{
    public string? CompanyId { get; init; }
    public string? CompanyTaxId { get; init; }
    public string? ExternalId { get; init; }
    public string? CorrelationId { get; init; }
    public required InvoiceCustomerInput Customer { get; init; }
    public required InvoiceServiceInput Service { get; init; }
    public required decimal Amount { get; init; }
    public required string CompetenceDate { get; init; }
    public string? Email { get; init; }
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
    public InvoiceEmailSnapshot EmailSnapshot { get; init; } = new();
    public string? ExternalId { get; init; }
    public string? IdempotencyKey { get; init; }
    public string Status { get; init; } = string.Empty;
    public InvoiceCustomerSnapshot CustomerSnapshot { get; init; } = new();
    public InvoiceServiceSnapshot ServiceSnapshot { get; init; } = new();

    /// <summary>Server-computed tax breakdown, passed through untouched.</summary>
    public JsonElement TaxSnapshot { get; init; }

    public decimal Amount { get; init; }
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
}

/// <summary>Email settings snapshot embedded in an invoice.</summary>
public sealed record InvoiceEmailSnapshot(bool Send);

/// <summary>Customer data snapshotted at issue time.</summary>
public sealed record InvoiceCustomerSnapshot
{
    public string Name { get; init; } = string.Empty;
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

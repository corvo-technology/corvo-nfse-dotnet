using System.Text.Json;

namespace Corvo.Nfse;

/// <summary>The event types a webhook can subscribe to.</summary>
public static class WebhookEvents
{
    public const string InvoiceAuthorized = "nfse.invoice.authorized";
    public const string InvoiceRejected = "nfse.invoice.rejected";
    public const string InvoiceFailed = "nfse.invoice.failed";
    public const string InvoiceReconciliationRequired = "nfse.invoice.reconciliation_required";
    public const string InvoiceManualReview = "nfse.invoice.manual_review";
    public const string InvoiceDocumentsReady = "nfse.invoice.documents_ready";
    public const string InvoiceEmailSent = "nfse.invoice.email_sent";
    public const string InvoiceEmailFailed = "nfse.invoice.email_failed";
    public const string InvoiceFiscalDiscrepancy = "nfse.invoice.fiscal_discrepancy";
    public const string InvoiceCancelled = "nfse.invoice.cancelled";
    public const string InvoiceCancellationRejected = "nfse.invoice.cancellation_rejected";
    public const string InvoiceTakerRejected = "nfse.invoice.taker_rejected";

    /// <summary>Only sent by <c>Webhooks.TestAsync</c>; it cannot be subscribed.</summary>
    public const string WebhookTest = "webhook.test";

    /// <summary>All twelve subscribable events.</summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        InvoiceAuthorized,
        InvoiceRejected,
        InvoiceFailed,
        InvoiceReconciliationRequired,
        InvoiceManualReview,
        InvoiceDocumentsReady,
        InvoiceEmailSent,
        InvoiceEmailFailed,
        InvoiceFiscalDiscrepancy,
        InvoiceCancelled,
        InvoiceCancellationRejected,
        InvoiceTakerRejected,
    };
}

/// <summary>A webhook subscription.</summary>
public sealed record Webhook
{
    public string Id { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public IReadOnlyList<string> Events { get; init; } = Array.Empty<string>();
    public bool Active { get; init; }

    /// <summary>
    /// <c>test</c> or <c>live</c>: a webhook created with an API key only receives that key's
    /// environment, and only a key of the same environment lists it.
    /// </summary>
    public string? Environment { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

/// <summary>A webhook subscription with its signing secret (only returned on create).</summary>
public sealed record WebhookWithSecret
{
    public string Id { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public IReadOnlyList<string> Events { get; init; } = Array.Empty<string>();
    public bool Active { get; init; }
    public string? Environment { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
}

/// <summary>
/// Body for <c>POST /v1/webhooks</c>. The URL must be public: addresses that resolve to private,
/// loopback, link-local, CGNAT, benchmarking, multicast or NAT64/6to4 ranges are refused when the
/// webhook is registered and when it is delivered.
/// </summary>
public sealed record WebhookCreateParams
{
    public required string Url { get; init; }
    public required IReadOnlyList<string> Events { get; init; }
}

/// <summary>Result of removing a webhook (<c>{ deleted }</c>).</summary>
public sealed record WebhookDeleteResult(bool Deleted);

/// <summary>Result of firing a test event: <c>{ sent: true }</c> or <c>{ sent: false, error, statusCode? }</c>.</summary>
public sealed record WebhookTestResult
{
    public bool Sent { get; init; }
    public string? Error { get; init; }
    public int? StatusCode { get; init; }
}

/// <summary>Result of retrying a delivery (<c>{ retried }</c>).</summary>
public sealed record WebhookRetryResult(bool Retried);

/// <summary>A single delivery attempt record.</summary>
public sealed record WebhookDelivery
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string WebhookConfigId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, JsonElement> Payload { get; init; } = new Dictionary<string, JsonElement>();
    public int? StatusCode { get; init; }
    public string? ResponseBody { get; init; }
    public string? ErrorMessage { get; init; }
    public string Status { get; init; } = string.Empty;
    public int? DurationMs { get; init; }
    public int RetryCount { get; init; }
    public string? NextRetryAt { get; init; }
    public string? LastError { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>Delivery counters for a trailing window.</summary>
public sealed record WebhookDeliveryStats
{
    public int Total { get; init; }
    public int Sent { get; init; }
    public int Failed { get; init; }
    public int Pending { get; init; }
    public int Delayed { get; init; }
}

/// <summary>Filters for <c>GET /v1/webhooks/deliveries</c>.</summary>
public sealed record WebhookDeliveryListParams
{
    public int? Hours { get; init; }
    public string? Environment { get; init; }
    public int? Limit { get; init; }
    public string? Cursor { get; init; }
}

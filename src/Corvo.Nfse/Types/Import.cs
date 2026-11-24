using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corvo.Nfse;

/// <summary>Import statuses (pass-through strings).</summary>
public static class ImportStatuses
{
    public const string Processing = "processing";
    public const string Ready = "ready";
    public const string Emitting = "emitting";
    public const string Done = "done";
    public const string Failed = "failed";
}

/// <summary>Import row statuses (pass-through strings).</summary>
public static class ImportRowStatuses
{
    public const string Valid = "valid";
    public const string Invalid = "invalid";
    public const string Queued = "queued";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
}

/// <summary>Row counters of an import. Wire names are <c>snake_case</c>.</summary>
public sealed record ImportCounters
{
    public int Total { get; init; }
    public int Valid { get; init; }
    public int Invalid { get; init; }
    public int Pending { get; init; }
    public int Accepted { get; init; }
    public int Authorized { get; init; }
    public int Failed { get; init; }

    [JsonPropertyName("valid_amount")]
    public decimal ValidAmount { get; init; }

    [JsonPropertyName("pending_amount")]
    public decimal PendingAmount { get; init; }

    [JsonPropertyName("authorized_amount")]
    public decimal AuthorizedAmount { get; init; }

    [JsonPropertyName("failed_amount")]
    public decimal FailedAmount { get; init; }

    [JsonPropertyName("awaiting_amount")]
    public decimal AwaitingAmount { get; init; }

    [JsonPropertyName("awaiting_credits")]
    public int AwaitingCredits { get; init; }
}

/// <summary>A spreadsheet import. Wire names are <c>snake_case</c>.</summary>
public sealed record ImportJob
{
    public string Id { get; init; } = string.Empty;

    /// <summary><see cref="ImportStatuses"/>.</summary>
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; init; } = string.Empty;

    [JsonPropertyName("company_id")]
    public string? CompanyId { get; init; }

    [JsonPropertyName("rows_expected")]
    public int? RowsExpected { get; init; }

    public ImportCounters Counters { get; init; } = new();

    [JsonPropertyName("unknown_columns")]
    public IReadOnlyList<string> UnknownColumns { get; init; } = Array.Empty<string>();

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = string.Empty;

    [JsonPropertyName("emit_started_at")]
    public string? EmitStartedAt { get; init; }

    [JsonPropertyName("emit_finished_at")]
    public string? EmitFinishedAt { get; init; }
}

/// <summary>Answer of <c>POST /v1/imports</c> (<c>202</c>).</summary>
public sealed record ImportStart
{
    public string Id { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    /// <summary>Set when the same file was already imported: that import.</summary>
    [JsonPropertyName("duplicate_of")]
    public string? DuplicateOf { get; init; }
}

/// <summary>One row of an import. Wire names are <c>snake_case</c>.</summary>
public sealed record ImportRow
{
    public int Line { get; init; }

    /// <summary><see cref="ImportRowStatuses"/>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The row's values as parsed.</summary>
    public JsonElement Data { get; init; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; init; }
}

/// <summary>Filters for <c>GET /v1/imports/{id}/rows</c>.</summary>
public sealed record ImportRowListParams
{
    /// <summary>One of <see cref="ImportRowStatuses"/>.</summary>
    public string? Status { get; init; }

    public int? Limit { get; init; }
    public string? Cursor { get; init; }
}

/// <summary>Error codes of the import routes that have no <c>ERROR_</c> prefix.</summary>
public static class ImportErrorCodes
{
    public const string PackageTooLarge = "PACKAGE_TOO_LARGE";
}

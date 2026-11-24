using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Corvo.Nfse;

/// <summary>Recurrence frequencies (pass-through strings).</summary>
public static class ScheduleFrequencies
{
    public const string Weekly = "WEEKLY";
    public const string Biweekly = "BIWEEKLY";
    public const string Monthly = "MONTHLY";
}

/// <summary>Schedule statuses (pass-through strings).</summary>
public static class ScheduleStatuses
{
    public const string Active = "active";
    public const string Paused = "paused";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
}

/// <summary>
/// A scheduled (<c>kind == "once"</c>) or recurring invoice. The note exists only when the schedule
/// runs (09:00 America/Sao_Paulo, Monday to Friday). Wire names are <c>snake_case</c>.
/// </summary>
public sealed record Schedule
{
    public string Id { get; init; } = string.Empty;

    /// <summary><c>once</c> or <c>recurring</c>.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary><c>WEEKLY</c>, <c>BIWEEKLY</c> or <c>MONTHLY</c>; null for <c>once</c>.</summary>
    public string? Frequency { get; init; }

    /// <summary><see cref="ScheduleStatuses"/>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary><c>customer</c> or <c>failures</c> when paused.</summary>
    [JsonPropertyName("pause_reason")]
    public string? PauseReason { get; init; }

    [JsonPropertyName("company_id")]
    public string CompanyId { get; init; } = string.Empty;

    [JsonPropertyName("anchor_on")]
    public string AnchorOn { get; init; } = string.Empty;

    [JsonPropertyName("next_run_on")]
    public string? NextRunOn { get; init; }

    [JsonPropertyName("ends_on")]
    public string? EndsOn { get; init; }

    /// <summary>Failed attempts in a row on the pending run (0 to 3).</summary>
    public int Attempts { get; init; }

    [JsonPropertyName("last_run_on")]
    public string? LastRunOn { get; init; }

    /// <summary>The invoice body each run issues.</summary>
    public JsonElement Invoice { get; init; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>One attempt to run a schedule (<c>GET /v1/schedules/{id}/runs</c>).</summary>
public sealed record ScheduleRun
{
    public string Id { get; init; } = string.Empty;

    /// <summary>The due date (may be a weekend day).</summary>
    [JsonPropertyName("occurrence_on")]
    public string OccurrenceOn { get; init; } = string.Empty;

    /// <summary>The day it actually ran.</summary>
    [JsonPropertyName("ran_on")]
    public string RanOn { get; init; } = string.Empty;

    public int Attempt { get; init; }

    /// <summary><c>issued</c> or <c>failed</c>.</summary>
    public string Outcome { get; init; } = string.Empty;

    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; init; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>Filters for <c>GET /v1/schedules</c>.</summary>
public sealed record ScheduleListParams
{
    /// <summary>One of <see cref="ScheduleStatuses"/>.</summary>
    public string? Status { get; init; }

    public string? CompanyId { get; init; }
    public int? Limit { get; init; }
    public string? Cursor { get; init; }
}

/// <summary>
/// Body of <c>PATCH /v1/schedules/{id}</c>; it applies to the next runs only. Unset fields are
/// kept; an explicit <c>null</c> <see cref="EndsOn"/> makes a recurring schedule open-ended.
/// </summary>
public sealed record ScheduleUpdateParams
{
    /// <summary>New date of the next run (<c>YYYY-MM-DD</c>).</summary>
    public string? AnchorOn { get; init; }

    public Optional<string> EndsOn { get; init; }

    /// <summary>One of <see cref="ScheduleFrequencies"/>.</summary>
    public string? Frequency { get; init; }

    /// <summary>Invoice body fields to change; merged over the stored body and validated like an emission.</summary>
    public JsonObject? Invoice { get; init; }

    internal JsonObject ToJson()
    {
        var body = new JsonObject();
        if (AnchorOn is not null)
        {
            body["anchorOn"] = AnchorOn;
        }

        if (EndsOn.IsSet)
        {
            body["endsOn"] = EndsOn.Value;
        }

        if (Frequency is not null)
        {
            body["frequency"] = Frequency;
        }

        if (Invoice is not null)
        {
            body["invoice"] = JsonNode.Parse(Invoice.ToJsonString());
        }

        return body;
    }
}

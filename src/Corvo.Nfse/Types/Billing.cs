using System.Text.Json.Serialization;

namespace Corvo.Nfse;

/// <summary>The tenant's current plan.</summary>
/// <remarks>Wire names for this payload are snake_case and are pinned with
/// <see cref="JsonPropertyNameAttribute"/> — pass-through, no re-casing.</remarks>
public sealed record BillingPlanInfo
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("monthly_allowance")]
    public int MonthlyAllowance { get; init; }

    [JsonPropertyName("daily_limit")]
    public int? DailyLimit { get; init; }

    [JsonPropertyName("max_active_companies")]
    public int MaxActiveCompanies { get; init; }

    [JsonPropertyName("monthly_price_cents")]
    public int MonthlyPriceCents { get; init; }
}

/// <summary>The tenant's subscription state.</summary>
public sealed record BillingSubscription
{
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("paid_until")]
    public string? PaidUntil { get; init; }

    [JsonPropertyName("cancel_at_period_end")]
    public bool CancelAtPeriodEnd { get; init; }

    [JsonPropertyName("scheduled_plan")]
    public string? ScheduledPlan { get; init; }

    public bool Blocked { get; init; }
}

/// <summary>Usage for the current period.</summary>
public sealed record BillingUsage
{
    [JsonPropertyName("overage_units")]
    public int OverageUnits { get; init; }

    [JsonPropertyName("overage_amount_cents")]
    public int OverageAmountCents { get; init; }

    [JsonPropertyName("hard_limit")]
    public decimal? HardLimit { get; init; }

    [JsonPropertyName("credits_used")]
    public int CreditsUsed { get; init; }

    [JsonPropertyName("credits_reserved")]
    public int CreditsReserved { get; init; }

    [JsonPropertyName("credits_remaining")]
    public int CreditsRemaining { get; init; }

    [JsonPropertyName("monthly_allowance")]
    public int MonthlyAllowance { get; init; }

    [JsonPropertyName("daily_usage")]
    public int DailyUsage { get; init; }

    [JsonPropertyName("daily_limit")]
    public int? DailyLimit { get; init; }

    [JsonPropertyName("active_companies")]
    public int ActiveCompanies { get; init; }

    [JsonPropertyName("max_active_companies")]
    public int MaxActiveCompanies { get; init; }

    [JsonPropertyName("period_start")]
    public string? PeriodStart { get; init; }

    [JsonPropertyName("period_end")]
    public string? PeriodEnd { get; init; }
}

/// <summary>Response of <c>GET /v1/billing/plan</c>.</summary>
public sealed record BillingPlanResponse
{
    public BillingPlanInfo Plan { get; init; } = new();
    public BillingSubscription Subscription { get; init; } = new();
    public BillingUsage Usage { get; init; } = new();
}

/// <summary>Response of <c>GET /v1/billing/usage</c> (camelCase wire).</summary>
public sealed record QuotaStatus
{
    public bool CanEmit { get; init; }
    public int CreditsRemaining { get; init; }
    public int CreditsUsed { get; init; }
    public int CreditsReserved { get; init; }
    public int MonthlyAllowance { get; init; }
    public int DailyUsage { get; init; }
    public int? DailyLimit { get; init; }
    public int ActiveCompanies { get; init; }
    public int MaxActiveCompanies { get; init; }
    public string? PeriodStart { get; init; }
    public string? PeriodEnd { get; init; }
}

using System.Text.Json.Serialization;

namespace Corvo.Nfse;

/// <summary>The tenant's current plan.</summary>
/// <remarks>Wire names for this payload are snake_case and are pinned with
/// <see cref="JsonPropertyNameAttribute"/> — pass-through, no re-casing.</remarks>
public sealed record BillingPlanInfo
{
    /// <summary>Open string: <c>free</c>, <c>self-hosted</c>, <c>solo</c>, <c>equipe</c>, <c>empresa</c>, <c>escala</c>, ...</summary>
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

    /// <summary>Whether the Stripe customer portal can be opened from the dashboard.</summary>
    [JsonPropertyName("stripe_portal_available")]
    public bool StripePortalAvailable { get; init; }

    [JsonPropertyName("stripe_subscription")]
    public bool StripeSubscription { get; init; }

    /// <summary>The Pix subscription in progress or active; <c>null</c> when there is none.</summary>
    [JsonPropertyName("pix_subscription")]
    public BillingPixSubscription? PixSubscription { get; init; }
}

/// <summary>A Pix subscription (<c>subscription.pix_subscription</c>). Wire names are camelCase here.</summary>
public sealed record BillingPixSubscription
{
    public string? BrCode { get; init; }
    public string? PaymentId { get; init; }
    public string? PaymentLinkUrl { get; init; }
    public string PlanSlug { get; init; } = string.Empty;

    /// <summary><c>active</c> or <c>pending</c>.</summary>
    public string Status { get; init; } = string.Empty;
}

/// <summary>Usage for the current period.</summary>
public sealed record BillingUsage
{
    /// <summary>Loose (avulso) notes bought separately.</summary>
    [JsonPropertyName("avulso_balance")]
    public int AvulsoBalance { get; init; }

    [JsonPropertyName("hard_limit")]
    public int HardLimit { get; init; }

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

/// <summary>
/// Response of <c>GET /v1/billing/plan</c>. A workspace without a plan answers
/// <c>404</c> with <c>Code == "NO_PLAN_ASSIGNED"</c> (see <see cref="BillingErrorCodes"/>).
/// </summary>
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

    /// <summary>Set when <c>CanEmit</c> is false: no_credits, daily_limit_exceeded, company_limit_exceeded or plan_blocked.</summary>
    public string? Reason { get; init; }
}

/// <summary>Error codes of the billing routes (they have no <c>ERROR_</c> prefix).</summary>
public static class BillingErrorCodes
{
    /// <summary><c>GET /v1/billing/plan</c> for a workspace that has no plan.</summary>
    public const string NoPlanAssigned = "NO_PLAN_ASSIGNED";
}

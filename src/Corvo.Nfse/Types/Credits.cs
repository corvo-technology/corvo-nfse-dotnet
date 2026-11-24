using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corvo.Nfse;

/// <summary>The tenant's credit balance (<c>GET /v1/credits</c>). There is no overage billing.</summary>
public sealed record CreditBalance
{
    /// <summary>Plan notes left plus loose (avulso) notes; the plan notes of a lapsed plan count 0.</summary>
    public int Balance { get; init; }

    /// <summary>Notes left of the plan's monthly allowance.</summary>
    public int PlanRemaining { get; init; }

    /// <summary>Loose notes bought separately.</summary>
    public int AvulsoBalance { get; init; }

    public int HardLimit { get; init; }
}

/// <summary>The price of a one-off purchase of notes (<c>GET /v1/credits/purchase/preview</c>).</summary>
public sealed record CreditPurchasePreview
{
    public int Units { get; init; }
    public int UnitPriceCents { get; init; }
    public int TotalCents { get; init; }
}

/// <summary>
/// A plan of the credit catalog. <see cref="Slug"/> is an open string: <c>free</c>, <c>self-hosted</c>,
/// <c>solo</c>, <c>equipe</c>, <c>empresa</c> or <c>escala</c> today; read the allowances from the plan.
/// </summary>
public sealed record CreditPlan
{
    public string Id { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int MonthlyCreditAllowance { get; init; }
    public int? DailyLimit { get; init; }
    public int MaxActiveCompanies { get; init; }
    public int CreditUnitPriceCents { get; init; }
    public int ThroughputRps { get; init; }
    public bool IncludesWebhooks { get; init; }
    public bool IncludesXmlDownload { get; init; }
    public bool IncludesPdfDownload { get; init; }
    public bool IncludesApiKeys { get; init; }
    public string SupportTier { get; init; } = string.Empty;
    public bool Active { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

/// <summary>A credit ledger entry. <c>Amount</c> is in notes (not BRL); members ending in <c>Cents</c> are in centavos.</summary>
public sealed record CreditTransaction
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// <c>purchase</c>, <c>subscription_charge</c>, <c>topup</c>, <c>overage</c>, <c>invoice_authorized</c>,
    /// <c>invoice_rejected</c>, <c>expiration</c>, <c>message_sent</c>, <c>refund</c>, <c>adjustment</c> or <c>bonus</c>.
    /// </summary>
    public string Type { get; init; } = string.Empty;
    public int Amount { get; init; }
    public string? Description { get; init; }

    /// <summary>Free-form details of the entry.</summary>
    public JsonElement? Metadata { get; init; }

    /// <summary><c>subscription</c>, <c>avulso</c> or <c>bonus</c>.</summary>
    public string? Source { get; init; }

    public int? AmountBrlCents { get; init; }
    public int? UnitPriceCentsApplied { get; init; }
    public string? PlanIdSnapshot { get; init; }
    public string? ReservedInvoiceId { get; init; }
    public string? PeriodId { get; init; }
    public bool? IsReservation { get; init; }
    public string? RelatedTransactionId { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>
/// An entry of the public price catalog (<c>GET /v1/plans</c>): prices in centavos, monthly cycle only.
/// The public catalog is not part of the 0.3.0 API contract (it needs no key); prefer
/// <c>Credits.PlansAsync</c> and <c>Billing.PlanAsync</c>.
/// </summary>
public sealed record PlanQuote
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int MonthlyAllowance { get; init; }
    public int BasePriceCents { get; init; }
    public int PerCreditOverageCents { get; init; }
    public int MonthlyTotalCents { get; init; }
    public string Currency { get; init; } = string.Empty;
}

/// <summary>The plan recommended for a monthly volume (<c>GET /v1/plans/quote</c>). The wire names are snake_case.</summary>
public sealed record PlanRecommendation
{
    [JsonPropertyName("volume")]
    public int Volume { get; init; }

    [JsonPropertyName("recommended_plan")]
    public string RecommendedPlan { get; init; } = string.Empty;

    [JsonPropertyName("plan_name")]
    public string PlanName { get; init; } = string.Empty;

    [JsonPropertyName("monthly_allowance")]
    public int MonthlyAllowance { get; init; }

    [JsonPropertyName("overage_credits")]
    public int OverageCredits { get; init; }

    [JsonPropertyName("overage_cost_cents")]
    public int OverageCostCents { get; init; }

    [JsonPropertyName("base_price_cents")]
    public int BasePriceCents { get; init; }

    [JsonPropertyName("total_estimated_cents")]
    public int TotalEstimatedCents { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("billing_cycle")]
    public string BillingCycle { get; init; } = string.Empty;
}

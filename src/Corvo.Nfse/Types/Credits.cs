using System.Text.Json.Serialization;

namespace Corvo.Nfse;

/// <summary>The tenant's credit balance and overage posture.</summary>
public sealed record CreditBalance
{
    public string TenantId { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public int OverageUnits { get; init; }
    public int OverageAmountCents { get; init; }
    public decimal? HardLimit { get; init; }
}

/// <summary>A purchasable credit plan.</summary>
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

/// <summary>A credit ledger entry. <c>Amount</c> is in credits (not BRL); members ending in <c>Cents</c> are in centavos.</summary>
public sealed record CreditTransaction
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Description { get; init; }

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

/// <summary>An entry of the public price catalog (<c>GET /v1/plans</c>): prices in centavos, monthly cycle only.</summary>
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

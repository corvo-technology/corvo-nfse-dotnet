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
    public int DailyLimit { get; init; }
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

/// <summary>A credit ledger transaction.</summary>
public sealed record CreditTransaction
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal CreditDelta { get; init; }
    public decimal BalanceAfter { get; init; }
    public string Environment { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

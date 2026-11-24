using System.Text.Json;

namespace Corvo.Nfse;

/// <summary>A payment or sales platform Corvo can connect to.</summary>
public sealed record IntegrationProvider
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public IReadOnlyList<string> SupportedFlows { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IntegrationCredentialField> CredentialFields { get; init; } = Array.Empty<IntegrationCredentialField>();
    public IntegrationWebhookInstructions WebhookInstructions { get; init; } = new();
}

/// <summary>One credential the provider asks for.</summary>
public sealed record IntegrationCredentialField
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool Secret { get; init; }
    public bool? Generate { get; init; }
    public bool? InWebhookUrl { get; init; }
    public bool? RevealOnce { get; init; }
}

/// <summary>How to point the provider's webhook at Corvo, in English and Portuguese.</summary>
public sealed record IntegrationWebhookInstructions
{
    public string En { get; init; } = string.Empty;
    public string Pt { get; init; } = string.Empty;
}

/// <summary>A connection between a company and a provider.</summary>
public sealed record IntegrationConnection
{
    public string Id { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;

    /// <summary><c>test</c> or <c>live</c>.</summary>
    public string Environment { get; init; } = string.Empty;

    /// <summary><c>draft</c>, <c>active</c>, <c>paused</c>, <c>reauth_required</c> or <c>disconnected</c>.</summary>
    public string State { get; init; } = string.Empty;

    public string? ExternalAccountId { get; init; }
    public string? ActiveFrom { get; init; }

    /// <summary>The address to register in the provider.</summary>
    public string WebhookUrl { get; init; } = string.Empty;

    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>A just-created connection with the secrets generated for it (shown once).</summary>
public sealed record IntegrationConnectionCreated
{
    public string Id { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string? ExternalAccountId { get; init; }
    public string? ActiveFrom { get; init; }
    public string WebhookUrl { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, JsonElement> Secrets { get; init; } = new Dictionary<string, JsonElement>();
}

/// <summary>Body of <c>POST /v1/integrations/connections</c>.</summary>
public sealed record IntegrationConnectionCreateParams
{
    public required string Provider { get; init; }
    public required string CompanyId { get; init; }

    /// <summary>The values of the provider's <see cref="IntegrationProvider.CredentialFields"/>.</summary>
    public required IReadOnlyDictionary<string, string> Credentials { get; init; }

    /// <summary><c>test</c> or <c>live</c>; defaults to the key's environment.</summary>
    public string? Environment { get; init; }

    /// <summary>Only events from this moment on are invoiced.</summary>
    public string? ActiveFrom { get; init; }
}

/// <summary>Which products of the provider issue which service, and with which options.</summary>
public sealed record IntegrationRule
{
    public string Id { get; init; } = string.Empty;
    public string? ExternalProductId { get; init; }
    public JsonElement Service { get; init; }
    public bool SendEmail { get; init; }
    public bool AllowAnonymousCustomer { get; init; }
    public bool CancelOnRefund { get; init; }
}

/// <summary>One rule of <c>IntegrationConnections.ReplaceRulesAsync</c>.</summary>
public sealed record IntegrationRuleInput
{
    public required bool SendEmail { get; init; }
    public required InvoiceServiceInput Service { get; init; }

    /// <summary>Null or omitted: the rule applies to every product.</summary>
    public string? ExternalProductId { get; init; }

    public bool? AllowAnonymousCustomer { get; init; }
    public bool? CancelOnRefund { get; init; }
}

/// <summary>A webhook event received from a provider.</summary>
public sealed record IntegrationEvent
{
    public string Id { get; init; } = string.Empty;
    public string DeliveryId { get; init; } = string.Empty;
    public string ExternalType { get; init; } = string.Empty;

    /// <summary><c>received</c>, <c>processing</c>, <c>retry_pending</c>, <c>processed</c>, <c>ignored</c>, <c>needs_attention</c> or <c>dead</c>.</summary>
    public string State { get; init; } = string.Empty;

    public int Attempts { get; init; }
    public string? LastError { get; init; }
    public string? OperationId { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>The customer of an integration operation (the tax id may be masked).</summary>
public sealed record IntegrationOperationCustomer
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? TaxId { get; init; }
    public bool TaxIdMasked { get; init; }
}

/// <summary>A sale received from a provider and its fiscal outcome.</summary>
public sealed record IntegrationOperation
{
    public string Id { get; init; } = string.Empty;
    public string OperationKey { get; init; } = string.Empty;

    /// <summary><c>pending</c>, <c>paid</c>, <c>refunded</c>, <c>disputed</c> or <c>canceled</c>.</summary>
    public string CommercialState { get; init; } = string.Empty;

    /// <summary><c>none</c>, <c>blocked</c>, <c>needs_attention</c> or <c>submitted</c>.</summary>
    public string FiscalStatus { get; init; } = string.Empty;

    public string? BlockReason { get; init; }
    public int AmountCents { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IntegrationOperationCustomer? Customer { get; init; }
    public string? InvoiceId { get; init; }
    public string? PaidAt { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

/// <summary>Body of <c>PATCH /v1/integrations/operations/{id}/customer</c>.</summary>
public sealed record IntegrationCustomerParams
{
    public required string TaxId { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
}

/// <summary>Result of retrying an operation.</summary>
public sealed record IntegrationOperationRetry
{
    /// <summary><c>processed</c>, <c>ignored</c> or <c>needs_attention</c>.</summary>
    public string State { get; init; } = string.Empty;

    public string? OperationId { get; init; }
    public string? LastError { get; init; }
}

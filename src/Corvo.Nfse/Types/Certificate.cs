namespace Corvo.Nfse;

/// <summary>A digital certificate (A1 .p12) attached to a company.</summary>
public sealed record Certificate
{
    public string Id { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Fingerprint { get; init; } = string.Empty;
    public string ValidFrom { get; init; } = string.Empty;
    public string ValidUntil { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
}

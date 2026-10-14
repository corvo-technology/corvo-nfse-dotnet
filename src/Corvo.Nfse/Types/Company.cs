namespace Corvo.Nfse;

/// <summary>Allowed tax regimes (pass-through string).</summary>
public static class TaxRegimes
{
    public const string Mei = "MEI";
    public const string SimplesNacional = "Simples Nacional";
    public const string LucroPresumido = "Lucro Presumido";
    public const string LucroReal = "Lucro Real";
}

/// <summary>Company address payload used for create/update.</summary>
public sealed record CompanyAddressInput
{
    public required string Street { get; init; }
    public string? Number { get; init; }
    public string? Complement { get; init; }
    public required string District { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string Zip { get; init; }
}

/// <summary>Body for <c>POST /v1/companies</c>.</summary>
public sealed record CompanyCreateParams
{
    public required string LegalName { get; init; }
    public required string TaxId { get; init; }
    public required string TaxRegime { get; init; }
    public string? TradeName { get; init; }
    public string? MunicipalReg { get; init; }
    public string? MunicipalityCode { get; init; }
    public bool? SimpleNational { get; init; }
    public string? SpecialRegime { get; init; }
    public CompanyAddressInput? Address { get; init; }
}

/// <summary>A fiscal company (emitente).</summary>
public sealed record Company
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string LegalName { get; init; } = string.Empty;
    public string? TradeName { get; init; }
    public string TaxId { get; init; } = string.Empty;
    public string? MunicipalReg { get; init; }
    public string? MunicipalityCode { get; init; }
    public string TaxRegime { get; init; } = string.Empty;
    public bool SimpleNational { get; init; }
    public string? SpecialRegime { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressNumber { get; init; }
    public string? AddressComplement { get; init; }
    public string? AddressDistrict { get; init; }
    public string AddressCity { get; init; } = string.Empty;
    public string AddressState { get; init; } = string.Empty;
    public string AddressZip { get; init; } = string.Empty;
    public string FiscalSchemaVersion { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public string? DeletedAt { get; init; }
    public IReadOnlyList<string> SetupGaps { get; init; } = Array.Empty<string>();
}

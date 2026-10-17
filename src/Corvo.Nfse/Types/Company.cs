namespace Corvo.Nfse;

/// <summary>Allowed tax regimes (pass-through string).</summary>
public static class TaxRegimes
{
    public const string Mei = "MEI";
    public const string SimplesNacional = "Simples Nacional";
    public const string LucroPresumido = "Lucro Presumido";
    public const string LucroReal = "Lucro Real";
}

/// <summary>Company address. Every field is optional; <c>State</c> is the two-letter UF and <c>Zip</c> has 8 digits.</summary>
public sealed record CompanyAddressInput
{
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Complement { get; init; }
    public string? District { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Zip { get; init; }
}

/// <summary>The body of <c>POST /v1/companies</c>.</summary>
public sealed record CompanyCreateParams
{
    public required string LegalName { get; init; }

    /// <summary>CNPJ.</summary>
    public required string TaxId { get; init; }

    public required string TaxRegime { get; init; }

    /// <summary>IBGE code of the company's city (7 digits).</summary>
    public required string MunicipalityCode { get; init; }

    public string? TradeName { get; init; }

    /// <summary>Stored, but only sent on the invoice when <see cref="SendMunicipalReg"/> is true.</summary>
    public string? MunicipalReg { get; init; }

    /// <summary>Default false: turn it on only if the city registered the company in the national registry.</summary>
    public bool? SendMunicipalReg { get; init; }

    /// <summary>National service code (6 digits) used when an invoice omits <c>Service.Code</c>.</summary>
    public string? DefaultServiceCode { get; init; }

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
    public bool? SendMunicipalReg { get; init; }
    public string? MunicipalityCode { get; init; }
    public string? DefaultServiceCode { get; init; }
    public string? TaxRegime { get; init; }
    public bool SimpleNational { get; init; }
    public string? SpecialRegime { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressNumber { get; init; }
    public string? AddressComplement { get; init; }
    public string? AddressDistrict { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressState { get; init; }
    public string? AddressZip { get; init; }
    public string FiscalSchemaVersion { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public string? DeletedAt { get; init; }
    /// <summary>What is still pending: <c>municipality_code</c>, <c>certificate</c>.</summary>
    public IReadOnlyList<string> SetupGaps { get; init; } = Array.Empty<string>();
}

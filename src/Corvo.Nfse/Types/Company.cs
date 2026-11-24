namespace Corvo.Nfse;

/// <summary>
/// Allowed tax regimes (pass-through string). Corvo serves only MEI and Simples Nacional: any other
/// value is rejected with <c>422 ERROR_TAX_REGIME_NOT_SUPPORTED</c>.
/// </summary>
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

    /// <summary>
    /// Required for <c>Simples Nacional</c>, ignored for <c>MEI</c>: the effective total rate (%) of
    /// your DAS, as in the PGDAS-D, e.g. <c>"6.54"</c> (0 to 100, at most 2 decimals).
    /// </summary>
    public string? SimplesRate { get; init; }

    /// <summary>
    /// <c>Simples Nacional</c> only: the effective ISS rate (%) of your bracket (1.80 to 5.00), needed
    /// when a customer withholds ISS.
    /// </summary>
    public string? SimplesIssRate { get; init; }

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

    /// <summary>Effective total rate (%) of the DAS, e.g. <c>"6.54"</c>.</summary>
    public string? SimplesRate { get; init; }

    /// <summary>Effective ISS rate (%) of the Simples bracket.</summary>
    public string? SimplesIssRate { get; init; }

    /// <summary>What the Receita says about the municipality's adhesion to the national system.</summary>
    public MunicipalityAgreement? MunicipalityAgreement { get; init; }

    /// <summary>Registry mismatches found for the company (<c>TAX_REGIME_REGISTRY_MISMATCH</c>, <c>MEI_MUNICIPALITY_MISMATCH</c>).</summary>
    public IReadOnlyList<RegistryWarning>? Warnings { get; init; }
}

/// <summary>The municipality's adhesion to the national NFS-e system, as reported by the Receita.</summary>
public sealed record MunicipalityAgreement
{
    /// <summary><c>not_checked</c>, <c>active</c>, <c>inactive</c>, <c>unknown</c> or <c>not_found</c>.</summary>
    public string Status { get; init; } = string.Empty;

    public string? AgreementType { get; init; }
    public string? CheckedAt { get; init; }
}

/// <summary>A registry warning on a company.</summary>
public sealed record RegistryWarning
{
    /// <summary><c>TAX_REGIME_REGISTRY_MISMATCH</c> or <c>MEI_MUNICIPALITY_MISMATCH</c>.</summary>
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

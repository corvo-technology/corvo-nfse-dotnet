namespace Corvo.Nfse;

/// <summary>The accountant who receives the monthly package (<c>GET .../accountant-package</c>).</summary>
public sealed record AccountantSettings
{
    public string? Email { get; init; }

    /// <summary>The last month sent, <c>YYYY-MM</c>.</summary>
    public string? LastSentMonth { get; init; }
}

/// <summary>Result of <c>AccountantPackage.UpdateAsync</c>.</summary>
public sealed record AccountantSettingsUpdate
{
    public string? Email { get; init; }
    public string? LastSentMonth { get; init; }

    /// <summary>True when a test e-mail was sent to the new address.</summary>
    public bool TestEmailSent { get; init; }
}

/// <summary>Result of <c>AccountantPackage.SendAsync</c>.</summary>
public sealed record AccountantSendResult
{
    /// <summary>
    /// <c>sent</c>, <c>skipped-test</c>, <c>skipped-no-email</c>, <c>skipped-empty</c>,
    /// <c>skipped-too-large</c> or <c>skipped-already-sent</c>.
    /// </summary>
    public string Status { get; init; } = string.Empty;
}

/// <summary>One attempt to send the package to the accountant.</summary>
public sealed record AccountantSend
{
    public string Id { get; init; } = string.Empty;
    public string Month { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;

    /// <summary><c>sent</c>, <c>skipped-empty</c>, <c>skipped-too-large</c> or <c>failed</c>.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary><c>scheduled</c>, <c>manual</c> or <c>mcp-or-api</c>.</summary>
    public string Trigger { get; init; } = string.Empty;

    public int InvoiceCount { get; init; }
    public int TotalCents { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>Count and total of a month package (<c>GET .../invoice-package/summary</c>).</summary>
public sealed record PackageSummary
{
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyTaxId { get; init; } = string.Empty;
    public string Month { get; init; } = string.Empty;
    public int Count { get; init; }

    /// <summary>The most notes a package can hold.</summary>
    public int Limit { get; init; }

    /// <summary>Sum of the gross amount of the authorized notes, in BRL, as a decimal string with two places (<c>"1234.50"</c>).</summary>
    public string Total { get; init; } = string.Empty;
}

/// <summary>The DPS numbering of a company (<c>/v1/companies/{id}/dps-sequence</c>).</summary>
public sealed record DpsSequence
{
    public long LastNumber { get; init; }
    public string Series { get; init; } = string.Empty;
}

/// <summary>Error codes of the company routes that have no <c>ERROR_</c> prefix.</summary>
public static class CompanyErrorCodes
{
    /// <summary><c>DpsSequence.SetAsync</c> with a number below the current one.</summary>
    public const string DpsNumberBelowCurrent = "DPS_NUMBER_BELOW_CURRENT";
}

/// <summary>A fiscal profile validity period of a company.</summary>
public sealed record FiscalProfile
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;

    /// <summary><c>MEI</c>, <c>SIMPLES_NACIONAL</c>, <c>LUCRO_PRESUMIDO</c>, <c>LUCRO_REAL</c>, <c>OUTRO_REGIME</c> or <c>UNKNOWN</c>.</summary>
    public string TaxRegime { get; init; } = string.Empty;

    public bool SimpleNational { get; init; }
    public string? SimplesApuracao { get; init; }
    public string? SimplesIssRate { get; init; }
    public string? SpecialTaxRegime { get; init; }
    public bool IbsCbsRegularOption { get; init; }
    public string? IbsCbsRegularFrom { get; init; }
    public string ValidFrom { get; init; } = string.Empty;
    public string? ValidUntil { get; init; }
    public string Source { get; init; } = string.Empty;

    /// <summary><c>unreviewed</c>, <c>approved</c> or <c>rejected</c>.</summary>
    public string ReviewStatus { get; init; } = string.Empty;

    public string? ReviewedAt { get; init; }
    public string? ReviewedBy { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>Body of <c>PUT /v1/companies/{id}/fiscal-profile</c>.</summary>
public sealed record FiscalProfileUpsertParams
{
    public required string TaxRegime { get; init; }
    public bool? SimpleNational { get; init; }

    /// <summary>Only <c>SN</c> is accepted.</summary>
    public string? SimplesApuracao { get; init; }

    public string? SpecialTaxRegime { get; init; }
    public bool? IbsCbsRegularOption { get; init; }
    public string? IbsCbsRegularFrom { get; init; }
    public string? ValidFrom { get; init; }
    public bool? Confirm { get; init; }
}

/// <summary>The withholding rates (%) of a service tax profile.</summary>
public sealed record WithholdingRates
{
    public string? Cofins { get; init; }
    public string? Csll { get; init; }
    public string? Inss { get; init; }
    public string? Irrf { get; init; }
    public string? Pis { get; init; }
}

/// <summary>A service tax classification of a company's catalog.</summary>
public sealed record ServiceTaxProfile
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ServiceCode { get; init; } = string.Empty;
    public string? MunicipalServiceCode { get; init; }
    public string? NbsCode { get; init; }
    public string? ClassCode { get; init; }
    public string? Cst { get; init; }
    public string? OperationIndicator { get; init; }

    /// <summary><c>SERVICE</c>, <c>GOODS</c>, <c>MIXED</c> or <c>PARTNER_SALON</c>.</summary>
    public string OperationNature { get; init; } = string.Empty;

    /// <summary><c>NORMAL</c>, <c>FIXED</c> or <c>EXEMPT</c>.</summary>
    public string? IssTaxation { get; init; }

    public string? IssRate { get; init; }
    public bool IssWithholdingDefault { get; init; }
    public string? IncidenceMunicipalityCode { get; init; }
    public IReadOnlyList<string> RegimeApplicability { get; init; } = Array.Empty<string>();

    /// <summary><c>UNKNOWN</c>, <c>NONE_CONFIRMED</c> or <c>CONFIGURED</c>.</summary>
    public string WithholdingPolicy { get; init; } = string.Empty;

    public WithholdingRates WithholdingRates { get; init; } = new();
    public string ClassificationSource { get; init; } = string.Empty;
    public string? CatalogVersion { get; init; }
    public string EffectiveFrom { get; init; } = string.Empty;
    public string? EffectiveUntil { get; init; }

    /// <summary><c>unreviewed</c>, <c>approved</c> or <c>rejected</c>.</summary>
    public string ReviewStatus { get; init; } = string.Empty;

    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>Body of <c>POST /v1/companies/{id}/service-tax-profiles</c>.</summary>
public sealed record ServiceTaxProfileCreateParams
{
    public required string Name { get; init; }
    public required string ServiceCode { get; init; }

    /// <summary><c>YYYY-MM-DD</c>.</summary>
    public required string EffectiveFrom { get; init; }

    public string? EffectiveUntil { get; init; }
    public string? MunicipalServiceCode { get; init; }
    public string? NbsCode { get; init; }
    public string? ClassCode { get; init; }
    public string? Cst { get; init; }
    public string? OperationIndicator { get; init; }
    public string? OperationNature { get; init; }
    public string? IssTaxation { get; init; }
    public string? IssRate { get; init; }
    public bool? IssWithholdingDefault { get; init; }
    public string? IncidenceMunicipalityCode { get; init; }
    public IReadOnlyList<string>? RegimeApplicability { get; init; }
    public string? WithholdingPolicy { get; init; }
    public WithholdingRates? WithholdingRates { get; init; }
    public string? ClassificationSource { get; init; }
    public string? CatalogVersion { get; init; }
}

/// <summary>
/// Body of <c>PATCH /v1/companies/{id}/service-tax-profiles/{serviceId}</c>. Unset fields are kept;
/// assigning <c>null</c> to an <see cref="Optional{T}"/> field clears it on the server.
/// </summary>
public sealed record ServiceTaxProfileUpdateParams
{
    public string? Name { get; init; }
    public string? ServiceCode { get; init; }
    public string? EffectiveFrom { get; init; }
    public Optional<string> EffectiveUntil { get; init; }
    public Optional<string> MunicipalServiceCode { get; init; }
    public Optional<string> NbsCode { get; init; }
    public Optional<string> ClassCode { get; init; }
    public Optional<string> Cst { get; init; }
    public Optional<string> OperationIndicator { get; init; }
    public string? OperationNature { get; init; }
    public Optional<string> IssTaxation { get; init; }
    public Optional<string> IssRate { get; init; }
    public bool? IssWithholdingDefault { get; init; }
    public Optional<string> IncidenceMunicipalityCode { get; init; }
    public IReadOnlyList<string>? RegimeApplicability { get; init; }
    public string? WithholdingPolicy { get; init; }
    public WithholdingRates? WithholdingRates { get; init; }
    public string? ClassificationSource { get; init; }
    public Optional<string> CatalogVersion { get; init; }

    internal System.Text.Json.Nodes.JsonObject ToJson()
    {
        var body = new System.Text.Json.Nodes.JsonObject();

        void Plain(string name, object? value)
        {
            if (value is not null)
            {
                body[name] = System.Text.Json.JsonSerializer.SerializeToNode(value, CorvoJson.Options);
            }
        }

        void Tri(string name, Optional<string> value)
        {
            if (value.IsSet)
            {
                body[name] = value.Value;
            }
        }

        Plain("name", Name);
        Plain("serviceCode", ServiceCode);
        Plain("effectiveFrom", EffectiveFrom);
        Tri("effectiveUntil", EffectiveUntil);
        Tri("municipalServiceCode", MunicipalServiceCode);
        Tri("nbsCode", NbsCode);
        Tri("classCode", ClassCode);
        Tri("cst", Cst);
        Tri("operationIndicator", OperationIndicator);
        Plain("operationNature", OperationNature);
        Tri("issTaxation", IssTaxation);
        Tri("issRate", IssRate);
        Plain("issWithholdingDefault", IssWithholdingDefault);
        Tri("incidenceMunicipalityCode", IncidenceMunicipalityCode);
        Plain("regimeApplicability", RegimeApplicability);
        Plain("withholdingPolicy", WithholdingPolicy);
        Plain("withholdingRates", WithholdingRates);
        Plain("classificationSource", ClassificationSource);
        Tri("catalogVersion", CatalogVersion);
        return body;
    }
}

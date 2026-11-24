namespace Corvo.Nfse;

/// <summary>A customer saved from earlier invoices (<c>GET /v1/customers</c>).</summary>
public sealed record SavedCustomer
{
    public string TaxId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string LastUsedAt { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}

/// <summary>Body of <c>PATCH /v1/customers/{taxId}</c>. Unset fields are kept; an explicit <c>null</c> email clears it.</summary>
public sealed record CustomerUpdateParams
{
    public string? Name { get; init; }
    public Optional<string> Email { get; init; }

    internal System.Text.Json.Nodes.JsonObject ToJson()
    {
        var body = new System.Text.Json.Nodes.JsonObject();
        if (Name is not null)
        {
            body["name"] = Name;
        }

        if (Email.IsSet)
        {
            body["email"] = Email.Value;
        }

        return body;
    }
}

/// <summary>Registration data of a CNPJ (<c>GET /v1/cnpj/{cnpj}</c>).</summary>
public sealed record CnpjProfile
{
    public string LegalName { get; init; } = string.Empty;
    public string TradeName { get; init; } = string.Empty;

    /// <summary>The registration situation reported by the Receita.</summary>
    public string Situation { get; init; } = string.Empty;

    public bool Active { get; init; }
    public bool? Mei { get; init; }
    public bool? Simples { get; init; }
    public string? MunicipalityCode { get; init; }
    public CnpjAddress Address { get; init; } = new();
}

/// <summary>The postal address of a <see cref="CnpjProfile"/>.</summary>
public sealed record CnpjAddress
{
    public string Street { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string? Complement { get; init; }
    public string District { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Zip { get; init; } = string.Empty;
}

/// <summary>A favorite service of a company (<c>/v1/companies/{id}/service-favorites</c>).</summary>
public sealed record ServiceFavorite
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string CompanyId { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string ServiceCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int? AmountCents { get; init; }
    public string? MunicipalityCode { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

/// <summary>Body of <c>POST /v1/companies/{id}/service-favorites</c>.</summary>
public sealed record ServiceFavoriteCreateParams
{
    public required string Label { get; init; }

    /// <summary>National service code (6 digits).</summary>
    public required string ServiceCode { get; init; }

    public required string Description { get; init; }
    public int? AmountCents { get; init; }
    public string? MunicipalityCode { get; init; }
}

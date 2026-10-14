using System.Text.Json;
using System.Text.Json.Nodes;

namespace Corvo.Nfse;

/// <summary>
/// A tri-state value for PATCH-style fields: unset (omitted from the request), a value,
/// or an explicit JSON <c>null</c> (which clears the field on the server).
/// Assigning — including assigning <c>null</c> — marks the field as set; leaving it
/// untouched omits it.
/// </summary>
/// <typeparam name="T">The field value type.</typeparam>
/// <example>
/// <code>
/// new CompanyUpdateParams { TradeName = "ACME", MunicipalReg = null } // clears municipalReg only
/// </code>
/// </example>
public readonly struct Optional<T>
{
    private readonly T? _value;

    private Optional(T? value)
    {
        _value = value;
        IsSet = true;
    }

    /// <summary>Whether the field was explicitly assigned (it may still hold <c>null</c>).</summary>
    public bool IsSet { get; }

    /// <summary>The assigned value; meaningless when <see cref="IsSet"/> is <c>false</c>.</summary>
    public T? Value => _value;

    /// <summary>Marks the field as set with <paramref name="value"/> (which may be <c>null</c> to clear it).</summary>
    public static implicit operator Optional<T>(T? value) => new(value);
}

/// <summary>Body for <c>PATCH /v1/companies/{id}</c>. Unset fields are omitted; explicit <c>null</c> clears.</summary>
public sealed record CompanyUpdateParams
{
    public string? LegalName { get; init; }
    public string? TradeName { get; init; }
    public Optional<string> MunicipalReg { get; init; }
    public Optional<string> MunicipalityCode { get; init; }
    public string? TaxRegime { get; init; }
    public bool? SimpleNational { get; init; }
    public Optional<CompanyAddressInput> Address { get; init; }

    /// <summary>
    /// Builds the wire body: unset fields are omitted, explicit nulls are preserved, and
    /// the exact server-side field names are used (no re-casing). Returns an empty
    /// object when nothing was set.
    /// </summary>
    internal JsonObject ToJson()
    {
        JsonObject? body = null;

        void Put(string name, object? value)
        {
            body ??= new JsonObject();
            body[name] = value switch
            {
                null => null,
                string text => JsonValue.Create(text),
                bool flag => JsonValue.Create(flag),
                _ => JsonSerializer.SerializeToNode(value, CorvoJson.Options),
            };
        }

        if (LegalName is not null)
        {
            Put("legalName", LegalName);
        }

        if (TradeName is not null)
        {
            Put("tradeName", TradeName);
        }

        if (MunicipalReg.IsSet)
        {
            Put("municipalReg", MunicipalReg.Value);
        }

        if (MunicipalityCode.IsSet)
        {
            Put("municipalityCode", MunicipalityCode.Value);
        }

        if (TaxRegime is not null)
        {
            Put("taxRegime", TaxRegime);
        }

        if (SimpleNational is not null)
        {
            Put("simpleNational", SimpleNational);
        }

        if (Address.IsSet)
        {
            Put("address", Address.Value is null
                ? null
                : JsonSerializer.SerializeToNode(Address.Value, CorvoJson.Options));
        }

        return body ?? new JsonObject();
    }
}

using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>Saved customers: search, iterate, get, update, delete.</summary>
public sealed class CustomersResource
{
    private readonly HttpTransport _http;

    internal CustomersResource(HttpTransport http) => _http = http;

    /// <summary>
    /// Lists saved customers, most recently used first (<c>GET /v1/customers</c>). <paramref name="search"/> is part
    /// of the name or the first digits of the CPF/CNPJ (punctuation is ignored). The cursor is opaque.
    /// </summary>
    public Task<Result<Paginated<SavedCustomer>>> SearchAsync(
        string? search = null,
        int? limit = null,
        string? cursor = null,
        CancellationToken ct = default)
        => _http.ListAsync<SavedCustomer>(
            "/customers",
            HttpTransport.Query(("search", search), ("limit", limit), ("cursor", cursor)),
            HttpTransport.MapPlain<SavedCustomer>(),
            ct);

    /// <summary>Walks every page of <see cref="SearchAsync"/>; a page failure is yielded as an error result.</summary>
    public IAsyncEnumerable<Result<SavedCustomer>> IterateAsync(
        string? search = null,
        int? limit = null,
        string? cursor = null,
        CancellationToken ct = default)
        => _http.IterateAsync<SavedCustomer>(
            "/customers",
            HttpTransport.Query(("search", search), ("limit", limit), ("cursor", cursor)),
            HttpTransport.MapPlain<SavedCustomer>(),
            ct);

    /// <summary>Fetches a saved customer by CPF or CNPJ (<c>GET /v1/customers/{taxId}</c>).</summary>
    public Task<Result<SavedCustomer>> GetAsync(string taxId, CancellationToken ct = default)
        => _http.RequestAsync<SavedCustomer>(HttpMethod.Get, $"/customers/{Uri.EscapeDataString(taxId)}", ct: ct);

    /// <summary>
    /// Corrects the name or the email of a saved customer (<c>PATCH /v1/customers/{taxId}</c>). Issued
    /// invoices are not changed. Assign <c>null</c> to <see cref="CustomerUpdateParams.Email"/> to clear it.
    /// </summary>
    public Task<Result<SavedCustomer>> UpdateAsync(string taxId, CustomerUpdateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<SavedCustomer>(
            HttpMethod.Patch,
            $"/customers/{Uri.EscapeDataString(taxId)}",
            body: parameters.ToJson(),
            ct: ct);

    /// <summary>Forgets a saved customer (<c>DELETE /v1/customers/{taxId}</c>); issued invoices are not changed.</summary>
    public Task<Result<bool>> DeleteAsync(string taxId, CancellationToken ct = default)
        => _http.RequestAckAsync(HttpMethod.Delete, $"/customers/{Uri.EscapeDataString(taxId)}", ct: ct);
}

/// <summary>CNPJ registry lookup.</summary>
public sealed class CnpjResource
{
    private readonly HttpTransport _http;

    internal CnpjResource(HttpTransport http) => _http = http;

    /// <summary>
    /// Looks up the registration data of a CNPJ (<c>GET /v1/cnpj/{cnpj}</c>). Errors: <c>CNPJ_INVALID</c> and
    /// <c>CNPJ_LOOKUP_UNAVAILABLE</c>.
    /// </summary>
    public Task<Result<CnpjProfile>> LookupAsync(string cnpj, CancellationToken ct = default)
        => _http.RequestAsync<CnpjProfile>(HttpMethod.Get, $"/cnpj/{Uri.EscapeDataString(cnpj)}", ct: ct);
}

/// <summary>Favorite services of a company: list, create, delete.</summary>
public sealed class ServiceFavoritesResource
{
    private readonly HttpTransport _http;

    internal ServiceFavoritesResource(HttpTransport http) => _http = http;

    /// <summary>Lists the favorite services of a company (<c>GET /v1/companies/{companyId}/service-favorites</c>).</summary>
    public Task<Result<IReadOnlyList<ServiceFavorite>>> ListAsync(string companyId, CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<ServiceFavorite>>(
            HttpMethod.Get,
            $"/companies/{Uri.EscapeDataString(companyId)}/service-favorites",
            ct: ct);

    /// <summary>Saves a favorite service (<c>POST /v1/companies/{companyId}/service-favorites</c>).</summary>
    public Task<Result<ServiceFavorite>> CreateAsync(string companyId, ServiceFavoriteCreateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<ServiceFavorite>(
            HttpMethod.Post,
            $"/companies/{Uri.EscapeDataString(companyId)}/service-favorites",
            body: parameters,
            ct: ct);

    /// <summary>Removes a favorite service (<c>DELETE /v1/companies/{companyId}/service-favorites/{favoriteId}</c>).</summary>
    public Task<Result<bool>> DeleteAsync(string companyId, string favoriteId, CancellationToken ct = default)
        => _http.RequestAckAsync(
            HttpMethod.Delete,
            $"/companies/{Uri.EscapeDataString(companyId)}/service-favorites/{Uri.EscapeDataString(favoriteId)}",
            ct: ct);
}

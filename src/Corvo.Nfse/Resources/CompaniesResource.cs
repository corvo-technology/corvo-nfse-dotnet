using System.Text.Json;
using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>Companies: create, list, iterate, get, update, remove.</summary>
public sealed class CompaniesResource
{
    private readonly HttpTransport _http;

    internal CompaniesResource(HttpTransport http) => _http = http;

    /// <summary>Creates a company (<c>POST /v1/companies</c>).</summary>
    public Task<Result<Company>> CreateAsync(CompanyCreateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<Company>(HttpMethod.Post, "/companies", body: parameters, ct: ct);

    /// <summary>Lists companies (<c>GET /v1/companies</c>).</summary>
    public Task<Result<Paginated<Company>>> ListAsync(int? limit = null, string? cursor = null, CancellationToken ct = default)
        => _http.ListAsync<Company>(
            "/companies",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapPlain<Company>(),
            ct);

    /// <summary>Walks every page of companies; a page failure is yielded as an error result.</summary>
    public IAsyncEnumerable<Result<Company>> IterateAsync(int? limit = null, string? cursor = null, CancellationToken ct = default)
        => _http.IterateAsync<Company>(
            "/companies",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapPlain<Company>(),
            ct);

    /// <summary>Fetches one company (<c>GET /v1/companies/{id}</c>).</summary>
    public Task<Result<Company>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<Company>(HttpMethod.Get, $"/companies/{Uri.EscapeDataString(id)}", ct: ct);

    /// <summary>
    /// Updates a company (<c>PATCH /v1/companies/{id}</c>). Unset fields are omitted;
    /// assign <c>null</c> to <see cref="Optional{T}"/> fields to clear them server-side.
    /// </summary>
    public Task<Result<Company>> UpdateAsync(string id, CompanyUpdateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<Company>(HttpMethod.Patch, $"/companies/{Uri.EscapeDataString(id)}", body: parameters.ToJson(), ct: ct);

    /// <summary>Soft-deletes a company (<c>DELETE /v1/companies/{id}</c>).</summary>
    public async Task<Result<bool>> RemoveAsync(string id, CancellationToken ct = default)
    {
        var result = await _http
            .RequestAsync<JsonElement>(HttpMethod.Delete, $"/companies/{Uri.EscapeDataString(id)}", ct: ct)
            .ConfigureAwait(false);
        return result.Error is null
            ? new Result<bool>(true, null)
            : new Result<bool>(false, result.Error);
    }
}

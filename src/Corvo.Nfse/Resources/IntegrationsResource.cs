using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>Integration providers (payment and sales platforms Corvo can connect to).</summary>
public sealed class IntegrationsResource
{
    private readonly HttpTransport _http;

    internal IntegrationsResource(HttpTransport http) => _http = http;

    /// <summary>Lists the providers and the credentials each one needs (<c>GET /v1/integrations/providers</c>).</summary>
    public Task<Result<IReadOnlyList<IntegrationProvider>>> ListProvidersAsync(CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<IntegrationProvider>>(HttpMethod.Get, "/integrations/providers", ct: ct);
}

/// <summary>
/// Connections between a company and a provider. A connection belongs to the environment of the key that
/// created it, and only a key of the same environment sees it.
/// </summary>
public sealed class IntegrationConnectionsResource
{
    private readonly HttpTransport _http;

    internal IntegrationConnectionsResource(HttpTransport http) => _http = http;

    /// <summary>Lists connections (<c>GET /v1/integrations/connections</c>).</summary>
    public Task<Result<IReadOnlyList<IntegrationConnection>>> ListAsync(CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<IntegrationConnection>>(HttpMethod.Get, "/integrations/connections", ct: ct);

    /// <summary>Creates a connection in <c>draft</c> state; the secrets generated for it come back only here (<c>POST /v1/integrations/connections</c>).</summary>
    public Task<Result<IntegrationConnectionCreated>> CreateAsync(IntegrationConnectionCreateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<IntegrationConnectionCreated>(HttpMethod.Post, "/integrations/connections", body: parameters, ct: ct);

    /// <summary>Fetches a connection (<c>GET /v1/integrations/connections/{id}</c>).</summary>
    public Task<Result<IntegrationConnection>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<IntegrationConnection>(HttpMethod.Get, Path(id), ct: ct);

    /// <summary>Checks the credentials with the provider (<c>POST .../validate</c>).</summary>
    public Task<Result<IntegrationConnection>> ValidateAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<IntegrationConnection>(HttpMethod.Post, Path(id) + "/validate", ct: ct);

    /// <summary>Replaces the credentials (<c>PUT .../credentials</c>).</summary>
    public Task<Result<IntegrationConnection>> UpdateCredentialsAsync(
        string id,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken ct = default)
        => _http.RequestAsync<IntegrationConnection>(
            HttpMethod.Put,
            Path(id) + "/credentials",
            body: new { credentials },
            ct: ct);

    /// <summary>Starts issuing invoices for the provider's sales (<c>POST .../activate</c>).</summary>
    public Task<Result<IntegrationConnection>> ActivateAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<IntegrationConnection>(HttpMethod.Post, Path(id) + "/activate", ct: ct);

    /// <summary>Pauses the connection (<c>POST .../pause</c>).</summary>
    public Task<Result<IntegrationConnection>> PauseAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<IntegrationConnection>(HttpMethod.Post, Path(id) + "/pause", ct: ct);

    /// <summary>Disconnects (<c>POST .../disconnect</c>).</summary>
    public Task<Result<IntegrationConnection>> DisconnectAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<IntegrationConnection>(HttpMethod.Post, Path(id) + "/disconnect", ct: ct);

    /// <summary>Lists the issuing rules (<c>GET .../rules</c>).</summary>
    public Task<Result<IReadOnlyList<IntegrationRule>>> ListRulesAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<IntegrationRule>>(HttpMethod.Get, Path(id) + "/rules", ct: ct);

    /// <summary>Replaces all the issuing rules (<c>PUT .../rules</c>).</summary>
    public Task<Result<IReadOnlyList<IntegrationRule>>> ReplaceRulesAsync(
        string id,
        IEnumerable<IntegrationRuleInput> rules,
        CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<IntegrationRule>>(
            HttpMethod.Put,
            Path(id) + "/rules",
            body: new { rules = rules.ToArray() },
            ct: ct);

    /// <summary>Lists the latest webhook events received (<c>GET .../events</c>); <paramref name="limit"/> is 1 to 100 (50 by default).</summary>
    public Task<Result<IReadOnlyList<IntegrationEvent>>> ListEventsAsync(string id, int? limit = null, CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<IntegrationEvent>>(
            HttpMethod.Get,
            Path(id) + "/events",
            query: HttpTransport.Query(("limit", limit)),
            ct: ct);

    /// <summary>Lists the latest operations (<c>GET .../operations</c>); <paramref name="limit"/> is 1 to 100 (50 by default).</summary>
    public Task<Result<IReadOnlyList<IntegrationOperation>>> ListOperationsAsync(string id, int? limit = null, CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<IntegrationOperation>>(
            HttpMethod.Get,
            Path(id) + "/operations",
            query: HttpTransport.Query(("limit", limit)),
            ct: ct);

    private static string Path(string id) => $"/integrations/connections/{Uri.EscapeDataString(id)}";
}

/// <summary>Operations received from providers: complete the customer, retry.</summary>
public sealed class IntegrationOperationsResource
{
    private readonly HttpTransport _http;

    internal IntegrationOperationsResource(HttpTransport http) => _http = http;

    /// <summary>Completes the customer of a blocked operation (<c>PATCH /v1/integrations/operations/{id}/customer</c>).</summary>
    public Task<Result<IntegrationOperation>> CompleteCustomerAsync(
        string id,
        IntegrationCustomerParams parameters,
        CancellationToken ct = default)
        => _http.RequestAsync<IntegrationOperation>(
            HttpMethod.Patch,
            $"/integrations/operations/{Uri.EscapeDataString(id)}/customer",
            body: parameters,
            ct: ct);

    /// <summary>Retries an operation that needs attention (<c>POST /v1/integrations/operations/{id}/retry</c>).</summary>
    public Task<Result<IntegrationOperationRetry>> RetryAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<IntegrationOperationRetry>(
            HttpMethod.Post,
            $"/integrations/operations/{Uri.EscapeDataString(id)}/retry",
            ct: ct);
}

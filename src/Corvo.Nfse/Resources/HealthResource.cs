using System.Text.Json;
using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>Public health checks. Both return the raw JSON body (no <c>{ data }</c> envelope).</summary>
public sealed class HealthResource
{
    private readonly HttpTransport _http;

    internal HealthResource(HttpTransport http) => _http = http;

    /// <summary>Liveness probe (<c>GET /v1/health/live</c>).</summary>
    public Task<Result<JsonElement>> LiveAsync(CancellationToken ct = default)
        => _http.RequestAsync<JsonElement>(HttpMethod.Get, "/health/live", unwrap: UnwrapMode.None, ct: ct);

    /// <summary>Readiness probe (<c>GET /v1/health/ready</c>).</summary>
    public Task<Result<JsonElement>> ReadyAsync(CancellationToken ct = default)
        => _http.RequestAsync<JsonElement>(HttpMethod.Get, "/health/ready", unwrap: UnwrapMode.None, ct: ct);
}

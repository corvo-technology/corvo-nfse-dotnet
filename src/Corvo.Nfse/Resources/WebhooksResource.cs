using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>Webhooks: list, create (returns the secret once), remove, test, plus <see cref="Deliveries"/>.</summary>
public sealed class WebhooksResource
{
    private readonly HttpTransport _http;

    internal WebhooksResource(HttpTransport http)
    {
        _http = http;
        Deliveries = new DeliveriesResource(http);
    }

    /// <summary>Webhook delivery attempts: list, iterate, stats, retry.</summary>
    public DeliveriesResource Deliveries { get; }

    /// <summary>Lists webhooks (<c>GET /v1/webhooks</c>).</summary>
    public Task<Result<Paginated<Webhook>>> ListAsync(int? limit = null, string? cursor = null, CancellationToken ct = default)
        => _http.ListAsync<Webhook>(
            "/webhooks",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapPlain<Webhook>(),
            ct);

    /// <summary>
    /// Creates a webhook (<c>POST /v1/webhooks</c>). The response is the only time the
    /// signing secret (<see cref="WebhookWithSecret.Secret"/>) is returned.
    /// </summary>
    public Task<Result<WebhookWithSecret>> CreateAsync(WebhookCreateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<WebhookWithSecret>(HttpMethod.Post, "/webhooks", body: parameters, ct: ct);

    /// <summary>Removes a webhook (<c>DELETE /v1/webhooks/{id}</c>).</summary>
    public Task<Result<WebhookDeleteResult>> RemoveAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<WebhookDeleteResult>(HttpMethod.Delete, $"/webhooks/{Uri.EscapeDataString(id)}", ct: ct);

    /// <summary>Fires a <c>webhook.test</c> event (<c>POST /v1/webhooks/{id}/test</c>).</summary>
    public Task<Result<WebhookTestResult>> TestAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<WebhookTestResult>(HttpMethod.Post, $"/webhooks/{Uri.EscapeDataString(id)}/test", ct: ct);
}

/// <summary>Webhook deliveries: list, iterate, stats, retry.</summary>
public sealed class DeliveriesResource
{
    private readonly HttpTransport _http;

    internal DeliveriesResource(HttpTransport http) => _http = http;

    /// <summary>Lists delivery attempts (<c>GET /v1/webhooks/deliveries</c>).</summary>
    public Task<Result<Paginated<WebhookDelivery>>> ListAsync(WebhookDeliveryListParams? parameters = null, CancellationToken ct = default)
        => _http.ListAsync<WebhookDelivery>("/webhooks/deliveries", Query(parameters), HttpTransport.MapPlain<WebhookDelivery>(), ct);

    /// <summary>Walks every page of deliveries; a page failure is yielded as an error result.</summary>
    public IAsyncEnumerable<Result<WebhookDelivery>> IterateAsync(WebhookDeliveryListParams? parameters = null, CancellationToken ct = default)
        => _http.IterateAsync<WebhookDelivery>("/webhooks/deliveries", Query(parameters), HttpTransport.MapPlain<WebhookDelivery>(), ct);

    /// <summary>Delivery counters for a trailing window (<c>GET /v1/webhooks/deliveries/stats</c>).</summary>
    public Task<Result<WebhookDeliveryStats>> StatsAsync(
        int? hours = null,
        string? environment = null,
        CancellationToken ct = default)
        => _http.RequestAsync<WebhookDeliveryStats>(
            HttpMethod.Get,
            "/webhooks/deliveries/stats",
            query: HttpTransport.Query(("hours", hours), ("environment", environment)),
            ct: ct);

    /// <summary>Retries a failed delivery (<c>POST /v1/webhooks/deliveries/{id}/retry</c>).</summary>
    public Task<Result<WebhookRetryResult>> RetryAsync(string deliveryId, CancellationToken ct = default)
        => _http.RequestAsync<WebhookRetryResult>(
            HttpMethod.Post,
            $"/webhooks/deliveries/{Uri.EscapeDataString(deliveryId)}/retry",
            ct: ct);

    private static Dictionary<string, string?> Query(WebhookDeliveryListParams? parameters)
    {
        parameters ??= new WebhookDeliveryListParams();
        return HttpTransport.Query(
            ("hours", parameters.Hours),
            ("environment", parameters.Environment),
            ("limit", parameters.Limit),
            ("cursor", parameters.Cursor));
    }
}

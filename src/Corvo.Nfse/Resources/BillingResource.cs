using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>Billing: plan, usage.</summary>
public sealed class BillingResource
{
    private readonly HttpTransport _http;

    internal BillingResource(HttpTransport http) => _http = http;

    /// <summary>
    /// Current plan + subscription + usage (<c>GET /v1/billing/plan</c>). A workspace without a plan answers <c>404</c> with
    /// <see cref="BillingErrorCodes.NoPlanAssigned"/>.
    /// </summary>
    public Task<Result<BillingPlanResponse>> PlanAsync(CancellationToken ct = default)
        => _http.RequestAsync<BillingPlanResponse>(HttpMethod.Get, "/billing/plan", ct: ct);

    /// <summary>Quota status for the current period (<c>GET /v1/billing/usage</c>).</summary>
    public Task<Result<QuotaStatus>> UsageAsync(CancellationToken ct = default)
        => _http.RequestAsync<QuotaStatus>(HttpMethod.Get, "/billing/usage", ct: ct);
}

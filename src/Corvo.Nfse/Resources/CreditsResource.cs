using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>Credits: balance, plans, history, iterateHistory.</summary>
public sealed class CreditsResource
{
    private readonly HttpTransport _http;

    internal CreditsResource(HttpTransport http) => _http = http;

    /// <summary>Current credit balance (<c>GET /v1/credits/balance</c>).</summary>
    public Task<Result<CreditBalance>> BalanceAsync(CancellationToken ct = default)
        => _http.RequestAsync<CreditBalance>(HttpMethod.Get, "/credits/balance", ct: ct);

    /// <summary>Credit plans, cursor-paginated (<c>GET /v1/credits/plans</c>).</summary>
    public Task<Result<Paginated<CreditPlan>>> PlansAsync(int? limit = null, string? cursor = null, CancellationToken ct = default)
        => _http.ListAsync<CreditPlan>(
            "/credits/plans",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapProps<CreditPlan>(),
            ct);

    /// <summary>Credit ledger, cursor-paginated (<c>GET /v1/credits/history</c>).</summary>
    public Task<Result<Paginated<CreditTransaction>>> HistoryAsync(int? limit = null, string? cursor = null, CancellationToken ct = default)
        => _http.ListAsync<CreditTransaction>(
            "/credits/history",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapProps<CreditTransaction>(),
            ct);

    /// <summary>Walks every page of the credit ledger; a page failure is yielded as an error result.</summary>
    public IAsyncEnumerable<Result<CreditTransaction>> IterateHistoryAsync(int? limit = null, string? cursor = null, CancellationToken ct = default)
        => _http.IterateAsync<CreditTransaction>(
            "/credits/history",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapProps<CreditTransaction>(),
            ct);
}

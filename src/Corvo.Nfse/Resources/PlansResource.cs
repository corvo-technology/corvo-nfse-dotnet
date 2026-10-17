using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>The public price catalog (monthly cycle only): list, get, quote.</summary>
public sealed class PlansResource
{
    private readonly HttpTransport _http;

    internal PlansResource(HttpTransport http) => _http = http;

    /// <summary>Plan prices, from the smallest to the largest allowance (<c>GET /v1/plans</c>).</summary>
    public Task<Result<Paginated<PlanQuote>>> ListAsync(int? limit = null, string? cursor = null, CancellationToken ct = default)
        => _http.ListAsync<PlanQuote>(
            "/plans",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapPlain<PlanQuote>(),
            ct);

    /// <summary>The price of one plan: free, developer, growth, scale or volume (<c>GET /v1/plans/{slug}</c>).</summary>
    public Task<Result<PlanQuote>> GetAsync(string slug, CancellationToken ct = default)
        => _http.RequestAsync<PlanQuote>(HttpMethod.Get, $"/plans/{Uri.EscapeDataString(slug)}", ct: ct);

    /// <summary>
    /// The plan recommended for <paramref name="volume"/> invoices per month (<c>GET /v1/plans/quote</c>).
    /// <c>Data</c> is <c>null</c> (with no error) when the volume is missing or not positive.
    /// </summary>
    public Task<Result<PlanRecommendation>> QuoteAsync(int volume, CancellationToken ct = default)
        => _http.RequestAsync<PlanRecommendation>(
            HttpMethod.Get,
            "/plans/quote",
            query: HttpTransport.Query(("volume", volume)),
            ct: ct);
}

using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>
/// The public price catalog: list, get (both in the 0.3.0 contract, no API key needed) and quote. The plan data a key
/// can read is in <see cref="CreditsResource.PlansAsync"/> and
/// <see cref="BillingResource.PlanAsync"/>.
/// </summary>
public sealed class PlansResource
{
    private readonly HttpTransport _http;

    internal PlansResource(HttpTransport http) => _http = http;

    /// <summary>
    /// Plan prices, from the smallest to the largest allowance (<c>GET /v1/plans</c>). <paramref name="cycle"/> is
    /// <c>monthly</c> (default) or <c>annual</c> (discounted).
    /// </summary>
    public Task<Result<Paginated<PlanPrice>>> ListAsync(int? limit = null, string? cursor = null, string? cycle = null, CancellationToken ct = default)
        => _http.ListAsync<PlanPrice>(
            "/plans",
            HttpTransport.Query(("limit", limit), ("cursor", cursor), ("cycle", cycle)),
            HttpTransport.MapPlain<PlanPrice>(),
            ct);

    /// <summary>The price of one plan: for example free, solo, equipe, empresa or escala (<c>GET /v1/plans/{slug}</c>).</summary>
    public Task<Result<PlanPrice>> GetAsync(string slug, string? cycle = null, CancellationToken ct = default)
        => _http.RequestAsync<PlanPrice>(
            HttpMethod.Get,
            $"/plans/{Uri.EscapeDataString(slug)}",
            query: HttpTransport.Query(("cycle", cycle)),
            ct: ct);

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

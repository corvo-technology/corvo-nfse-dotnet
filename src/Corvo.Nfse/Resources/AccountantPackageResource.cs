using System.Text.Json.Nodes;
using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>The monthly package for the accountant: settings, send now, history.</summary>
public sealed class AccountantPackageResource
{
    private readonly HttpTransport _http;

    internal AccountantPackageResource(HttpTransport http) => _http = http;

    /// <summary>The accountant e-mail of the monthly package (<c>GET /v1/companies/{companyId}/accountant-package</c>).</summary>
    public Task<Result<AccountantSettings>> GetAsync(string companyId, CancellationToken ct = default)
        => _http.RequestAsync<AccountantSettings>(HttpMethod.Get, Path(companyId), ct: ct);

    /// <summary>
    /// Sets the accountant e-mail (<c>PUT /v1/companies/{companyId}/accountant-package</c>); <c>null</c> turns the
    /// monthly send off. A test e-mail is sent to a new address (<see cref="AccountantSettingsUpdate.TestEmailSent"/>).
    /// </summary>
    public Task<Result<AccountantSettingsUpdate>> UpdateAsync(string companyId, string? accountantEmail, CancellationToken ct = default)
        => _http.RequestAsync<AccountantSettingsUpdate>(
            HttpMethod.Put,
            Path(companyId),
            body: new JsonObject { ["accountantEmail"] = accountantEmail },
            ct: ct);

    /// <summary>Sends the package of a month (<c>YYYY-MM</c>) to the accountant now (<c>POST .../accountant-package/send</c>).</summary>
    public Task<Result<AccountantSendResult>> SendAsync(string companyId, string month, CancellationToken ct = default)
        => _http.RequestAsync<AccountantSendResult>(
            HttpMethod.Post,
            Path(companyId) + "/send",
            body: new { month },
            ct: ct);

    /// <summary>The last 12 attempts to send the package (<c>GET .../accountant-package/sends</c>).</summary>
    public Task<Result<IReadOnlyList<AccountantSend>>> ListSendsAsync(string companyId, CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<AccountantSend>>(HttpMethod.Get, Path(companyId) + "/sends", ct: ct);

    private static string Path(string companyId) => $"/companies/{Uri.EscapeDataString(companyId)}/accountant-package";
}

/// <summary>The monthly ZIP of a company's invoices: summary and download.</summary>
public sealed class InvoicePackageResource
{
    private readonly HttpTransport _http;

    internal InvoicePackageResource(HttpTransport http) => _http = http;

    /// <summary>Count and total of a month's package, without building it (<c>GET .../invoice-package/summary</c>).</summary>
    public Task<Result<PackageSummary>> SummaryAsync(string companyId, string month, CancellationToken ct = default)
        => _http.RequestAsync<PackageSummary>(
            HttpMethod.Get,
            $"/companies/{Uri.EscapeDataString(companyId)}/invoice-package/summary",
            query: HttpTransport.Query(("month", month)),
            ct: ct);

    /// <summary>
    /// Downloads the package of a month (<c>YYYY-MM</c>) as a ZIP with <c>xml/</c>, <c>pdf/</c> and <c>resumo.csv</c>
    /// (<c>GET .../invoice-package</c>). <paramref name="include"/> is a comma-separated list of parts
    /// (<c>xml</c>, <c>pdf</c>); both by default. A package over the limit answers <c>PACKAGE_TOO_LARGE</c>.
    /// </summary>
    public Task<Result<DownloadedFile>> DownloadAsync(
        string companyId,
        string month,
        string? include = null,
        CancellationToken ct = default)
        => _http.RequestRawAsync(
            HttpMethod.Get,
            $"/companies/{Uri.EscapeDataString(companyId)}/invoice-package",
            query: HttpTransport.Query(("month", month), ("include", include)),
            ct: ct);
}

/// <summary>The DPS numbering of a company: get, set.</summary>
public sealed class DpsSequenceResource
{
    private readonly HttpTransport _http;

    internal DpsSequenceResource(HttpTransport http) => _http = http;

    /// <summary>The DPS series and last number (<c>GET /v1/companies/{id}/dps-sequence</c>).</summary>
    public Task<Result<DpsSequence>> GetAsync(string companyId, CancellationToken ct = default)
        => _http.RequestAsync<DpsSequence>(HttpMethod.Get, Path(companyId), ct: ct);

    /// <summary>
    /// Continues the numbering from the last note issued by another system (<c>PUT /v1/companies/{id}/dps-sequence</c>).
    /// A number below the current one fails with <see cref="CompanyErrorCodes.DpsNumberBelowCurrent"/>.
    /// </summary>
    public Task<Result<DpsSequence>> SetAsync(string companyId, long lastNumber, CancellationToken ct = default)
        => _http.RequestAsync<DpsSequence>(
            HttpMethod.Put,
            Path(companyId),
            body: new JsonObject { ["lastNumber"] = lastNumber },
            ct: ct);

    private static string Path(string companyId) => $"/companies/{Uri.EscapeDataString(companyId)}/dps-sequence";
}

/// <summary>The fiscal profile history of a company: list, upsert, approve.</summary>
public sealed class FiscalProfilesResource
{
    private readonly HttpTransport _http;

    internal FiscalProfilesResource(HttpTransport http) => _http = http;

    /// <summary>Lists the fiscal profile history (<c>GET /v1/companies/{id}/fiscal-profile</c>).</summary>
    public Task<Result<IReadOnlyList<FiscalProfile>>> ListAsync(string companyId, CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<FiscalProfile>>(HttpMethod.Get, Path(companyId), ct: ct);

    /// <summary>Creates a validity period (<c>PUT /v1/companies/{id}/fiscal-profile</c>); idempotent for an identical state.</summary>
    public Task<Result<FiscalProfile>> UpsertAsync(string companyId, FiscalProfileUpsertParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<FiscalProfile>(HttpMethod.Put, Path(companyId), body: parameters, ct: ct);

    /// <summary>Approves a profile (<c>POST /v1/companies/{id}/fiscal-profile/{profileId}/approve</c>).</summary>
    public Task<Result<FiscalProfile>> ApproveAsync(string companyId, string profileId, CancellationToken ct = default)
        => _http.RequestAsync<FiscalProfile>(
            HttpMethod.Post,
            $"{Path(companyId)}/{Uri.EscapeDataString(profileId)}/approve",
            ct: ct);

    private static string Path(string companyId) => $"/companies/{Uri.EscapeDataString(companyId)}/fiscal-profile";
}

/// <summary>The service tax classifications of a company: list, create, update, delete, approve.</summary>
public sealed class ServiceTaxProfilesResource
{
    private readonly HttpTransport _http;

    internal ServiceTaxProfilesResource(HttpTransport http) => _http = http;

    /// <summary>Lists the profiles effective on <paramref name="date"/> (<c>YYYY-MM-DD</c>, default today) (<c>GET /v1/companies/{id}/service-tax-profiles</c>).</summary>
    public Task<Result<IReadOnlyList<ServiceTaxProfile>>> ListAsync(string companyId, string? date = null, CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<ServiceTaxProfile>>(
            HttpMethod.Get,
            Path(companyId),
            query: HttpTransport.Query(("date", date)),
            ct: ct);

    /// <summary>Creates a profile (<c>POST /v1/companies/{id}/service-tax-profiles</c>).</summary>
    public Task<Result<ServiceTaxProfile>> CreateAsync(string companyId, ServiceTaxProfileCreateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<ServiceTaxProfile>(HttpMethod.Post, Path(companyId), body: parameters, ct: ct);

    /// <summary>Updates a profile (<c>PATCH /v1/companies/{id}/service-tax-profiles/{serviceId}</c>).</summary>
    public Task<Result<ServiceTaxProfile>> UpdateAsync(string companyId, string serviceId, ServiceTaxProfileUpdateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<ServiceTaxProfile>(
            HttpMethod.Patch,
            $"{Path(companyId)}/{Uri.EscapeDataString(serviceId)}",
            body: parameters.ToJson(),
            ct: ct);

    /// <summary>Deletes an expired, not yet approved profile (<c>DELETE /v1/companies/{id}/service-tax-profiles/{serviceId}</c>).</summary>
    public Task<Result<bool>> DeleteAsync(string companyId, string serviceId, CancellationToken ct = default)
        => _http.RequestAckAsync(HttpMethod.Delete, $"{Path(companyId)}/{Uri.EscapeDataString(serviceId)}", ct: ct);

    /// <summary>Approves the saved classification (<c>POST /v1/companies/{id}/service-tax-profiles/{serviceId}/approve</c>).</summary>
    public Task<Result<ServiceTaxProfile>> ApproveAsync(string companyId, string serviceId, CancellationToken ct = default)
        => _http.RequestAsync<ServiceTaxProfile>(
            HttpMethod.Post,
            $"{Path(companyId)}/{Uri.EscapeDataString(serviceId)}/approve",
            ct: ct);

    private static string Path(string companyId) => $"/companies/{Uri.EscapeDataString(companyId)}/service-tax-profiles";
}

/// <summary>Status of the national NFS-e portal.</summary>
public sealed class FiscalPortalResource
{
    private readonly HttpTransport _http;

    internal FiscalPortalResource(HttpTransport http) => _http = http;

    /// <summary>Whether the national portal is answering for this key's environment (<c>GET /v1/fiscal-portal/status</c>).</summary>
    public Task<Result<PortalStatus>> StatusAsync(CancellationToken ct = default)
        => _http.RequestAsync<PortalStatus>(HttpMethod.Get, "/fiscal-portal/status", ct: ct);
}

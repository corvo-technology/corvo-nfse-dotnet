using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>
/// Scheduled and recurring invoices: list, iterate, get, update, pause, resume, cancel, listRuns.
/// Create one with <c>Invoices.CreateAsync</c> and <c>Options.Type</c> <c>SCHEDULE</c> or <c>RECURRING</c>.
/// </summary>
public sealed class SchedulesResource
{
    private readonly HttpTransport _http;

    internal SchedulesResource(HttpTransport http) => _http = http;

    /// <summary>Lists schedules (<c>GET /v1/schedules</c>). The cursor is the offset of the next page; pass it back untouched.</summary>
    public Task<Result<Paginated<Schedule>>> ListAsync(ScheduleListParams? parameters = null, CancellationToken ct = default)
        => _http.ListAsync<Schedule>("/schedules", Query(parameters), HttpTransport.MapPlain<Schedule>(), ct);

    /// <summary>Walks every page of schedules; a page failure is yielded as an error result.</summary>
    public IAsyncEnumerable<Result<Schedule>> IterateAsync(ScheduleListParams? parameters = null, CancellationToken ct = default)
        => _http.IterateAsync<Schedule>("/schedules", Query(parameters), HttpTransport.MapPlain<Schedule>(), ct);

    /// <summary>Fetches a schedule (<c>GET /v1/schedules/{id}</c>).</summary>
    public Task<Result<Schedule>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<Schedule>(HttpMethod.Get, Path(id), ct: ct);

    /// <summary>Changes a schedule; it applies to the next runs only (<c>PATCH /v1/schedules/{id}</c>).</summary>
    public Task<Result<Schedule>> UpdateAsync(string id, ScheduleUpdateParams parameters, CancellationToken ct = default)
        => _http.RequestAsync<Schedule>(HttpMethod.Patch, Path(id), body: parameters.ToJson(), ct: ct);

    /// <summary>Pauses a schedule (<c>POST /v1/schedules/{id}/pause</c>).</summary>
    public Task<Result<Schedule>> PauseAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<Schedule>(HttpMethod.Post, Path(id) + "/pause", ct: ct);

    /// <summary>Resumes a paused schedule at its next occurrence (<c>POST /v1/schedules/{id}/resume</c>).</summary>
    public Task<Result<Schedule>> ResumeAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<Schedule>(HttpMethod.Post, Path(id) + "/resume", ct: ct);

    /// <summary>Cancels a schedule; this is final (<c>POST /v1/schedules/{id}/cancel</c>).</summary>
    public Task<Result<Schedule>> CancelAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<Schedule>(HttpMethod.Post, Path(id) + "/cancel", ct: ct);

    /// <summary>Every attempt to run a schedule, newest first (<c>GET /v1/schedules/{id}/runs</c>).</summary>
    public Task<Result<Paginated<ScheduleRun>>> ListRunsAsync(
        string id,
        int? limit = null,
        string? cursor = null,
        CancellationToken ct = default)
        => _http.ListAsync<ScheduleRun>(
            Path(id) + "/runs",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapPlain<ScheduleRun>(),
            ct);

    private static string Path(string id) => $"/schedules/{Uri.EscapeDataString(id)}";

    private static Dictionary<string, string?> Query(ScheduleListParams? parameters)
    {
        parameters ??= new ScheduleListParams();
        return HttpTransport.Query(
            ("status", parameters.Status),
            ("companyId", parameters.CompanyId),
            ("limit", parameters.Limit),
            ("cursor", parameters.Cursor));
    }
}

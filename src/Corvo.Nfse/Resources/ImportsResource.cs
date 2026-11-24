using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>
/// Spreadsheet imports: start (multipart upload), list, get, emit, listRows, iterateRows, updateRow,
/// discard, downloadTemplate, downloadErrors.
/// </summary>
public sealed class ImportsResource
{
    private readonly HttpTransport _http;

    internal ImportsResource(HttpTransport http) => _http = http;

    /// <summary>
    /// Uploads a <c>.csv</c> or <c>.xlsx</c> file (up to 2,000 rows and 10 MB) as <c>multipart/form-data</c>
    /// (<c>POST /v1/imports</c>, <c>202</c>). The file is checked in the background: poll <see cref="GetAsync"/>
    /// until <c>status</c> is <c>ready</c>. <paramref name="companyId"/> issues every row that has no CNPJ value.
    /// </summary>
    public async Task<Result<ImportStart>> StartAsync(
        byte[] file,
        string fileName,
        string? companyId = null,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        if (companyId is not null)
        {
            var companyPart = new StringContent(companyId);
            companyPart.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"companyId\"");
            form.Add(companyPart);
        }

        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeOf(fileName));
        fileContent.Headers.TryAddWithoutValidation(
            "Content-Disposition",
            $"form-data; name=\"file\"; filename={QuoteHeaderValue(fileName)}");
        form.Add(fileContent);

        return await _http.SendMultipartAsync<ImportStart>("/imports", form, ct).ConfigureAwait(false);
    }

    /// <summary>Lists the latest imports (<c>GET /v1/imports</c>); <paramref name="limit"/> is 1 to 20 (10 by default).</summary>
    public Task<Result<IReadOnlyList<ImportJob>>> ListAsync(int? limit = null, CancellationToken ct = default)
        => _http.RequestAsync<IReadOnlyList<ImportJob>>(
            HttpMethod.Get,
            "/imports",
            query: HttpTransport.Query(("limit", limit)),
            ct: ct);

    /// <summary>Fetches an import with its counters (<c>GET /v1/imports/{id}</c>).</summary>
    public Task<Result<ImportJob>> GetAsync(string id, CancellationToken ct = default)
        => _http.RequestAsync<ImportJob>(HttpMethod.Get, Path(id), ct: ct);

    /// <summary>
    /// Emits every valid row (<c>POST /v1/imports/{id}/emit</c>, <c>202</c>); <paramref name="sendEmail"/> emails each
    /// customer. Follow it with <see cref="GetAsync"/> until <c>status</c> is <c>done</c>.
    /// </summary>
    public Task<Result<ImportJob>> EmitAsync(string id, bool? sendEmail = null, CancellationToken ct = default)
        => _http.RequestAsync<ImportJob>(
            HttpMethod.Post,
            Path(id) + "/emit",
            body: new JsonObject { ["sendEmail"] = sendEmail },
            ct: ct);

    /// <summary>Lists the rows of an import (<c>GET /v1/imports/{id}/rows</c>).</summary>
    public Task<Result<Paginated<ImportRow>>> ListRowsAsync(string id, ImportRowListParams? parameters = null, CancellationToken ct = default)
        => _http.ListAsync<ImportRow>(Path(id) + "/rows", RowQuery(parameters), HttpTransport.MapPlain<ImportRow>(), ct);

    /// <summary>Walks every page of rows; a page failure is yielded as an error result.</summary>
    public IAsyncEnumerable<Result<ImportRow>> IterateRowsAsync(string id, ImportRowListParams? parameters = null, CancellationToken ct = default)
        => _http.IterateAsync<ImportRow>(Path(id) + "/rows", RowQuery(parameters), HttpTransport.MapPlain<ImportRow>(), ct);

    /// <summary>
    /// Fixes a row (<c>PATCH /v1/imports/{id}/rows/{line}</c>). <paramref name="item"/> holds the fields to change
    /// (the row is validated again); omit it to validate the row again as it is.
    /// </summary>
    public Task<Result<ImportRow>> UpdateRowAsync(string id, int line, JsonObject? item = null, CancellationToken ct = default)
        => _http.RequestAsync<ImportRow>(
            HttpMethod.Patch,
            $"{Path(id)}/rows/{line}",
            body: item is null ? new JsonObject() : new JsonObject { ["item"] = JsonNode.Parse(item.ToJsonString()) },
            ct: ct);

    /// <summary>Discards an import that has not been emitted (<c>DELETE /v1/imports/{id}</c>, <c>204</c>).</summary>
    public Task<Result<bool>> DiscardAsync(string id, CancellationToken ct = default)
        => _http.RequestAckAsync(HttpMethod.Delete, Path(id), ct: ct);

    /// <summary>Downloads the spreadsheet template with every column (<c>GET /v1/imports/template</c>, CSV).</summary>
    public Task<Result<DownloadedFile>> DownloadTemplateAsync(CancellationToken ct = default)
        => _http.RequestRawAsync(HttpMethod.Get, "/imports/template", ct: ct);

    /// <summary>Downloads the rows that need attention (<c>GET /v1/imports/{id}/errors.csv</c>, CSV).</summary>
    public Task<Result<DownloadedFile>> DownloadErrorsAsync(string id, CancellationToken ct = default)
        => _http.RequestRawAsync(HttpMethod.Get, Path(id) + "/errors.csv", ct: ct);

    private static string Path(string id) => $"/imports/{Uri.EscapeDataString(id)}";

    private static Dictionary<string, string?> RowQuery(ImportRowListParams? parameters)
    {
        parameters ??= new ImportRowListParams();
        return HttpTransport.Query(
            ("status", parameters.Status),
            ("cursor", parameters.Cursor),
            ("limit", parameters.Limit));
    }

    private static string MimeTypeOf(string fileName)
        => fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";

    private static string QuoteHeaderValue(string value)
    {
        var safe = value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"\"{safe}\"";
    }
}

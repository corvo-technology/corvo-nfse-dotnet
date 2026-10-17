using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Corvo.Nfse.Transport;

/// <summary>
/// The request/unwrap/error-mapping layer shared by every resource. Wraps
/// <see cref="HttpClient"/> and never throws for API, network, or timeout failures —
/// those always come back as a <see cref="Result{T}"/> with a <see cref="CorvoApiError"/>.
/// </summary>
internal sealed partial class HttpTransport : IDisposable
{
    internal const string DefaultBaseUrl = "https://api.corvo.tech";
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _prefix;
    private readonly TimeSpan _timeout;

    public HttpTransport(
        string apiKey,
        string? baseUrl,
        string? prefix,
        TimeSpan? timeout,
        bool allowInsecureHttp,
        HttpMessageHandler? handler)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(
                "Corvo: ApiKey is required — pass new CorvoOptions { ApiKey = \"...\" } or set CORVO_API_KEY.",
                nameof(apiKey));
        }

        var resolvedBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl!;
        if (!Uri.TryCreate(resolvedBaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException($"Corvo: invalid BaseUrl: {resolvedBaseUrl}", nameof(baseUrl));
        }

        if (baseUri.Scheme == Uri.UriSchemeHttp &&
            !allowInsecureHttp &&
            !IsPrivateHost(baseUri.Host))
        {
            throw new ArgumentException(
                $"Corvo: http:// is only allowed for loopback/private hosts (got {baseUri.Host}). " +
                "Use https:// or set AllowInsecureHttp = true.",
                nameof(baseUrl));
        }

        _apiKey = apiKey;
        _baseUrl = resolvedBaseUrl.TrimEnd('/');
        _prefix = (prefix ?? "v1").Trim('/');
        _timeout = timeout ?? DefaultTimeout;
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: true);
        // Timeouts are enforced per request with a linked CancellationTokenSource so that
        // they can be distinguished from caller-initiated cancellation.
        _http.Timeout = Timeout.InfiniteTimeSpan;
    }

    /// <summary>Issues a JSON request and unwraps the response envelope per <paramref name="unwrap"/>.</summary>
    public async Task<Result<T>> RequestAsync<T>(
        HttpMethod method,
        string path,
        object? body = null,
        IReadOnlyDictionary<string, string?>? query = null,
        string? idempotencyKey = null,
        UnwrapMode unwrap = UnwrapMode.Data,
        CancellationToken ct = default)
    {
        using var request = BuildRequest(method, path, body, content: null, query, idempotencyKey);
        using var timeoutCts = CreateTimeoutCts(ct);
        try
        {
            using var response = await _http.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await ReadBodySafeAsync(response, timeoutCts.Token).ConfigureAwait(false);
                return new Result<T>(default, ToError(response, errorBody));
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return new Result<T>(default, null);
            }

            var text = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            if (text.Length == 0)
            {
                return new Result<T>(default, null);
            }

            if (!TryParseJson(text, out var doc))
            {
                return new Result<T>(default, InvalidJson(response));
            }

            using (doc)
            {
                try
                {
                    return new Result<T>(Unwrap<T>(doc.RootElement, unwrap), null);
                }
                catch (JsonException)
                {
                    return new Result<T>(default, InvalidJson(response));
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            return new Result<T>(default, new CorvoApiError(
                0, "timeout", $"Request timed out after {_timeout.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture)}ms"));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Caller-initiated cancellation is not a transport failure.
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or SocketException)
        {
            return new Result<T>(default, NetworkError(ex));
        }
    }

    /// <summary>Issues a request for a binary payload (invoice PDF/XML).</summary>
    public async Task<Result<DownloadedFile>> RequestRawAsync(
        HttpMethod method,
        string path,
        IReadOnlyDictionary<string, string?>? query = null,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        using var request = BuildRequest(method, path, body: null, content: null, query, idempotencyKey);
        using var timeoutCts = CreateTimeoutCts(ct);
        try
        {
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await ReadBodySafeAsync(response, timeoutCts.Token).ConfigureAwait(false);
                return new Result<DownloadedFile>(default, ToError(response, errorBody));
            }

            var buffer = await response.Content.ReadAsByteArrayAsync(timeoutCts.Token).ConfigureAwait(false);
            var mimeType = response.Content.Headers.ContentType?.ToString();
            var fileName = ParseFileName(response.Content.Headers.ContentDisposition?.ToString());
            return new Result<DownloadedFile>(new DownloadedFile(buffer, mimeType, fileName), null);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            return new Result<DownloadedFile>(default, new CorvoApiError(
                0, "timeout", $"Request timed out after {_timeout.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture)}ms"));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or SocketException)
        {
            return new Result<DownloadedFile>(default, NetworkError(ex));
        }
    }

    /// <summary>Fetches a <c>{ data, pagination }</c> page and maps each item.</summary>
    public async Task<Result<Paginated<T>>> ListAsync<T>(
        string path,
        IReadOnlyDictionary<string, string?>? query,
        Func<JsonElement, T> mapItem,
        CancellationToken ct = default)
    {
        var page = await RequestAsync<RawPage>(HttpMethod.Get, path, query: query, unwrap: UnwrapMode.None, ct: ct)
            .ConfigureAwait(false);
        if (page.Error is not null)
        {
            return new Result<Paginated<T>>(default, page.Error);
        }

        if (page.Data is null)
        {
            return new Result<Paginated<T>>(default, null);
        }

        List<T> items;
        try
        {
            items = page.Data.Data is null
                ? new List<T>()
                : page.Data.Data.Select(mapItem).ToList();
        }
        catch (JsonException)
        {
            return new Result<Paginated<T>>(default, new CorvoApiError(200, "invalid_json", "Invalid JSON in response"));
        }

        var pagination = page.Data.Pagination ?? new Pagination(0, false, null);
        return new Result<Paginated<T>>(new Paginated<T>(items, pagination), null);
    }

    /// <summary>
    /// Walks every page of a cursor-paginated list. A failure (first page or later) is
    /// surfaced as a yielded <see cref="Result{T}"/> with <see cref="Result{T}.Error"/> set —
    /// iteration never throws for API/network/timeout failures.
    /// </summary>
    public async IAsyncEnumerable<Result<T>> IterateAsync<T>(
        string path,
        IReadOnlyDictionary<string, string?>? query,
        Func<JsonElement, T> mapItem,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string?>? current = query;
        for (; ; )
        {
            var page = await ListAsync<T>(path, current, mapItem, ct).ConfigureAwait(false);
            if (page.Error is not null)
            {
                yield return new Result<T>(default, page.Error);
                yield break;
            }

            if (page.Data is null)
            {
                yield break;
            }

            foreach (var item in page.Data.Data)
            {
                yield return new Result<T>(item, null);
            }

            var pagination = page.Data.Pagination;
            if (!pagination.HasMore || pagination.NextCursor is null)
            {
                yield break;
            }

            var next = query is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?>(query);
            next["cursor"] = pagination.NextCursor;
            current = next;
        }
    }

    /// <summary>Marshals a multipart upload (certificate upload) without setting Content-Type manually.</summary>
    public async Task<Result<T>> SendMultipartAsync<T>(
        string path,
        HttpContent content,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(path, query: null));
        request.Headers.TryAddWithoutValidation("X-API-Key", _apiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Content = content;
        using var timeoutCts = CreateTimeoutCts(ct);
        try
        {
            using var response = await _http.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await ReadBodySafeAsync(response, timeoutCts.Token).ConfigureAwait(false);
                return new Result<T>(default, ToError(response, errorBody));
            }

            var text = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            if (text.Length == 0)
            {
                return new Result<T>(default, null);
            }

            if (!TryParseJson(text, out var doc))
            {
                return new Result<T>(default, InvalidJson(response));
            }

            using (doc)
            {
                try
                {
                    return new Result<T>(Unwrap<T>(doc.RootElement, UnwrapMode.Data), null);
                }
                catch (JsonException)
                {
                    return new Result<T>(default, InvalidJson(response));
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            return new Result<T>(default, new CorvoApiError(
                0, "timeout", $"Request timed out after {_timeout.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture)}ms"));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or SocketException)
        {
            return new Result<T>(default, NetworkError(ex));
        }
    }

    /// <summary>Public entry point used by resources to build query dictionaries (nulls are skipped).</summary>
    public static Dictionary<string, string?> Query(params (string Key, object? Value)[] entries)
    {
        var result = new Dictionary<string, string?>(entries.Length);
        foreach (var (key, value) in entries)
        {
            result[key] = value switch
            {
                null => null,
                string s => s,
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value!.ToString(),
            };
        }
        return result;
    }

    /// <summary>Item mapper for plain entities (<c>data</c> array items are the entity itself).</summary>
    public static Func<JsonElement, T> MapPlain<T>()
        => element => Deserialize<T>(element)!;

    internal static T? Deserialize<T>(JsonElement element)
        => element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? default
            : JsonSerializer.Deserialize<T>(element.GetRawText(), CorvoJson.Options);

    private HttpRequestMessage BuildRequest(
        HttpMethod method,
        string path,
        object? body,
        HttpContent? content,
        IReadOnlyDictionary<string, string?>? query,
        string? idempotencyKey)
    {
        var request = new HttpRequestMessage(method, BuildUrl(path, query));
        request.Headers.TryAddWithoutValidation("X-API-Key", _apiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        if (idempotencyKey is { Length: > 0 })
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        if (content is not null)
        {
            request.Content = content;
        }
        else if (body is not null)
        {
            // camelCase pass-through: the wire format is the request object, unchanged.
            // Pre-built JsonNode bodies (PATCH updates) carry explicit nulls and exact
            // wire names, so they are written verbatim with the default (never-ignore) policy.
            var json = body is JsonNode node
                ? node.ToJsonString()
                : JsonSerializer.Serialize(body, CorvoJson.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private string BuildUrl(string path, IReadOnlyDictionary<string, string?>? query)
    {
        var builder = new StringBuilder(_baseUrl);
        if (_prefix.Length > 0)
        {
            builder.Append('/').Append(_prefix);
        }

        if (path.Length > 0 && path[0] != '/')
        {
            builder.Append('/');
        }

        builder.Append(path);

        if (query is not null)
        {
            var first = true;
            foreach (var pair in query)
            {
                if (pair.Value is null)
                {
                    continue;
                }

                builder.Append(first ? '?' : '&');
                builder.Append(Uri.EscapeDataString(pair.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(pair.Value));
                first = false;
            }
        }

        return builder.ToString();
    }

    private CancellationTokenSource CreateTimeoutCts(CancellationToken ct)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (_timeout > TimeSpan.Zero)
        {
            cts.CancelAfter(_timeout);
        }

        return cts;
    }

    private static async Task<string> ReadBodySafeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or NotSupportedException)
        {
            return string.Empty;
        }
    }

    private static T? Unwrap<T>(JsonElement body, UnwrapMode mode)
    {
        switch (mode)
        {
            case UnwrapMode.None:
                return Deserialize<T>(body);
            default:
                return TryGetData(body, out var data) ? Deserialize<T>(data) : default;
        }
    }

    private static bool TryGetData(JsonElement body, out JsonElement data)
    {
        if (body.ValueKind == JsonValueKind.Object &&
            body.TryGetProperty("data", out data) &&
            data.ValueKind != JsonValueKind.Null)
        {
            return true;
        }

        data = default;
        return false;
    }

    private static bool TryParseJson(string text, out JsonDocument doc)
    {
        try
        {
            doc = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            // Malformed JSON: callers map this to the invalid_json error.
            doc = null!;
            return false;
        }
    }

    private static CorvoApiError InvalidJson(HttpResponseMessage response)
        => new(
            (int)response.StatusCode,
            "invalid_json",
            "Invalid JSON in response",
            RequestId: HeaderValue(response, "X-Request-Id"));

    private static CorvoApiError NetworkError(Exception ex)
        => new(0, "network_error", $"Network request failed: {ex.Message}");

    private static CorvoApiError ToError(HttpResponseMessage response, string bodyText)
    {
        var status = (int)response.StatusCode;
        string? code = null;
        string? message = null;
        string? requestId = null;
        int? retryAfter = null;
        IReadOnlyList<CorvoErrorDetail>? details = null;

        if (bodyText.Length > 0 && TryParseJson(bodyText, out var doc))
        {
            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetString(root, "code", out var bodyCode))
                    {
                        code = bodyCode;
                    }

                    if (TryGetString(root, "requestId", out var bodyRequestId))
                    {
                        requestId = bodyRequestId;
                    }

                    message = ReadMessage(root);
                    if (TryGetDouble(root, "retryAfter", out var bodyRetryAfter))
                    {
                        retryAfter = (int)bodyRetryAfter;
                    }

                    if (root.TryGetProperty("details", out var detailsElement) &&
                        detailsElement.ValueKind == JsonValueKind.Array)
                    {
                        details = ReadDetails(detailsElement);
                    }
                }
            }
        }

        if (code is null || code.Length == 0)
        {
            code = $"http_{status}";
        }

        message ??= $"Request failed with status {status}";
        requestId ??= HeaderValue(response, "X-Request-Id");

        if (retryAfter is null &&
            HeaderValue(response, "Retry-After") is { } retryAfterHeader &&
            int.TryParse(retryAfterHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out var headerRetryAfter))
        {
            retryAfter = headerRetryAfter;
        }

        return new CorvoApiError(status, code, message, details, requestId, retryAfter);
    }

    private static string? ReadMessage(JsonElement root)
    {
        if (root.TryGetProperty("message", out var message))
        {
            if (message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }

            if (message.ValueKind == JsonValueKind.Array)
            {
                return string.Join("; ", message.EnumerateArray().Select(static item =>
                    item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.ToString()));
            }
        }

        return TryGetString(root, "error", out var error) ? error : null;
    }

    private static IReadOnlyList<CorvoErrorDetail>? ReadDetails(JsonElement details)
    {
        var result = new List<CorvoErrorDetail>();
        foreach (var item in details.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            TryGetString(item, "field", out var field);
            TryGetString(item, "code", out var code);
            TryGetString(item, "message", out var message);
            result.Add(new CorvoErrorDetail(field ?? string.Empty, code ?? string.Empty, message ?? string.Empty));
        }

        return result.Count > 0 ? result : null;
    }

    private static bool TryGetString(JsonElement root, string property, out string? value)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(property, out var element) &&
            element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString();
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryGetDouble(JsonElement root, string property, out double value)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(property, out var element) &&
            element.ValueKind == JsonValueKind.Number &&
            element.TryGetDouble(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static string? HeaderValue(HttpResponseMessage response, string name)
        => response.Headers.TryGetValues(name, out var values)
            ? values.FirstOrDefault()
            : response.Content.Headers.TryGetValues(name, out var contentValues)
                ? contentValues.FirstOrDefault()
                : null;

    private static string? ParseFileName(string? contentDisposition)
    {
        if (contentDisposition is null || contentDisposition.Length == 0)
        {
            return null;
        }

        var match = FileNameRegex().Match(contentDisposition);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static bool IsPrivateHost(string host)
    {
        host = host.ToLowerInvariant();
        if (host is "localhost" or "[::1]" or "::1" || host.EndsWith(".localhost", StringComparison.Ordinal))
        {
            return true;
        }

        var parts = host.Split('.');
        if (parts.Length != 4)
        {
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var a) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var b))
        {
            return false;
        }

        return a is 127 or 10 or 0 ||
               (a == 192 && b == 168) ||
               (a == 172 && b >= 16 && b <= 31) ||
               (a == 169 && b == 254);
    }

    [GeneratedRegex("filename=\"([^\"]+)\"")]
    private static partial Regex FileNameRegex();

    private sealed record RawPage(JsonElement[]? Data, Pagination? Pagination);

    public void Dispose() => _http.Dispose();
}

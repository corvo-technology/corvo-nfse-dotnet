using System.Net.Http;

namespace Corvo.Nfse;

/// <summary>Configuration for <see cref="CorvoClient"/>.</summary>
public sealed class CorvoOptions
{
    /// <summary>
    /// API key sent as the <c>X-API-Key</c> header (<c>corvo_test_</c>/<c>corvo_live_</c> + 40 hex).
    /// Falls back to the <c>CORVO_API_KEY</c> environment variable when <c>null</c>.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API base URL. Falls back to <c>CORVO_BASE_URL</c>, then to the public
    /// <c>https://api.corvo.tech</c>. Point this at your self-hosted install.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>API path prefix. Defaults to <c>v1</c> (matches the backend's <c>API_PREFIX</c>).</summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Per-request timeout. Defaults to 30 seconds. Zero or negative means no timeout.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Allow plain <c>http://</c> base URLs for hosts that are not loopback/private.
    /// Off by default.
    /// </summary>
    public bool AllowInsecureHttp { get; set; }

    /// <summary>
    /// Optional <see cref="HttpMessageHandler"/> to send requests through. Intended for
    /// unit tests (mock handlers) and advanced pipelines (logging, retries). The client
    /// disposes it together with its <see cref="HttpClient"/>.
    /// </summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }
}

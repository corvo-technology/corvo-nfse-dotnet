using System.Net.Http;
using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>
/// The Corvo NFS-e API client. Composes the resource namespaces over a shared transport:
/// <see cref="Companies"/>, <see cref="Certificates"/>, <see cref="Invoices"/>,
/// <see cref="Webhooks"/>, <see cref="Credits"/>, <see cref="Billing"/>, <see cref="Plans"/>, <see cref="Health"/>.
/// </summary>
/// <example>
/// <code>
/// using var client = new CorvoClient(new CorvoOptions { ApiKey = "corvo_live_..." });
/// var page = await client.Companies.ListAsync(limit: 10);
/// if (page.Error is { } error) Console.WriteLine($"{error.Code} {error.Status}");
/// </code>
/// </example>
public sealed class CorvoClient : IDisposable
{
    /// <summary>The SDK version, the same as the package version.</summary>
    public const string Version = "0.1.1";

    private readonly HttpTransport _transport;

    /// <summary>Companies: create, get, update, remove, list, iterate.</summary>
    public CompaniesResource Companies { get; }

    /// <summary>Certificates: upload (multipart), list, remove.</summary>
    public CertificatesResource Certificates { get; }

    /// <summary>Invoices: create, calculate, get, list, iterate, events, pdf, xml, downloadUrl, sendEmail.</summary>
    public InvoicesResource Invoices { get; }

    /// <summary>Webhooks: list, create, remove, test, plus the nested <c>Deliveries</c> namespace.</summary>
    public WebhooksResource Webhooks { get; }

    /// <summary>Credits: balance, plans, history, iterateHistory.</summary>
    public CreditsResource Credits { get; }

    /// <summary>Billing: plan, usage.</summary>
    public BillingResource Billing { get; }

    /// <summary>The public price catalog: list, get, quote.</summary>
    public PlansResource Plans { get; }

    /// <summary>Public health checks: live, ready.</summary>
    public HealthResource Health { get; }

    /// <summary>
    /// Creates a client. Reads <c>CORVO_API_KEY</c>/<c>CORVO_BASE_URL</c> as fallbacks for
    /// unset options; throws <see cref="ArgumentException"/> when misconfigured (missing key,
    /// invalid base URL, or plain http:// to a public host without <see cref="CorvoOptions.AllowInsecureHttp"/>).
    /// </summary>
    /// <param name="options">Client options; defaults to environment variables + <c>https://api.corvo.tech</c>.</param>
    /// <exception cref="ArgumentException">The configuration is invalid.</exception>
    public CorvoClient(CorvoOptions? options = null)
    {
        options ??= new CorvoOptions();

        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("CORVO_API_KEY");
        var baseUrl = options.BaseUrl
            ?? Environment.GetEnvironmentVariable("CORVO_BASE_URL")
            ?? HttpTransport.DefaultBaseUrl;

        _transport = new HttpTransport(
            apiKey: apiKey ?? string.Empty,
            baseUrl: baseUrl,
            prefix: options.Prefix,
            timeout: options.Timeout,
            allowInsecureHttp: options.AllowInsecureHttp,
            handler: options.HttpMessageHandler);

        Companies = new CompaniesResource(_transport);
        Certificates = new CertificatesResource(_transport);
        Invoices = new InvoicesResource(_transport);
        Webhooks = new WebhooksResource(_transport);
        Credits = new CreditsResource(_transport);
        Billing = new BillingResource(_transport);
        Plans = new PlansResource(_transport);
        Health = new HealthResource(_transport);
    }

    /// <summary>Releases the underlying <see cref="HttpClient"/> (and its handler).</summary>
    public void Dispose() => _transport.Dispose();
}

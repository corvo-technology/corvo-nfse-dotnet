using System.Net.Http;
using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>
/// The Corvo NFS-e API client. Composes the resource namespaces over a shared transport:
/// <see cref="Companies"/>, <see cref="Certificates"/>, <see cref="Invoices"/>,
/// <see cref="Webhooks"/>, <see cref="Credits"/>, <see cref="Billing"/>, <see cref="Plans"/>, <see cref="Health"/>,
/// plus <see cref="Customers"/>, <see cref="Cnpj"/>, <see cref="ServiceFavorites"/>, <see cref="AccountantPackage"/>,
/// <see cref="InvoicePackage"/>, <see cref="DpsSequence"/>, <see cref="FiscalProfiles"/>, <see cref="ServiceTaxProfiles"/>,
/// <see cref="FiscalPortal"/>, <see cref="Schedules"/>, <see cref="Imports"/> and the integration namespaces.
/// The namespaces follow the <c>operationId</c> prefixes of the API contract (<c>docs/openapi</c>).
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
    public const string Version = "0.3.0";

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

    /// <summary>Saved customers: search, iterate, get, update, delete.</summary>
    public CustomersResource Customers { get; }

    /// <summary>CNPJ registry lookup.</summary>
    public CnpjResource Cnpj { get; }

    /// <summary>Favorite services of a company: list, create, delete.</summary>
    public ServiceFavoritesResource ServiceFavorites { get; }

    /// <summary>The monthly package for the accountant: get, update, send, listSends.</summary>
    public AccountantPackageResource AccountantPackage { get; }

    /// <summary>The monthly ZIP of a company's invoices: summary, download.</summary>
    public InvoicePackageResource InvoicePackage { get; }

    /// <summary>The DPS numbering of a company: get, set.</summary>
    public DpsSequenceResource DpsSequence { get; }

    /// <summary>The fiscal profile history of a company: list, upsert, approve.</summary>
    public FiscalProfilesResource FiscalProfiles { get; }

    /// <summary>The service tax classifications of a company: list, create, update, delete, approve.</summary>
    public ServiceTaxProfilesResource ServiceTaxProfiles { get; }

    /// <summary>Status of the national NFS-e portal.</summary>
    public FiscalPortalResource FiscalPortal { get; }

    /// <summary>Scheduled and recurring invoices: list, get, update, pause, resume, cancel, listRuns.</summary>
    public SchedulesResource Schedules { get; }

    /// <summary>Spreadsheet imports: start, list, get, emit, rows, discard, template, errors.</summary>
    public ImportsResource Imports { get; }

    /// <summary>Integration providers.</summary>
    public IntegrationsResource Integrations { get; }

    /// <summary>Connections between a company and a provider.</summary>
    public IntegrationConnectionsResource IntegrationConnections { get; }

    /// <summary>Operations received from providers: complete the customer, retry.</summary>
    public IntegrationOperationsResource IntegrationOperations { get; }

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
        Customers = new CustomersResource(_transport);
        Cnpj = new CnpjResource(_transport);
        ServiceFavorites = new ServiceFavoritesResource(_transport);
        AccountantPackage = new AccountantPackageResource(_transport);
        InvoicePackage = new InvoicePackageResource(_transport);
        DpsSequence = new DpsSequenceResource(_transport);
        FiscalProfiles = new FiscalProfilesResource(_transport);
        ServiceTaxProfiles = new ServiceTaxProfilesResource(_transport);
        FiscalPortal = new FiscalPortalResource(_transport);
        Schedules = new SchedulesResource(_transport);
        Imports = new ImportsResource(_transport);
        Integrations = new IntegrationsResource(_transport);
        IntegrationConnections = new IntegrationConnectionsResource(_transport);
        IntegrationOperations = new IntegrationOperationsResource(_transport);
    }

    /// <summary>Releases the underlying <see cref="HttpClient"/> (and its handler).</summary>
    public void Dispose() => _transport.Dispose();
}

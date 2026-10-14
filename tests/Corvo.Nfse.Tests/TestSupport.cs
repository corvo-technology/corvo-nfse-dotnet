using System.Net;
using System.Text;

namespace Corvo.Nfse.Tests;

/// <summary>A request as seen by the mock handler.</summary>
internal sealed record CapturedRequest(
    HttpMethod Method,
    string Url,
    string? Body,
    Dictionary<string, string> Headers);

/// <summary>Mock <see cref="HttpMessageHandler"/> — no network, no live backend.</summary>
internal sealed class TestHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    public List<CapturedRequest> Requests { get; } = new();

    private TestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        => _responder = responder;

    /// <summary>Always responds with the same response.</summary>
    public static TestHandler Respond(HttpResponseMessage response)
        => new((_, _) => Task.FromResult(response));

    /// <summary>Always responds with the same JSON body.</summary>
    public static TestHandler RespondJson(string json, HttpStatusCode status = HttpStatusCode.OK)
        => Respond(Response(json, status));

    /// <summary>Responds with a sequence of JSON bodies, one per request (last one repeats).</summary>
    public static TestHandler Sequence(params string[] bodies)
    {
        var index = 0;
        return new TestHandler((_, _) =>
        {
            var body = bodies[Math.Min(index, bodies.Length - 1)];
            index++;
            return Task.FromResult(Response(body));
        });
    }

    /// <summary>Fails every request with the given exception.</summary>
    public static TestHandler Fail(Exception exception)
        => new((_, _) => Task.FromException<HttpResponseMessage>(exception));

    /// <summary>Delays (honouring cancellation) before responding — used for timeout tests.</summary>
    public static TestHandler Delay(TimeSpan delay, HttpResponseMessage? final = null)
        => new(async (_, ct) =>
        {
            await Task.Delay(delay, ct);
            return final ?? Response("""{"data":null}""");
        });

    /// <summary>Builds a JSON response, optionally configured with extra headers.</summary>
    public static HttpResponseMessage Response(
        string? body,
        HttpStatusCode status = HttpStatusCode.OK,
        Action<HttpResponseMessage>? configure = null)
    {
        var response = new HttpResponseMessage(status);
        response.Content = body is null
            ? new ByteArrayContent(Array.Empty<byte>())
            : new StringContent(body, Encoding.UTF8, "application/json");
        configure?.Invoke(response);
        return response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in request.Headers)
        {
            headers[pair.Key] = string.Join(",", pair.Value);
        }

        if (request.Content is not null)
        {
            foreach (var pair in request.Content.Headers)
            {
                headers[pair.Key] = string.Join(",", pair.Value);
            }
        }

        Requests.Add(new CapturedRequest(request.Method, request.RequestUri!.OriginalString, body, headers));
        return await _responder(request, cancellationToken);
    }
}

/// <summary>Client factory for unit tests.</summary>
internal static class TestClient
{
    public static string ApiKey { get; } = "corvo_test_" + new string('a', 40);

    public const string BaseUrl = "https://api.test.local";

    public static CorvoClient Create(
        TestHandler handler,
        TimeSpan? timeout = null,
        string? apiKey = null,
        string? baseUrl = null,
        bool allowInsecureHttp = false)
        => new(new CorvoOptions
        {
            ApiKey = apiKey ?? ApiKey,
            BaseUrl = baseUrl ?? BaseUrl,
            Timeout = timeout,
            AllowInsecureHttp = allowInsecureHttp,
            HttpMessageHandler = handler,
        });
}

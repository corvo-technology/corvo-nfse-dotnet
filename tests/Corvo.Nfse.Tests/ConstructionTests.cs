namespace Corvo.Nfse.Tests;

public class ConstructionTests
{
    [Fact]
    public void Missing_API_key_throws_ArgumentException()
    {
        var original = Environment.GetEnvironmentVariable("CORVO_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("CORVO_API_KEY", null);
            var exception = Assert.Throws<ArgumentException>(() => new CorvoClient());
            Assert.Contains("CORVO_API_KEY", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CORVO_API_KEY", original);
        }
    }

    [Fact]
    public void Invalid_base_url_throws()
    {
        Assert.Throws<ArgumentException>(() => TestClient.Create(
            TestHandler.RespondJson("{}"),
            baseUrl: "not a url"));
    }

    [Fact]
    public void Plain_http_to_a_public_host_throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => TestClient.Create(
            TestHandler.RespondJson("{}"),
            baseUrl: "http://api.example.com"));

        Assert.Contains("AllowInsecureHttp", exception.Message);
    }

    [Fact]
    public void Plain_http_loopback_is_allowed_by_default()
    {
        using var client = TestClient.Create(TestHandler.RespondJson("{}"), baseUrl: "http://localhost:4000");
        Assert.NotNull(client);
    }

    [Fact]
    public void Plain_http_public_host_is_allowed_when_opted_in()
    {
        using var client = TestClient.Create(
            TestHandler.RespondJson("{}"),
            baseUrl: "http://api.example.com",
            allowInsecureHttp: true);
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Environment_variables_supply_key_and_base_url()
    {
        var originalKey = Environment.GetEnvironmentVariable("CORVO_API_KEY");
        var originalBaseUrl = Environment.GetEnvironmentVariable("CORVO_BASE_URL");
        var envKey = "corvo_live_" + new string('b', 40);
        try
        {
            Environment.SetEnvironmentVariable("CORVO_API_KEY", envKey);
            Environment.SetEnvironmentVariable("CORVO_BASE_URL", "https://env.example.com");

            var handler = TestHandler.RespondJson("""{"status":"ok"}""");
            using var client = new CorvoClient(new CorvoOptions { HttpMessageHandler = handler });
            var result = await client.Health.LiveAsync();

            Assert.Null(result.Error);
            Assert.Equal("https://env.example.com/v1/health/live", handler.Requests[0].Url);
            Assert.Equal(envKey, handler.Requests[0].Headers["X-API-Key"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CORVO_API_KEY", originalKey);
            Environment.SetEnvironmentVariable("CORVO_BASE_URL", originalBaseUrl);
        }
    }

    [Fact]
    public void Version_constant_matches_the_package()
    {
        Assert.Equal("0.1.1", CorvoClient.Version);
    }
}

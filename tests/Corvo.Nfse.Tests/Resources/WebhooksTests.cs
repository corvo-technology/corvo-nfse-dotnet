using System.Net;

namespace Corvo.Nfse.Tests.Resources;

public class WebhooksTests
{
    [Fact]
    public async Task Create_returns_the_secret_and_posts_the_exact_body()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"id":"wh_1","url":"https://hooks.example/corvo","events":["nfse.invoice.authorized"],"active":true,"secret":"whsec_live_1","createdAt":"2026-01-01T00:00:00Z","updatedAt":"2026-01-01T00:00:00Z"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Webhooks.CreateAsync(new WebhookCreateParams
        {
            Url = "https://hooks.example/corvo",
            Events = new[] { WebhookEvents.InvoiceAuthorized },
        });

        Assert.Null(result.Error);
        Assert.Equal("whsec_live_1", result.Data!.Secret);
        Assert.Equal(
            """{"url":"https://hooks.example/corvo","events":["nfse.invoice.authorized"]}""",
            handler.Requests[0].Body);
    }

    [Fact]
    public async Task List_maps_webhooks()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"id":"wh_1","url":"https://hooks.example","events":["webhook.test"],"active":true}],"pagination":{"limit":20,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Webhooks.ListAsync();

        Assert.Null(result.Error);
        var webhook = result.Data!.Data[0];
        Assert.Equal("wh_1", webhook.Id);
        Assert.Contains(WebhookEvents.WebhookTest, webhook.Events);
    }

    [Fact]
    public async Task Remove_and_test_parse_their_small_payloads()
    {
        var handler = TestHandler.Sequence(
            """{"data":{"deleted":true}}""",
            """{"data":{"sent":true}}""");
        using var client = TestClient.Create(handler);

        var removed = await client.Webhooks.RemoveAsync("wh_1");
        Assert.True(removed.Data!.Deleted);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.EndsWith("/webhooks/wh_1", handler.Requests[0].Url);

        var tested = await client.Webhooks.TestAsync("wh_1");
        Assert.True(tested.Data!.Sent);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.EndsWith("/webhooks/wh_1/test", handler.Requests[1].Url);
    }

    [Fact]
    public async Task Deliveries_list_forwards_filters()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"id":"dlv_1","eventType":"nfse.invoice.authorized"}],"pagination":{"limit":25,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Webhooks.Deliveries.ListAsync(new WebhookDeliveryListParams
        {
            Hours = 24,
            Environment = "live",
            Limit = 25,
        });

        Assert.Null(result.Error);
        Assert.Equal("dlv_1", result.Data!.Data[0].Id);
        Assert.Equal(
            "https://api.test.local/v1/webhooks/deliveries?hours=24&environment=live&limit=25",
            handler.Requests[0].Url);
    }

    [Fact]
    public async Task Deliveries_stats_reads_the_counters()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"total":5,"sent":4,"failed":1,"pending":0,"delayed":0}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Webhooks.Deliveries.StatsAsync(hours: 12, environment: "test");

        Assert.Null(result.Error);
        var stats = result.Data!;
        Assert.Equal(5, stats.Total);
        Assert.Equal(4, stats.Sent);
        Assert.Equal(
            "https://api.test.local/v1/webhooks/deliveries/stats?hours=12&environment=test",
            handler.Requests[0].Url);
    }

    [Fact]
    public async Task Deliveries_retry_posts_to_the_retry_path()
    {
        var handler = TestHandler.RespondJson("""{"data":{"retried":true}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Webhooks.Deliveries.RetryAsync("dlv_1");

        Assert.True(result.Data!.Retried);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.EndsWith("/webhooks/deliveries/dlv_1/retry", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Webhook_failures_surface_as_error_results()
    {
        var handler = TestHandler.RespondJson(
            """{"code":"ERROR_WEBHOOK_URL_INVALID","message":"URL must be https"}""",
            HttpStatusCode.BadRequest);
        using var client = TestClient.Create(handler);

        var result = await client.Webhooks.CreateAsync(new WebhookCreateParams
        {
            Url = "http://insecure.example",
            Events = new[] { WebhookEvents.WebhookTest },
        });

        Assert.True(result.IsError);
        Assert.Null(result.Data);
        var error = result.Error!;
        Assert.Equal("ERROR_WEBHOOK_URL_INVALID", error.Code);
        Assert.Equal(400, error.Status);
    }
}

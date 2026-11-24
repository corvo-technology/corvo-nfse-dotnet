namespace Corvo.Nfse.Tests.Resources;

public class IntegrationsTests
{
    private const string Connection =
        """{"id":"con_1","provider":"hotmart","companyId":"cmp_1","environment":"test","state":"draft","externalAccountId":null,"activeFrom":null,"webhookUrl":"https://api.corvo.tech/v1/integrations/inbound/hotmart/abc","createdAt":"2026-11-24T00:00:00.000Z"}""";

    [Fact]
    public async Task Providers_list_parses_credential_fields()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"id":"hotmart","name":"Hotmart","version":"1","supportedFlows":["sale"],"credentialFields":[{"key":"hottok","label":"Hottok","secret":true,"revealOnce":true}],"webhookInstructions":{"en":"Paste it","pt":"Cole"}}]}""");
        using var client = TestClient.Create(handler);

        var result = await client.Integrations.ListProvidersAsync();

        Assert.Null(result.Error);
        Assert.EndsWith("/v1/integrations/providers", handler.Requests[0].Url);
        var provider = result.Data![0];
        Assert.Equal("hotmart", provider.Id);
        Assert.True(provider.CredentialFields[0].Secret);
        Assert.True(provider.CredentialFields[0].RevealOnce);
        Assert.Null(provider.CredentialFields[0].Generate);
        Assert.Equal("Cole", provider.WebhookInstructions.Pt);
    }

    [Fact]
    public async Task Connections_create_sends_credentials_and_returns_the_secrets()
    {
        var created = Connection.TrimEnd('}') + ""","secrets":{"signingSecret":"s3"}}""";
        var handler = TestHandler.RespondJson($$$"""{"data":{{{created}}}}""", System.Net.HttpStatusCode.Created);
        using var client = TestClient.Create(handler);

        var result = await client.IntegrationConnections.CreateAsync(new IntegrationConnectionCreateParams
        {
            Provider = "hotmart",
            CompanyId = "cmp_1",
            Credentials = new Dictionary<string, string> { ["hottok"] = "abc" },
            Environment = "test",
        });

        Assert.Null(result.Error);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.EndsWith("/v1/integrations/connections", handler.Requests[0].Url);
        Assert.Equal(
            """{"provider":"hotmart","companyId":"cmp_1","credentials":{"hottok":"abc"},"environment":"test"}""",
            handler.Requests[0].Body);
        Assert.Equal("draft", result.Data!.State);
        Assert.Equal("s3", result.Data.Secrets["signingSecret"].GetString());
    }

    [Fact]
    public async Task Connection_lifecycle_routes()
    {
        var handler = TestHandler.Sequence($$$"""{"data":{{{Connection}}}}""");
        using var client = TestClient.Create(handler);

        await client.IntegrationConnections.GetAsync("con_1");
        await client.IntegrationConnections.ValidateAsync("con_1");
        await client.IntegrationConnections.UpdateCredentialsAsync("con_1", new Dictionary<string, string> { ["hottok"] = "new" });
        await client.IntegrationConnections.ActivateAsync("con_1");
        await client.IntegrationConnections.PauseAsync("con_1");
        await client.IntegrationConnections.DisconnectAsync("con_1");

        Assert.Equal(
            new[]
            {
                "GET /v1/integrations/connections/con_1",
                "POST /v1/integrations/connections/con_1/validate",
                "PUT /v1/integrations/connections/con_1/credentials",
                "POST /v1/integrations/connections/con_1/activate",
                "POST /v1/integrations/connections/con_1/pause",
                "POST /v1/integrations/connections/con_1/disconnect",
            },
            handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}").ToArray());
        Assert.Equal("""{"credentials":{"hottok":"new"}}""", handler.Requests[2].Body);
    }

    [Fact]
    public async Task Connections_list_rules_events_operations()
    {
        var handler = TestHandler.Sequence(
            $$$"""{"data":[{{{Connection}}}]}""",
            """{"data":[{"id":"r1","externalProductId":null,"service":{"description":"x"},"sendEmail":true,"allowAnonymousCustomer":false,"cancelOnRefund":true}]}""",
            """{"data":[{"id":"r1","externalProductId":"p1","service":{"description":"x"},"sendEmail":false,"allowAnonymousCustomer":true,"cancelOnRefund":false}]}""",
            """{"data":[{"id":"e1","deliveryId":"d1","externalType":"PURCHASE_APPROVED","state":"processed","attempts":1,"lastError":null,"operationId":"op_1","createdAt":"2026-11-24T00:00:00.000Z"}]}""",
            """{"data":[{"id":"op_1","operationKey":"k","commercialState":"paid","fiscalStatus":"blocked","blockReason":"customer_tax_id_missing","amountCents":9900,"currency":"BRL","customer":{"name":"M","email":null,"taxId":null,"taxIdMasked":false},"invoiceId":null,"paidAt":null,"createdAt":"2026-11-24T00:00:00.000Z","updatedAt":"2026-11-24T00:00:00.000Z"}]}""");
        using var client = TestClient.Create(handler);

        var list = await client.IntegrationConnections.ListAsync();
        Assert.Equal("con_1", list.Data![0].Id);
        Assert.EndsWith("/v1/integrations/connections", handler.Requests[0].Url);

        var rules = await client.IntegrationConnections.ListRulesAsync("con_1");
        Assert.True(rules.Data![0].CancelOnRefund);

        var replaced = await client.IntegrationConnections.ReplaceRulesAsync("con_1", new[]
        {
            new IntegrationRuleInput
            {
                SendEmail = false,
                Service = new InvoiceServiceInput { Description = "x" },
                ExternalProductId = "p1",
                AllowAnonymousCustomer = true,
            },
        });
        Assert.Equal(HttpMethod.Put, handler.Requests[2].Method);
        Assert.Equal(
            """{"rules":[{"sendEmail":false,"service":{"description":"x"},"externalProductId":"p1","allowAnonymousCustomer":true}]}""",
            handler.Requests[2].Body);
        Assert.Equal("p1", replaced.Data![0].ExternalProductId);

        var events = await client.IntegrationConnections.ListEventsAsync("con_1", limit: 10);
        Assert.EndsWith("/connections/con_1/events?limit=10", handler.Requests[3].Url);
        Assert.Equal("processed", events.Data![0].State);

        var operations = await client.IntegrationConnections.ListOperationsAsync("con_1", limit: 5);
        Assert.EndsWith("/connections/con_1/operations?limit=5", handler.Requests[4].Url);
        var operation = operations.Data![0];
        Assert.Equal("blocked", operation.FiscalStatus);
        Assert.Equal(9900, operation.AmountCents);
        Assert.False(operation.Customer!.TaxIdMasked);
    }

    [Fact]
    public async Task Operations_complete_customer_and_retry()
    {
        var handler = TestHandler.Sequence(
            """{"data":{"id":"op_1","operationKey":"k","commercialState":"paid","fiscalStatus":"none","amountCents":100,"currency":"BRL","createdAt":"x","updatedAt":"x"}}""",
            """{"data":{"state":"processed","operationId":"op_1"}}""");
        using var client = TestClient.Create(handler);

        var completed = await client.IntegrationOperations.CompleteCustomerAsync(
            "op_1",
            new IntegrationCustomerParams { TaxId = "52998224725", Name = "Maria" });
        Assert.Equal("none", completed.Data!.FiscalStatus);
        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.EndsWith("/v1/integrations/operations/op_1/customer", handler.Requests[0].Url);
        Assert.Equal("""{"taxId":"52998224725","name":"Maria"}""", handler.Requests[0].Body);

        var retried = await client.IntegrationOperations.RetryAsync("op_1");
        Assert.Equal("processed", retried.Data!.State);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.EndsWith("/v1/integrations/operations/op_1/retry", handler.Requests[1].Url);
    }
}

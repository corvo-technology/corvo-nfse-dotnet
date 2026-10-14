using System.Net;

namespace Corvo.Nfse.Tests;

public class TransportTests
{
    [Fact]
    public async Task Unwraps_data_envelope()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"cmp_1","legalName":"Loja Center"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Companies.GetAsync("cmp_1");

        Assert.Null(result.Error);
        Assert.True(result.IsSuccess);
        var company = result.Data!;
        Assert.Equal("cmp_1", company.Id);
        Assert.Equal("Loja Center", company.LegalName);
        Assert.Equal($"https://api.test.local/v1/companies/cmp_1", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Unwraps_data_props_for_invoice_entities()
    {
        var handler = TestHandler.RespondJson("""{"data":{"props":{"id":"inv_1","status":"authorized"}}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.GetAsync("inv_1");

        Assert.Null(result.Error);
        var invoice = result.Data!;
        Assert.Equal("inv_1", invoice.Id);
        Assert.Equal(InvoiceStatuses.Authorized, invoice.Status);
    }

    [Fact]
    public async Task Health_returns_the_raw_body_without_envelope()
    {
        var handler = TestHandler.RespondJson("""{"status":"ok","uptime":12}""");
        using var client = TestClient.Create(handler);

        var result = await client.Health.LiveAsync();

        Assert.Null(result.Error);
        var health = result.Data!;
        Assert.Equal("ok", health.GetProperty("status").GetString());
        Assert.Equal(12, health.GetProperty("uptime").GetInt32());
        Assert.Equal("https://api.test.local/v1/health/live", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Empty_204_response_yields_null_data()
    {
        var handler = TestHandler.Respond(TestHandler.Response(null, HttpStatusCode.NoContent));
        using var client = TestClient.Create(handler);

        var result = await client.Webhooks.RemoveAsync("wh_1");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task Sends_X_API_Key_and_never_authorization()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"cmp_1"}}""");
        using var client = TestClient.Create(handler);

        await client.Companies.GetAsync("cmp_1");

        var headers = handler.Requests[0].Headers;
        Assert.Equal(TestClient.ApiKey, headers["X-API-Key"]);
        Assert.Equal("application/json", headers["Accept"]);
        Assert.False(headers.ContainsKey("Authorization"));
    }

    [Fact]
    public async Task Query_parameters_are_encoded_and_nulls_are_skipped()
    {
        var handler = TestHandler.RespondJson("""{"data":[],"pagination":{"limit":10,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        await client.Companies.ListAsync(limit: 10, cursor: "cur/2");

        Assert.Equal("https://api.test.local/v1/companies?limit=10&cursor=cur%2F2", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Request_body_is_camel_case_with_nulls_omitted()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"cmp_1"}}""");
        using var client = TestClient.Create(handler);

        await client.Companies.CreateAsync(new CompanyCreateParams
        {
            LegalName = "Loja Center",
            TaxId = "12345678000119",
            TaxRegime = TaxRegimes.SimplesNacional,
        });

        var request = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.test.local/v1/companies", request.Url);
        Assert.Equal(
            """{"legalName":"Loja Center","taxId":"12345678000119","taxRegime":"Simples Nacional"}""",
            request.Body);
    }

    [Fact]
    public async Task Update_body_preserves_explicit_nulls_and_omits_unset_fields()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"cmp_1"}}""");
        using var client = TestClient.Create(handler);

        await client.Companies.UpdateAsync("cmp_1", new CompanyUpdateParams
        {
            LegalName = "New Name",
            MunicipalReg = null,
            Address = null,
        });

        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal(
            """{"legalName":"New Name","municipalReg":null,"address":null}""",
            handler.Requests[0].Body);
    }

    [Fact]
    public async Task Idempotency_key_header_is_forwarded()
    {
        var handler = TestHandler.RespondJson("""{"data":{"props":{"id":"inv_1"}}}""");
        using var client = TestClient.Create(handler);

        await client.Invoices.SendEmailAsync("inv_1", idempotencyKey: "idem_abc");

        Assert.Equal("idem_abc", handler.Requests[0].Headers["Idempotency-Key"]);
    }

    [Fact]
    public async Task Validation_error_maps_status_code_details_and_request_id()
    {
        var handler = TestHandler.RespondJson(
            """
            {
              "code": "ERROR_VALIDATION",
              "message": ["taxId must be a valid CPF/CNPJ"],
              "details": [{ "field": "customer.taxId", "code": "invalid_tax_id", "message": "Invalid tax id" }],
              "requestId": "req_1"
            }
            """,
            HttpStatusCode.BadRequest);
        using var client = TestClient.Create(handler);

        var result = await client.Companies.GetAsync("cmp_1");

        Assert.True(result.IsError);
        Assert.Null(result.Data);
        var error = result.Error!;
        Assert.Equal(400, error.Status);
        Assert.Equal("ERROR_VALIDATION", error.Code);
        Assert.Equal("taxId must be a valid CPF/CNPJ", error.Message);
        Assert.Equal("req_1", error.RequestId);
        Assert.NotNull(error.Details);
        var details = error.Details!;
        Assert.Single(details);
        Assert.Equal("customer.taxId", details[0].Field);
        Assert.Equal("invalid_tax_id", details[0].Code);
    }

    [Fact]
    public async Task Rate_limit_maps_retry_after_and_request_id_from_headers()
    {
        var handler = TestHandler.Respond(TestHandler.Response(null, (HttpStatusCode)429, response =>
        {
            response.Headers.TryAddWithoutValidation("Retry-After", "30");
            response.Headers.TryAddWithoutValidation("X-Request-Id", "req_429");
        }));
        using var client = TestClient.Create(handler);

        var error = (await client.Companies.GetAsync("cmp_1")).Error!;

        Assert.NotNull(error);
        Assert.Equal(429, error!.Status);
        Assert.Equal("http_429", error.Code);
        Assert.Equal("Request failed with status 429", error.Message);
        Assert.Equal(30, error.RetryAfter);
        Assert.Equal("req_429", error.RequestId);
    }

    [Fact]
    public async Task Retry_after_from_body_wins_over_header()
    {
        var handler = TestHandler.Respond(TestHandler.Response(
            """{"code":"RATE_LIMITED","retryAfter":12}""",
            (HttpStatusCode)429,
            response => response.Headers.TryAddWithoutValidation("Retry-After", "60")));
        using var client = TestClient.Create(handler);

        var error = (await client.Companies.GetAsync("cmp_1")).Error!;

        Assert.NotNull(error);
        Assert.Equal("RATE_LIMITED", error!.Code);
        Assert.Equal(12, error.RetryAfter);
    }

    [Fact]
    public async Task Gone_maps_server_code_with_status_fallback_message()
    {
        var handler = TestHandler.RespondJson("""{"code":"ERROR_COMPANY_GONE"}""", HttpStatusCode.Gone);
        using var client = TestClient.Create(handler);

        var error = (await client.Companies.GetAsync("cmp_1")).Error!;

        Assert.NotNull(error);
        Assert.Equal(410, error!.Status);
        Assert.Equal("ERROR_COMPANY_GONE", error.Code);
        Assert.Equal("Request failed with status 410", error.Message);
    }

    [Fact]
    public async Task Non_json_success_body_maps_to_invalid_json()
    {
        var handler = TestHandler.Respond(TestHandler.Response("not-json"));
        using var client = TestClient.Create(handler);

        var error = (await client.Companies.GetAsync("cmp_1")).Error!;

        Assert.NotNull(error);
        Assert.Equal("invalid_json", error!.Code);
        Assert.Equal(200, error.Status);
        Assert.Equal("Invalid JSON in response", error.Message);
    }

    [Fact]
    public async Task Network_failure_returns_network_error_result()
    {
        var handler = TestHandler.Fail(new HttpRequestException("connection refused"));
        using var client = TestClient.Create(handler);

        var error = (await client.Companies.GetAsync("cmp_1")).Error!;

        Assert.NotNull(error);
        Assert.Equal(0, error!.Status);
        Assert.Equal("network_error", error.Code);
        Assert.True(error.IsNetworkError);
        Assert.StartsWith("Network request failed:", error.Message);
    }

    [Fact]
    public async Task Timeout_returns_timeout_result_not_an_exception()
    {
        var handler = TestHandler.Delay(TimeSpan.FromSeconds(30));
        using var client = TestClient.Create(handler, timeout: TimeSpan.FromMilliseconds(100));

        var error = (await client.Companies.GetAsync("cmp_1")).Error!;

        Assert.NotNull(error);
        Assert.Equal(0, error!.Status);
        Assert.Equal("timeout", error.Code);
        Assert.True(error.IsTimeout);
    }

    [Fact]
    public async Task List_maps_pagination_metadata()
    {
        var handler = TestHandler.RespondJson(
            """
            {
              "data": [{"id":"cmp_1"},{"id":"cmp_2"}],
              "pagination": {"limit":2,"hasMore":true,"nextCursor":"cur2"}
            }
            """);
        using var client = TestClient.Create(handler);

        var result = await client.Companies.ListAsync(limit: 2);

        Assert.Null(result.Error);
        var page = result.Data!;
        Assert.Equal(2, page.Data.Count);
        Assert.Equal("cmp_1", page.Data[0].Id);
        Assert.Equal(2, page.Pagination.Limit);
        Assert.True(page.Pagination.HasMore);
        Assert.Equal("cur2", page.Pagination.NextCursor);
    }

    [Fact]
    public async Task List_maps_props_wrapped_items()
    {
        var handler = TestHandler.RespondJson(
            """
            {
              "data": [{"props":{"id":"inv_1"}},{"props":{"id":"inv_2"}}],
              "pagination": {"limit":2,"hasMore":false,"nextCursor":null}
            }
            """);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.ListAsync(new InvoiceListParams { Limit = 2 });

        Assert.Null(result.Error);
        var page = result.Data!;
        Assert.Equal(new[] { "inv_1", "inv_2" }, page.Data.Select(i => i.Id));
        Assert.False(page.Pagination.HasMore);
    }

    [Fact]
    public async Task Iterate_walks_every_page_using_the_cursor()
    {
        var handler = TestHandler.Sequence(
            """
            {"data":[{"id":"cmp_1"},{"id":"cmp_2"}],"pagination":{"limit":2,"hasMore":true,"nextCursor":"cur2"}}
            """,
            """
            {"data":[{"id":"cmp_3"}],"pagination":{"limit":2,"hasMore":false,"nextCursor":null}}
            """);
        using var client = TestClient.Create(handler);

        var items = new List<string>();
        await foreach (var page in client.Companies.IterateAsync(limit: 2))
        {
            Assert.Null(page.Error);
            items.Add(page.Data!.Id);
        }

        Assert.Equal(new[] { "cmp_1", "cmp_2", "cmp_3" }, items);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("cursor=cur2", handler.Requests[1].Url);
    }

    [Fact]
    public async Task Iterate_surfaces_first_page_failure_as_an_error_result()
    {
        var handler = TestHandler.RespondJson("""{"code":"ERROR_INTERNAL"}""", HttpStatusCode.InternalServerError);
        using var client = TestClient.Create(handler);

        var pages = new List<Result<Company>>();
        await foreach (var page in client.Companies.IterateAsync())
        {
            pages.Add(page);
        }

        var failure = Assert.Single(pages);
        Assert.True(failure.IsError);
        var error = failure.Error!;
        Assert.Equal(500, error.Status);
        Assert.Equal("ERROR_INTERNAL", error.Code);
    }

    [Fact]
    public async Task Match_visits_exactly_one_branch()
    {
        var ok = TestHandler.RespondJson("""{"data":{"id":"cmp_1"}}""");
        using var client = TestClient.Create(ok);
        var success = await client.Companies.GetAsync("cmp_1");
        Assert.Equal("cmp_1", success.Match(data => data!.Id, error => error.Code));

        var bad = TestHandler.RespondJson("""{"code":"ERROR_NOT_FOUND"}""", HttpStatusCode.NotFound);
        using var failing = TestClient.Create(bad);
        var failure = await failing.Companies.GetAsync("cmp_missing");
        Assert.Equal("ERROR_NOT_FOUND", failure.Match(data => data!.Id, error => error.Code));
    }

    [Fact]
    public async Task Caller_cancellation_is_not_swallowed_as_a_timeout()
    {
        var handler = TestHandler.Delay(TimeSpan.FromSeconds(30));
        using var client = TestClient.Create(handler);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.Companies.GetAsync("cmp_1", cts.Token));
    }
}

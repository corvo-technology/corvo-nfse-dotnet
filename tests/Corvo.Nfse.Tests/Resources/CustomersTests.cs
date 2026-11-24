using System.Net;

namespace Corvo.Nfse.Tests.Resources;

public class CustomersTests
{
    private const string Customer =
        """{"taxId":"52998224725","name":"Maria Souza","email":"maria@example.com","lastUsedAt":"2026-11-01T10:00:00.000Z","createdAt":"2026-10-01T10:00:00.000Z","updatedAt":"2026-11-01T10:00:00.000Z"}""";

    [Fact]
    public async Task Search_sends_the_query_and_parses_the_page()
    {
        var handler = TestHandler.RespondJson($$$"""{"data":[{{{Customer}}}],"pagination":{"limit":10,"hasMore":true,"nextCursor":"10"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Customers.SearchAsync(search: "Maria", limit: 10, cursor: "0");

        Assert.Null(result.Error);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://api.test.local/v1/customers?search=Maria&limit=10&cursor=0", handler.Requests[0].Url);
        Assert.Equal("corvo_test_" + new string('a', 40), handler.Requests[0].Headers["X-API-Key"]);
        var page = result.Data!;
        Assert.True(page.Pagination.HasMore);
        Assert.Equal("10", page.Pagination.NextCursor);
        Assert.Equal("Maria Souza", page.Data[0].Name);
        Assert.Equal("maria@example.com", page.Data[0].Email);
    }

    [Fact]
    public async Task Iterate_passes_the_cursor_back_untouched()
    {
        var handler = TestHandler.Sequence(
            $$$"""{"data":[{{{Customer}}}],"pagination":{"limit":1,"hasMore":true,"nextCursor":"1"}}""",
            """{"data":[],"pagination":{"limit":1,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        var count = 0;
        await foreach (var item in client.Customers.IterateAsync(limit: 1))
        {
            Assert.Null(item.Error);
            count++;
        }

        Assert.Equal(1, count);
        Assert.EndsWith("/customers?limit=1&cursor=1", handler.Requests[1].Url);
    }

    [Fact]
    public async Task Get_escapes_the_tax_id()
    {
        var handler = TestHandler.RespondJson($$$"""{"data":{{{Customer}}}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Customers.GetAsync("529.982.247-25");

        Assert.Null(result.Error);
        Assert.Equal("52998224725", result.Data!.TaxId);
        Assert.EndsWith("/customers/529.982.247-25", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Update_sends_only_the_set_fields_and_an_explicit_null_email()
    {
        var handler = TestHandler.Sequence($$$"""{"data":{{{Customer}}}}""", $$$"""{"data":{{{Customer}}}}""");
        using var client = TestClient.Create(handler);

        await client.Customers.UpdateAsync("52998224725", new CustomerUpdateParams { Name = "Maria S." });
        await client.Customers.UpdateAsync("52998224725", new CustomerUpdateParams { Email = null });

        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal("""{"name":"Maria S."}""", handler.Requests[0].Body);
        Assert.Equal("""{"email":null}""", handler.Requests[1].Body);
    }

    [Fact]
    public async Task Delete_accepts_a_null_data_body()
    {
        var handler = TestHandler.RespondJson("""{"data":null}""");
        using var client = TestClient.Create(handler);

        var result = await client.Customers.DeleteAsync("52998224725");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.EndsWith("/customers/52998224725", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Delete_maps_an_error_with_its_code_and_request_id()
    {
        var handler = TestHandler.RespondJson(
            """{"statusCode":404,"code":"ERROR_CUSTOMER_NOT_FOUND","message":"not found","requestId":"req_1"}""",
            HttpStatusCode.NotFound);
        using var client = TestClient.Create(handler);

        var result = await client.Customers.DeleteAsync("52998224725");

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.Error!.Status);
        Assert.Equal("ERROR_CUSTOMER_NOT_FOUND", result.Error.Code);
        Assert.Equal("req_1", result.Error.RequestId);
    }

    [Fact]
    public async Task Cnpj_lookup_parses_the_profile()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"legalName":"ACME LTDA","tradeName":"Acme","situation":"ATIVA","active":true,"mei":false,"simples":true,"municipalityCode":"3550308","address":{"street":"Rua A","number":"10","district":"Centro","city":"Sao Paulo","state":"SP","zip":"01001000"}}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Cnpj.LookupAsync("11222333000181");

        Assert.Null(result.Error);
        Assert.EndsWith("/v1/cnpj/11222333000181", handler.Requests[0].Url);
        var profile = result.Data!;
        Assert.Equal("ACME LTDA", profile.LegalName);
        Assert.True(profile.Active);
        Assert.False(profile.Mei);
        Assert.True(profile.Simples);
        Assert.Equal("SP", profile.Address.State);
        Assert.Null(profile.Address.Complement);
    }

    [Fact]
    public async Task Cnpj_lookup_error_keeps_codes_without_the_error_prefix()
    {
        var handler = TestHandler.Respond(TestHandler.Response(
            """{"statusCode":503,"code":"CNPJ_LOOKUP_UNAVAILABLE","message":"try later","requestId":"r"}""",
            HttpStatusCode.ServiceUnavailable,
            response => response.Headers.TryAddWithoutValidation("Retry-After", "30")));
        using var client = TestClient.Create(handler);

        var result = await client.Cnpj.LookupAsync("11222333000181");

        Assert.Equal("CNPJ_LOOKUP_UNAVAILABLE", result.Error!.Code);
        Assert.Equal(30, result.Error.RetryAfter);
    }

    [Fact]
    public async Task Favorites_list_create_and_delete()
    {
        const string favorite =
            """{"id":"fav_1","tenantId":"t","companyId":"cmp_1","label":"Consultoria","serviceCode":"010101","description":"Consultoria","amountCents":15000,"municipalityCode":null,"createdAt":"2026-11-01T00:00:00.000Z"}""";
        var handler = TestHandler.Sequence(
            $$$"""{"data":[{{{favorite}}}]}""",
            $$$"""{"data":{{{favorite}}}}""",
            """{"data":{"deleted":true}}""");
        using var client = TestClient.Create(handler);

        var list = await client.ServiceFavorites.ListAsync("cmp_1");
        Assert.Null(list.Error);
        Assert.Equal("fav_1", list.Data![0].Id);
        Assert.Equal(15000, list.Data[0].AmountCents);
        Assert.Null(list.Data[0].MunicipalityCode);
        Assert.EndsWith("/companies/cmp_1/service-favorites", handler.Requests[0].Url);

        var created = await client.ServiceFavorites.CreateAsync("cmp_1", new ServiceFavoriteCreateParams
        {
            Label = "Consultoria",
            ServiceCode = "010101",
            Description = "Consultoria",
            AmountCents = 15000,
        });
        Assert.Null(created.Error);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal(
            """{"label":"Consultoria","serviceCode":"010101","description":"Consultoria","amountCents":15000}""",
            handler.Requests[1].Body);

        var deleted = await client.ServiceFavorites.DeleteAsync("cmp_1", "fav_1");
        Assert.True(deleted.Data);
        Assert.Equal(HttpMethod.Delete, handler.Requests[2].Method);
        Assert.EndsWith("/companies/cmp_1/service-favorites/fav_1", handler.Requests[2].Url);
    }
}

using System.Net;

namespace Corvo.Nfse.Tests.Resources;

public class CompaniesTests
{
    [Fact]
    public async Task Create_posts_to_the_companies_path()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"cmp_1"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Companies.CreateAsync(new CompanyCreateParams
        {
            LegalName = "Loja Center",
            TaxId = "12345678000119",
            TaxRegime = TaxRegimes.SimplesNacional,
            MunicipalityCode = "3550308",
            Address = new CompanyAddressInput
            {
                Street = "Rua A",
                Number = "100",
                District = "Centro",
                City = "São Paulo",
                State = "SP",
                Zip = "01001000",
            },
        });

        Assert.Null(result.Error);
        Assert.Equal("cmp_1", result.Data!.Id);
        Assert.Contains("\"street\":\"Rua A\"", handler.Requests[0].Body);
        Assert.DoesNotContain("null", handler.Requests[0].Body);
    }

    [Fact]
    public async Task Remove_returns_true_on_success_and_false_on_error()
    {
        var ok = TestHandler.Respond(TestHandler.Response(null, HttpStatusCode.NoContent));
        using var successClient = TestClient.Create(ok);
        var success = await successClient.Companies.RemoveAsync("cmp_1");
        Assert.True(success.IsSuccess);
        Assert.True(success.Data);

        var bad = TestHandler.RespondJson("""{"code":"ERROR_NOT_FOUND"}""", HttpStatusCode.NotFound);
        using var failureClient = TestClient.Create(bad);
        var failure = await failureClient.Companies.RemoveAsync("cmp_missing");
        Assert.True(failure.IsError);
        Assert.False(failure.Data);
        var error = failure.Error!;
        Assert.Equal(404, error.Status);
        Assert.Equal("ERROR_NOT_FOUND", error.Code);
    }

    [Fact]
    public async Task Get_encodes_the_id()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"cmp/1"}}""");
        using var client = TestClient.Create(handler);

        await client.Companies.GetAsync("cmp/1");

        Assert.Equal("https://api.test.local/v1/companies/cmp%2F1", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Create_and_update_send_the_simples_rates()
    {
        var createHandler = TestHandler.RespondJson("""{"data":{"id":"cmp_1"}}""");
        using var createClient = TestClient.Create(createHandler);
        await createClient.Companies.CreateAsync(new CompanyCreateParams
        {
            LegalName = "Loja Center",
            TaxId = "12345678000119",
            TaxRegime = TaxRegimes.SimplesNacional,
            MunicipalityCode = "3550308",
            SimplesRate = "6.54",
            SimplesIssRate = "3.00",
        });

        var updateHandler = TestHandler.RespondJson("""{"data":{"id":"cmp_1"}}""");
        using var updateClient = TestClient.Create(updateHandler);
        await updateClient.Companies.UpdateAsync("cmp_1", new CompanyUpdateParams
        {
            SimplesRate = "7.00",
            SimplesIssRate = null,
        });

        Assert.Contains("\"simplesRate\":\"6.54\"", createHandler.Requests[0].Body);
        Assert.Contains("\"simplesIssRate\":\"3.00\"", createHandler.Requests[0].Body);
        Assert.Equal("""{"simplesRate":"7.00","simplesIssRate":null}""", updateHandler.Requests[0].Body);
    }
}

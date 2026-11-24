namespace Corvo.Nfse.Tests;

public sealed class CalculatePlansTests
{
    [Fact]
    public async Task Calculate_returns_the_estimate_and_omits_a_missing_customer()
    {
        var handler = TestHandler.RespondJson(
            """
            {"data":{"preEmission":true,"status":"ready",
              "taxes":{"iss":{"base":"15000.00","rate":"3","amount":"450.00"},
                       "ibsTotal":{"base":"14550.00","amount":"14.55"}},
              "netReceivableAmount":"15000.00","blocking":null}}
            """);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CalculateAsync(new InvoiceCreateParams
        {
            CompanyTaxId = "11222333000181",
            Service = new InvoiceServiceInput(),
            Amount = 15000m,
            CompetenceDate = "2026-10-16",
        });

        Assert.Null(result.Error);
        var estimate = result.Data!;
        Assert.Equal("ready", estimate.Status);
        Assert.Equal("450.00", estimate.Taxes.Iss!.Amount);
        Assert.Null(estimate.Taxes.IbsTotal!.Rate);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://api.test.local/v1/invoices/calculate", handler.Requests[0].Url);
        Assert.DoesNotContain("customer", handler.Requests[0].Body!);
    }

    [Fact]
    public async Task Calculate_accepts_null_classification_applicability_and_calculation()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"status":"blocked","classification":null,"applicability":null,"calculation":null}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CalculateAsync(new InvoiceCreateParams
        {
            Service = new InvoiceServiceInput(),
            Amount = 10m,
            CompetenceDate = "2026-10-16",
        });

        Assert.Null(result.Error);
        Assert.Equal("blocked", result.Data!.Status);
        Assert.Null(result.Data!.Classification);
        Assert.Null(result.Data.Applicability);
        Assert.Null(result.Data.Calculation);
    }

    [Fact]
    public async Task Calculate_blocked_is_data_not_an_error()
    {
        var handler = TestHandler.RespondJson(
            """
            {"data":{"status":"blocked",
              "blocking":{"code":"TAX_RATE_NOT_FOUND","message":"x","step":"s"},
              "pendingFields":[{"field":"iss.rate","reason":"missing"}]}}
            """);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CalculateAsync(new InvoiceCreateParams
        {
            Service = new InvoiceServiceInput(),
            Amount = 10m,
            CompetenceDate = "2026-10-16",
        });

        Assert.Null(result.Error);
        Assert.Equal("TAX_RATE_NOT_FOUND", result.Data!.Blocking!.Code);
        Assert.Single(result.Data.PendingFields);
    }

    [Fact]
    public async Task Plans_catalog_list_get_and_quote()
    {
        var handler = TestHandler.Sequence(
            """{"data":[{"slug":"solo","name":"Solo","monthlyAllowance":200,"basePriceCents":2990,"monthlyEquivalentCents":2990,"chargeTotalCents":2990,"currency":"BRL"}],"pagination":{"limit":5,"hasMore":false,"nextCursor":null}}""",
            """{"data":{"slug":"equipe","name":"Equipe","monthlyAllowance":1000,"basePriceCents":8990,"monthlyEquivalentCents":7490,"chargeTotalCents":89880,"currency":"BRL"}}""",
            """{"data":{"recommended_plan":"volume","total_estimated_cents":179990}}""",
            """{"data":null}""");
        using var client = TestClient.Create(handler);

        var page = await client.Plans.ListAsync(limit: 5);
        Assert.Equal("solo", page.Data!.Data[0].Slug);
        Assert.Equal(2990, page.Data.Data[0].ChargeTotalCents);

        var plan = await client.Plans.GetAsync("equipe", cycle: "annual");
        Assert.Equal(7490, plan.Data!.MonthlyEquivalentCents);
        Assert.Equal(89880, plan.Data.ChargeTotalCents);

        var quote = await client.Plans.QuoteAsync(20000);
        Assert.Equal("volume", quote.Data!.RecommendedPlan);
        Assert.Equal(179990, quote.Data.TotalEstimatedCents);

        var none = await client.Plans.QuoteAsync(0);
        Assert.Null(none.Error);
        Assert.Null(none.Data);

        Assert.Equal("https://api.test.local/v1/plans?limit=5", handler.Requests[0].Url);
        Assert.Equal("https://api.test.local/v1/plans/equipe?cycle=annual", handler.Requests[1].Url);
        Assert.Equal("https://api.test.local/v1/plans/quote?volume=20000", handler.Requests[2].Url);
    }
}

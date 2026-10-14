using System.Net;

namespace Corvo.Nfse.Tests.Resources;

public class CreditsBillingHealthTests
{
    [Fact]
    public async Task Credits_balance_parses()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"tenantId":"ten_1","balance":120.5,"overageUnits":10,"overageAmountCents":900,"hardLimit":null}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Credits.BalanceAsync();

        Assert.Null(result.Error);
        var balance = result.Data!;
        Assert.Equal("ten_1", balance.TenantId);
        Assert.Equal(120.5m, balance.Balance);
        Assert.Null(balance.HardLimit);
        Assert.Equal("https://api.test.local/v1/credits", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Credits_plans_use_the_props_envelope()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"props":{"id":"plan_1","slug":"starter","name":"Starter","creditUnitPriceCents":100}}],"pagination":{"limit":20,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Credits.PlansAsync();

        Assert.Null(result.Error);
        var plan = result.Data!.Data[0];
        Assert.Equal("plan_1", plan.Id);
        Assert.Equal(100, plan.CreditUnitPriceCents);
    }

    [Fact]
    public async Task Credit_history_iterates_across_pages()
    {
        var handler = TestHandler.Sequence(
            """{"data":[{"props":{"id":"txn_1","type":"purchase"}},{"props":{"id":"txn_2","type":"consume"}}],"pagination":{"limit":2,"hasMore":true,"nextCursor":"c2"}}""",
            """{"data":[{"props":{"id":"txn_3","type":"consume"}}],"pagination":{"limit":2,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        var ids = new List<string>();
        await foreach (var page in client.Credits.IterateHistoryAsync(limit: 2))
        {
            Assert.Null(page.Error);
            ids.Add(page.Data!.Id);
        }

        Assert.Equal(new[] { "txn_1", "txn_2", "txn_3" }, ids);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Billing_plan_maps_the_snake_case_fields()
    {
        var handler = TestHandler.RespondJson(
            """
            {
              "data": {
                "plan": {"slug":"scale","name":"Scale","monthly_allowance":5000,"daily_limit":null,"max_active_companies":10,"monthly_price_cents":49900},
                "subscription": {"status":"active","paid_until":"2026-02-01","cancel_at_period_end":false,"scheduled_plan":null,"blocked":false},
                "usage": {"overage_units":3,"overage_amount_cents":30,"hard_limit":100,"credits_used":497,"credits_reserved":0,"credits_remaining":5003,"monthly_allowance":5000,"daily_usage":12,"daily_limit":200,"active_companies":2,"max_active_companies":10,"period_start":"2026-01-01","period_end":"2026-02-01"}
              }
            }
            """);
        using var client = TestClient.Create(handler);

        var result = await client.Billing.PlanAsync();

        Assert.Null(result.Error);
        var plan = result.Data!;
        Assert.Equal("scale", plan.Plan.Slug);
        Assert.Equal(5000, plan.Plan.MonthlyAllowance);
        Assert.Null(plan.Plan.DailyLimit);
        Assert.Equal(49900, plan.Plan.MonthlyPriceCents);
        Assert.Equal("active", plan.Subscription.Status);
        Assert.False(plan.Subscription.CancelAtPeriodEnd);
        Assert.Equal(5003, plan.Usage.CreditsRemaining);
        Assert.Equal(100, plan.Usage.HardLimit);
        Assert.Equal("2026-01-01", plan.Usage.PeriodStart);
        Assert.Equal("https://api.test.local/v1/billing/plan", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Billing_usage_maps_the_camel_case_quota()
    {
        var handler = TestHandler.RespondJson(
            """
            {"data":{"canEmit":true,"creditsRemaining":42,"creditsUsed":8,"creditsReserved":0,"monthlyAllowance":50,"dailyUsage":3,"dailyLimit":10,"activeCompanies":1,"maxActiveCompanies":5,"periodStart":"2026-01-01","periodEnd":"2026-02-01"}}
            """);
        using var client = TestClient.Create(handler);

        var result = await client.Billing.UsageAsync();

        Assert.Null(result.Error);
        var quota = result.Data!;
        Assert.True(quota.CanEmit);
        Assert.Equal(42, quota.CreditsRemaining);
        Assert.Equal(10, quota.DailyLimit);
        Assert.Equal("https://api.test.local/v1/billing/usage", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Health_ready_returns_raw_json()
    {
        var handler = TestHandler.RespondJson("""{"status":"ready"}""");
        using var client = TestClient.Create(handler);

        var result = await client.Health.ReadyAsync();

        Assert.Null(result.Error);
        Assert.Equal("ready", result.Data!.GetProperty("status").GetString());
        Assert.EndsWith("/v1/health/ready", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Health_failures_still_come_back_as_error_results()
    {
        var handler = TestHandler.RespondJson("""{"status":"degraded"}""", HttpStatusCode.ServiceUnavailable);
        using var client = TestClient.Create(handler);

        var result = await client.Health.LiveAsync();

        Assert.True(result.IsError);
        var error = result.Error!;
        Assert.Equal(503, error.Status);
        Assert.Equal("http_503", error.Code);
        Assert.Equal("Request failed with status 503", error.Message);
    }
}

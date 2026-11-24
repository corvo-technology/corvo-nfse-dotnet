using System.Text.Json.Nodes;

namespace Corvo.Nfse.Tests.Resources;

public class SchedulesTests
{
    private const string Schedule =
        """{"id":"sch_1","kind":"recurring","frequency":"MONTHLY","status":"active","pause_reason":null,"company_id":"cmp_1","anchor_on":"2026-12-01","next_run_on":"2026-12-01","ends_on":null,"attempts":0,"last_run_on":null,"invoice":{"amount":100,"service":{"description":"x"}},"created_at":"2026-11-24T00:00:00.000Z"}""";

    [Fact]
    public async Task Create_with_schedule_type_returns_the_schedule_not_an_invoice()
    {
        var handler = TestHandler.RespondJson($$$"""{"data":{{{Schedule}}}}""", System.Net.HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CreateAsync(
            new InvoiceCreateParams
            {
                CompanyId = "cmp_1",
                Service = new InvoiceServiceInput { Description = "x" },
                Amount = 100m,
                CompetenceDate = "2026-12-01",
                Options = new InvoiceOptionsInput
                {
                    Type = InvoiceTypes.Recurring,
                    Recurrence = new InvoiceRecurrence { Frequency = ScheduleFrequencies.Monthly, EndsOn = "2027-06-30" },
                },
            },
            idempotencyKey: "k1");

        Assert.Null(result.Error);
        Assert.True(result.Data!.IsSchedule);
        Assert.Null(result.Data.Invoice);
        var schedule = result.Data.Schedule!;
        Assert.Equal("sch_1", schedule.Id);
        Assert.Equal("recurring", schedule.Kind);
        Assert.Equal("MONTHLY", schedule.Frequency);
        Assert.Equal("cmp_1", schedule.CompanyId);
        Assert.Equal("2026-12-01", schedule.AnchorOn);
        Assert.Equal("k1", handler.Requests[0].Headers["Idempotency-Key"]);
        Assert.Equal(
            """{"companyId":"cmp_1","service":{"description":"x"},"amount":100,"competenceDate":"2026-12-01","options":{"type":"RECURRING","recurrence":{"frequency":"MONTHLY","endsOn":"2027-06-30"}}}""",
            handler.Requests[0].Body);
    }

    [Fact]
    public async Task List_sends_filters_and_parses_snake_case_fields()
    {
        var handler = TestHandler.RespondJson($$$"""{"data":[{{{Schedule}}}],"pagination":{"limit":50,"hasMore":true,"nextCursor":"50"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Schedules.ListAsync(new ScheduleListParams { Status = ScheduleStatuses.Active, CompanyId = "cmp_1", Limit = 50 });

        Assert.Null(result.Error);
        Assert.Equal("https://api.test.local/v1/schedules?status=active&companyId=cmp_1&limit=50", handler.Requests[0].Url);
        Assert.Equal("50", result.Data!.Pagination.NextCursor);
        var item = result.Data.Data[0];
        Assert.Equal("2026-12-01", item.NextRunOn);
        Assert.Null(item.PauseReason);
        Assert.Equal(100, item.Invoice.GetProperty("amount").GetInt32());
    }

    [Fact]
    public async Task Get_pause_resume_cancel_use_the_expected_routes()
    {
        var handler = TestHandler.Sequence($$$"""{"data":{{{Schedule}}}}""");
        using var client = TestClient.Create(handler);

        await client.Schedules.GetAsync("sch_1");
        await client.Schedules.PauseAsync("sch_1");
        await client.Schedules.ResumeAsync("sch_1");
        await client.Schedules.CancelAsync("sch_1");

        Assert.Equal(
            new[] { "GET /v1/schedules/sch_1", "POST /v1/schedules/sch_1/pause", "POST /v1/schedules/sch_1/resume", "POST /v1/schedules/sch_1/cancel" },
            handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}").ToArray());
    }

    [Fact]
    public async Task Update_sends_camel_case_fields_and_an_explicit_null_ends_on()
    {
        var handler = TestHandler.RespondJson($$$"""{"data":{{{Schedule}}}}""");
        using var client = TestClient.Create(handler);

        await client.Schedules.UpdateAsync("sch_1", new ScheduleUpdateParams
        {
            AnchorOn = "2026-12-15",
            EndsOn = null,
            Frequency = ScheduleFrequencies.Weekly,
            Invoice = new JsonObject { ["amount"] = 250 },
        });

        Assert.Equal(HttpMethod.Patch, handler.Requests[0].Method);
        Assert.Equal("""{"anchorOn":"2026-12-15","endsOn":null,"frequency":"WEEKLY","invoice":{"amount":250}}""", handler.Requests[0].Body);
    }

    [Fact]
    public async Task ListRuns_parses_the_run_log()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"id":"run_1","occurrence_on":"2026-12-05","ran_on":"2026-12-07","attempt":1,"outcome":"issued","invoice_id":"inv_1","error_code":null,"error_message":null,"created_at":"2026-12-07T12:00:00.000Z"}],"pagination":{"limit":20,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Schedules.ListRunsAsync("sch_1", limit: 20);

        Assert.Equal("https://api.test.local/v1/schedules/sch_1/runs?limit=20", handler.Requests[0].Url);
        var run = result.Data!.Data[0];
        Assert.Equal("issued", run.Outcome);
        Assert.Equal("2026-12-05", run.OccurrenceOn);
        Assert.Equal("2026-12-07", run.RanOn);
        Assert.Equal("inv_1", run.InvoiceId);
    }
}

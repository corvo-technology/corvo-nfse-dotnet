using System.Net;

namespace Corvo.Nfse.Tests.Resources;

public class InvoicesV3Tests
{
    private const string InvoiceJson =
        """{"id":"inv_2","companyId":"cmp_1","tenantId":"t","environment":"test","status":"queued","documentsStatus":"pending","emailStatus":null,"stage":"pending","amount":100,"currency":"BRL","competenceDate":"2026-11-24T00:00:00.000Z","documentsReadyAt":null,"lastEmailStatus":null,"lastEmailSentAt":null,"substitutesInvoiceId":"inv_1","substitutionDescription":"typo","portalFlag":"taker_rejected","customerSnapshot":{"name":"X","foreign":{"country":"PT"}},"serviceSnapshot":{"code":"010101","description":"d","municipalityCode":"3550308","purchaseOrder":"PO-1","notes":"n","fiscal":{"cst":"000"}},"taxSnapshot":{},"createdAt":"x","updatedAt":"x"}""";

    [Fact]
    public async Task Create_immediate_still_returns_an_invoice_with_the_new_fields()
    {
        var handler = TestHandler.RespondJson($$$"""{"data":{{{InvoiceJson}}}}""", HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CreateAsync(new InvoiceCreateParams
        {
            CompanyId = "cmp_1",
            Customer = new InvoiceCustomerInput
            {
                Name = "Foreign Co",
                Foreign = new InvoiceForeignCustomer
                {
                    NoTaxIdReason = ForeignNoTaxIdReasons.NotRequired,
                    Country = "PT",
                    PostalCode = "1000-001",
                    City = "Lisboa",
                    Region = "Lisboa",
                    Street = "Rua A",
                    Number = "1",
                    District = "Centro",
                },
            },
            Service = new InvoiceServiceInput { Description = "d" },
            Amount = 100m,
            CompetenceDate = "2026-11-24",
            PurchaseOrder = "PO-1",
            Notes = "n",
        });

        Assert.Null(result.Error);
        Assert.False(result.Data!.IsSchedule);
        var invoice = result.Data.Invoice!;
        Assert.Equal("inv_1", invoice.SubstitutesInvoiceId);
        Assert.Equal("typo", invoice.SubstitutionDescription);
        Assert.Equal(InvoicePortalFlags.TakerRejected, invoice.PortalFlag);
        Assert.Equal("pending", invoice.DocumentsStatus);
        Assert.Equal("PO-1", invoice.ServiceSnapshot.PurchaseOrder);
        Assert.Equal("n", invoice.ServiceSnapshot.Notes);
        Assert.Equal("000", invoice.ServiceSnapshot.Fiscal!["cst"]);
        Assert.Equal("PT", invoice.CustomerSnapshot.Foreign!.Value.GetProperty("country").GetString());
        Assert.Equal(
            """{"companyId":"cmp_1","customer":{"name":"Foreign Co","foreign":{"noTaxIdReason":"not_required","country":"PT","postalCode":"1000-001","city":"Lisboa","region":"Lisboa","street":"Rua A","number":"1","district":"Centro"}},"service":{"description":"d"},"amount":100,"competenceDate":"2026-11-24","purchaseOrder":"PO-1","notes":"n"}""",
            handler.Requests[0].Body);
    }

    [Fact]
    public async Task List_filters_by_customer_tax_id()
    {
        var handler = TestHandler.RespondJson("""{"data":[],"pagination":{"limit":50,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        await client.Invoices.ListAsync(new InvoiceListParams { CustomerTaxId = "52998224725" });

        Assert.Equal("https://api.test.local/v1/invoices?customerTaxId=52998224725", handler.Requests[0].Url);
    }

    [Fact]
    public async Task SendEmail_without_an_address_sends_no_body()
    {
        var handler = TestHandler.RespondJson("""{"data":{"emailId":"eml_1","status":"queued"}}""", HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.SendEmailAsync("inv_1");

        Assert.Null(result.Error);
        Assert.True(string.IsNullOrEmpty(handler.Requests[0].Body));
    }

    [Fact]
    public async Task SendEmail_can_target_another_address()
    {
        var handler = TestHandler.RespondJson("""{"data":{"emailId":"eml_1","status":"queued"}}""", HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.SendEmailAsync("inv_1", idempotencyKey: "k", email: "outro@example.com");

        Assert.Null(result.Error);
        Assert.Equal("""{"email":"outro@example.com"}""", handler.Requests[0].Body);
        Assert.Equal("k", handler.Requests[0].Headers["Idempotency-Key"]);
    }

    [Fact]
    public async Task Substitute_posts_the_changes_with_an_idempotency_key()
    {
        var handler = TestHandler.RespondJson($$$"""{"data":{{{InvoiceJson}}}}""", HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.SubstituteAsync(
            "inv_1",
            new InvoiceSubstituteParams
            {
                Description = "Corrigir descricao do servico",
                Service = new InvoiceSubstituteService { Description = "novo" },
                Notes = "n",
            },
            idempotencyKey: "sub-1");

        Assert.Null(result.Error);
        Assert.Equal("inv_1", result.Data!.SubstitutesInvoiceId);
        var request = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/v1/invoices/inv_1/substitute", request.Url);
        Assert.Equal("sub-1", request.Headers["Idempotency-Key"]);
        Assert.Equal(
            """{"description":"Corrigir descricao do servico","notes":"n","service":{"description":"novo"}}""",
            request.Body);
    }

    [Fact]
    public async Task Substitute_error_keeps_the_code()
    {
        var handler = TestHandler.RespondJson(
            """{"statusCode":409,"code":"ERROR_INVOICE_NOT_SUBSTITUTABLE","message":"no","requestId":"r"}""",
            HttpStatusCode.Conflict);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.SubstituteAsync("inv_1", new InvoiceSubstituteParams { Description = "x" });

        Assert.Equal("ERROR_INVOICE_NOT_SUBSTITUTABLE", result.Error!.Code);
    }

    [Fact]
    public async Task Overview_sends_the_period()
    {
        var handler = TestHandler.RespondJson(
            $$$"""{"data":{"counts":{"authorized":5,"processing":1,"rejected":0},"dailyActivity":[{"date":"2026-11-24","count":3}],"period":"30d","from":"2026-10-26","to":"2026-11-24","timezone":"America/Sao_Paulo","recentInvoices":[{{{InvoiceJson}}}]}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.OverviewAsync("30d");

        Assert.Equal("https://api.test.local/v1/invoices/overview?period=30d", handler.Requests[0].Url);
        var overview = result.Data!;
        Assert.Equal(5, overview.Counts.Authorized);
        Assert.Equal(3, overview.DailyActivity[0].Count);
        Assert.Equal("America/Sao_Paulo", overview.Timezone);
        Assert.Equal("inv_2", overview.RecentInvoices[0].Id);
    }

    [Fact]
    public async Task Review_posts_the_action_and_ids_and_parses_skips()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"results":[{"id":"a","outcome":"accepted"},{"id":"b","outcome":"skipped","reason":"not_in_manual_review"}]}}""",
            HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.ReviewAsync(InvoiceReviewAction.Discard, new[] { "a", "b" });

        Assert.Null(result.Error);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.EndsWith("/v1/invoices/review", handler.Requests[0].Url);
        Assert.Equal("""{"action":"discard","invoiceIds":["a","b"]}""", handler.Requests[0].Body);
        Assert.Equal("accepted", result.Data!.Results[0].Outcome);
        Assert.Equal("not_in_manual_review", result.Data.Results[1].Reason);
    }

    [Fact]
    public async Task Events_parse_the_portal_fields()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"id":"e","invoiceId":"i","fromStatus":"authorized","toStatus":"cancelled","reason":null,"source":"portal","portalEvent":"101101","portalNsu":"42","createdAt":"x"}],"pagination":{"limit":50,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.EventsAsync("i");

        var evt = result.Data!.Data[0];
        Assert.Equal("portal", evt.Source);
        Assert.Equal("101101", evt.PortalEvent);
        Assert.Equal("42", evt.PortalNsu);
    }
}

public class CreditsWebhooksV3Tests
{
    [Fact]
    public async Task Credits_preview_purchase_is_a_get_with_units()
    {
        var handler = TestHandler.RespondJson("""{"data":{"units":10,"unitPriceCents":99,"totalCents":990}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Credits.PreviewPurchaseAsync(10);

        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://api.test.local/v1/credits/purchase/preview?units=10", handler.Requests[0].Url);
        Assert.Equal(990, result.Data!.TotalCents);
        Assert.Equal(99, result.Data.UnitPriceCents);
    }

    [Fact]
    public async Task Billing_plan_without_a_plan_is_a_no_plan_assigned_error()
    {
        var handler = TestHandler.RespondJson(
            """{"statusCode":404,"code":"NO_PLAN_ASSIGNED","message":"no plan","requestId":"r"}""",
            HttpStatusCode.NotFound);
        using var client = TestClient.Create(handler);

        var result = await client.Billing.PlanAsync();

        Assert.Equal(BillingErrorCodes.NoPlanAssigned, result.Error!.Code);
        Assert.Equal(404, result.Error.Status);
    }

    [Fact]
    public async Task Billing_plan_parses_a_pix_subscription()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"plan":{"slug":"solo","name":"Solo","monthly_allowance":100,"daily_limit":null,"max_active_companies":1,"monthly_price_cents":4900},"subscription":{"status":"pending","paid_until":null,"cancel_at_period_end":false,"scheduled_plan":null,"blocked":false,"stripe_portal_available":false,"stripe_subscription":false,"pix_subscription":{"brCode":"000201","paymentId":"p1","paymentLinkUrl":null,"planSlug":"solo","status":"pending"}},"usage":{"avulso_balance":0,"hard_limit":0,"credits_used":0,"credits_reserved":0,"credits_remaining":100,"monthly_allowance":100,"daily_usage":0,"daily_limit":null,"active_companies":0,"max_active_companies":1}}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Billing.PlanAsync();

        var pix = result.Data!.Subscription.PixSubscription!;
        Assert.Equal("000201", pix.BrCode);
        Assert.Equal("solo", pix.PlanSlug);
        Assert.Equal("pending", pix.Status);
        Assert.Null(pix.PaymentLinkUrl);
    }

    [Fact]
    public async Task Webhook_test_can_report_a_failed_delivery()
    {
        var handler = TestHandler.RespondJson("""{"data":{"sent":false,"error":"timeout","statusCode":504}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Webhooks.TestAsync("wh_1");

        Assert.False(result.Data!.Sent);
        Assert.Equal("timeout", result.Data.Error);
        Assert.Equal(504, result.Data.StatusCode);
    }

    [Fact]
    public async Task Webhook_create_sends_the_new_event_and_returns_the_environment()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"id":"wh_1","url":"https://example.com/h","events":["nfse.invoice.taker_rejected"],"active":true,"environment":"test","createdAt":"x","updatedAt":"x","secret":"whsec"}}""",
            HttpStatusCode.Created);
        using var client = TestClient.Create(handler);

        var result = await client.Webhooks.CreateAsync(new WebhookCreateParams
        {
            Url = "https://example.com/h",
            Events = new[] { WebhookEvents.InvoiceTakerRejected },
        });

        Assert.Equal("test", result.Data!.Environment);
        Assert.Equal("""{"url":"https://example.com/h","events":["nfse.invoice.taker_rejected"]}""", handler.Requests[0].Body);
    }
}

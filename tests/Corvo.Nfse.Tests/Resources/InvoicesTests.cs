using System.Net;
using System.Net.Http.Headers;

namespace Corvo.Nfse.Tests.Resources;

public class InvoicesTests
{
    private static InvoiceCreateParams NewInvoice() => new()
    {
        CompanyId = "cmp_1",
        Customer = new InvoiceCustomerInput { Name = "Joao Silva", TaxId = "52998224725" },
        Service = new InvoiceServiceInput { Code = "0101", Description = "Services" },
        Amount = 150.5m,
        CompetenceDate = "2026-01-31",
    };

    [Fact]
    public async Task Create_sends_camel_case_body_with_idempotency_key()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"inv_1","status":"queued"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CreateAsync(NewInvoice(), idempotencyKey: "idem_1");

        Assert.Null(result.Error);
        var invoice = result.Data!;
        Assert.Equal("inv_1", invoice.Id);
        Assert.Equal(InvoiceStatuses.Queued, invoice.Status);
        var request = handler.Requests[0];
        Assert.Equal("https://api.test.local/v1/invoices", request.Url);
        Assert.Equal(
            """{"companyId":"cmp_1","customer":{"taxId":"52998224725","name":"Joao Silva"},"service":{"code":"0101","description":"Services"},"amount":150.5,"competenceDate":"2026-01-31"}""",
            request.Body);
        Assert.Equal("idem_1", request.Headers["Idempotency-Key"]);
    }

    [Fact]
    public async Task List_sends_invoice_filters_as_query()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"id":"inv_1"}],"pagination":{"limit":1,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        await client.Invoices.ListAsync(new InvoiceListParams
        {
            CompanyId = "cmp_1",
            Status = InvoiceStatuses.Authorized,
            Limit = 1,
        });

        Assert.Equal(
            "https://api.test.local/v1/invoices?companyId=cmp_1&status=authorized&limit=1",
            handler.Requests[0].Url);
    }

    [Fact]
    public async Task Events_returns_the_items()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"id":"evt_1","invoiceId":"inv_1","toStatus":"authorized"}],"pagination":{"limit":50,"hasMore":false,"nextCursor":null}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.EventsAsync("inv_1");

        Assert.Null(result.Error);
        var evt = result.Data!.Data[0];
        Assert.Equal("evt_1", evt.Id);
        Assert.Equal("authorized", evt.ToStatus);
        Assert.EndsWith("/invoices/inv_1/events", handler.Requests[0].Url);
    }

    [Fact]
    public async Task DownloadUrl_posts_the_format()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"downloadUrl":"https://files.example/x","expiresAt":"2026-01-01T00:00:00Z","fileName":"x.xml","mime":"application/xml"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.DownloadUrlAsync("inv_1", InvoiceDownloadFormat.Xml);

        Assert.Null(result.Error);
        Assert.Equal("https://files.example/x", result.Data!.DownloadUrl);
        Assert.Equal("""{"format":"xml"}""", handler.Requests[0].Body);
    }

    [Fact]
    public async Task Pdf_returns_bytes_with_mime_and_file_name()
    {
        var handler = TestHandler.Respond(TestHandler.Response(null, HttpStatusCode.OK, response =>
        {
            var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
            response.Content = new ByteArrayContent(bytes);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment") { FileName = "\"inv_1.pdf\"" };
        }));
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.PdfAsync("inv_1");

        Assert.Null(result.Error);
        var file = result.Data!;
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }, file.Buffer);
        Assert.Equal("application/pdf", file.MimeType);
        Assert.Equal("inv_1.pdf", file.FileName);
        Assert.EndsWith("/invoices/inv_1/pdf", handler.Requests[0].Url);
    }

    [Fact]
    public async Task SendEmail_posts_with_idempotency_key()
    {
        var handler = TestHandler.RespondJson("""{"data":{"emailId":"eml_1","status":"queued"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.SendEmailAsync("inv_1", idempotencyKey: "idem_2");

        Assert.Null(result.Error);
        Assert.Equal("eml_1", result.Data!.EmailId);
        Assert.Null(handler.Requests[0].Body);
        Assert.Equal("idem_2", handler.Requests[0].Headers["Idempotency-Key"]);
    }

    [Fact]
    public async Task Create_maps_api_errors_to_error_results()
    {
        var handler = TestHandler.RespondJson(
            """{"code":"ERROR_VALIDATION","message":["amount must be positive"],"requestId":"req_9"}""",
            HttpStatusCode.UnprocessableEntity);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CreateAsync(NewInvoice());

        Assert.True(result.IsError);
        var error = result.Error!;
        Assert.Equal(422, error.Status);
        Assert.Equal("ERROR_VALIDATION", error.Code);
        Assert.Equal("amount must be positive", error.Message);
        Assert.Equal("req_9", error.RequestId);
    }

    [Fact]
    public async Task Cancel_posts_reason_and_description_and_returns_the_cancelled_invoice()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"id":"inv_1","status":"cancelled","cancellationReason":"issue_error","cancellationDescription":"Wrong amount on the invoice","cancelledAt":"2026-10-20T12:00:00.000Z","cancellationRequestedAt":null}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CancelAsync("inv_1", new InvoiceCancelParams
        {
            Reason = CancellationReasons.IssueError,
            Description = "Wrong amount on the invoice",
        });

        Assert.Null(result.Error);
        var invoice = result.Data!;
        Assert.Equal(InvoiceStatuses.Cancelled, invoice.Status);
        Assert.Equal(CancellationReasons.IssueError, invoice.CancellationReason);
        Assert.Equal("2026-10-20T12:00:00.000Z", invoice.CancelledAt);
        var request = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.test.local/v1/invoices/inv_1/cancel", request.Url);
        Assert.Equal("""{"reason":"issue_error","description":"Wrong amount on the invoice"}""", request.Body);
    }

    [Fact]
    public async Task Cancel_omits_the_description_when_not_given_and_escapes_the_id()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"inv/1","status":"cancelled"}}""");
        using var client = TestClient.Create(handler);

        await client.Invoices.CancelAsync("inv/1", new InvoiceCancelParams { Reason = CancellationReasons.Other });

        Assert.Equal("https://api.test.local/v1/invoices/inv%2F1/cancel", handler.Requests[0].Url);
        Assert.Equal("""{"reason":"other"}""", handler.Requests[0].Body);
    }

    [Fact]
    public async Task Cancel_returns_a_cancelling_invoice_on_202()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"id":"inv_1","status":"cancelling","cancellationReason":"service_not_provided","cancellationDescription":"Service was never delivered","cancelledAt":null,"cancellationRequestedAt":"2026-10-20T12:00:00.000Z"}}""",
            HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CancelAsync("inv_1", new InvoiceCancelParams
        {
            Reason = CancellationReasons.ServiceNotProvided,
            Description = "Service was never delivered",
        });

        Assert.Null(result.Error);
        var invoice = result.Data!;
        Assert.Equal(InvoiceStatuses.Cancelling, invoice.Status);
        Assert.Equal(CancellationReasons.ServiceNotProvided, invoice.CancellationReason);
        Assert.Null(invoice.CancelledAt);
        Assert.Equal("2026-10-20T12:00:00.000Z", invoice.CancellationRequestedAt);
    }

    [Fact]
    public async Task Cancel_surfaces_409_as_an_error_result_with_the_code()
    {
        var handler = TestHandler.RespondJson(
            """{"code":"ERROR_INVOICE_NOT_CANCELLABLE","message":"Invoice cannot be cancelled in status manual_review","requestId":"req_c"}""",
            HttpStatusCode.Conflict);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CancelAsync("inv_1", new InvoiceCancelParams
        {
            Reason = CancellationReasons.Other,
            Description = "Customer asked for it",
        });

        Assert.True(result.IsError);
        Assert.Equal(409, result.Error!.Status);
        Assert.Equal("ERROR_INVOICE_NOT_CANCELLABLE", result.Error.Code);
        Assert.Equal("req_c", result.Error.RequestId);
    }

    [Fact]
    public async Task Cancel_surfaces_422_as_an_error_result()
    {
        var handler = TestHandler.RespondJson(
            """{"code":"ERROR_VALIDATION","message":["description must be at least 15 characters"]}""",
            HttpStatusCode.UnprocessableEntity);
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CancelAsync("inv_1", new InvoiceCancelParams
        {
            Reason = CancellationReasons.Other,
            Description = "too short",
        });

        Assert.True(result.IsError);
        Assert.Equal(422, result.Error!.Status);
        Assert.Equal("ERROR_VALIDATION", result.Error.Code);
    }

    [Theory]
    [InlineData("cancelled", "issue_error", "2026-10-20T12:00:00.000Z", null)]
    [InlineData("cancelling", "other", null, "2026-10-20T11:00:00.000Z")]
    public async Task Get_parses_cancellation_status_and_fields(
        string status, string reason, string? cancelledAt, string? requestedAt)
    {
        static string Json(string? v) => v is null ? "null" : "\"" + v + "\"";
        var handler = TestHandler.RespondJson(
            "{\"data\":{\"id\":\"inv_1\",\"status\":" + Json(status)
            + ",\"cancellationReason\":" + Json(reason)
            + ",\"cancellationDescription\":\"Wrong amount on the invoice\""
            + ",\"cancelledAt\":" + Json(cancelledAt)
            + ",\"cancellationRequestedAt\":" + Json(requestedAt) + "}}");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.GetAsync("inv_1");

        Assert.Null(result.Error);
        var invoice = result.Data!;
        Assert.Equal(status, invoice.Status);
        Assert.Equal(reason, invoice.CancellationReason);
        Assert.Equal("Wrong amount on the invoice", invoice.CancellationDescription);
        Assert.Equal(cancelledAt, invoice.CancelledAt);
        Assert.Equal(requestedAt, invoice.CancellationRequestedAt);
    }

    [Fact]
    public async Task Get_parses_an_invoice_without_cancellation_fields()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"inv_1","status":"authorized"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.GetAsync("inv_1");

        Assert.Null(result.Error);
        Assert.Null(result.Data!.CancellationReason);
        Assert.Null(result.Data.CancellationDescription);
        Assert.Null(result.Data.CancelledAt);
        Assert.Null(result.Data.CancellationRequestedAt);
    }

    [Fact]
    public void Status_and_reason_constants_match_the_wire_values()
    {
        Assert.Equal("cancelling", InvoiceStatuses.Cancelling);
        Assert.Equal("cancelled", InvoiceStatuses.Cancelled);
        Assert.Equal("issue_error", CancellationReasons.IssueError);
        Assert.Equal("service_not_provided", CancellationReasons.ServiceNotProvided);
        Assert.Equal("other", CancellationReasons.Other);
    }
}

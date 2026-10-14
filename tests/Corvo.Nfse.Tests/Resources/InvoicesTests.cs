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
        var handler = TestHandler.RespondJson("""{"data":{"props":{"id":"inv_1","status":"queued"}}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Invoices.CreateAsync(NewInvoice(), idempotencyKey: "idem_1");

        Assert.Null(result.Error);
        var invoice = result.Data!;
        Assert.Equal("inv_1", invoice.Id);
        Assert.Equal(InvoiceStatuses.Queued, invoice.Status);
        var request = handler.Requests[0];
        Assert.Equal("https://api.test.local/v1/invoices", request.Url);
        Assert.Equal(
            """{"companyId":"cmp_1","customer":{"name":"Joao Silva","taxId":"52998224725"},"service":{"code":"0101","description":"Services"},"amount":150.5,"competenceDate":"2026-01-31"}""",
            request.Body);
        Assert.Equal("idem_1", request.Headers["Idempotency-Key"]);
    }

    [Fact]
    public async Task List_sends_invoice_filters_as_query()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"props":{"id":"inv_1"}}],"pagination":{"limit":1,"hasMore":false,"nextCursor":null}}""");
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
    public async Task Events_uses_the_props_envelope()
    {
        var handler = TestHandler.RespondJson(
            """{"data":[{"props":{"id":"evt_1","invoiceId":"inv_1","toStatus":"authorized"}}],"pagination":{"limit":50,"hasMore":false,"nextCursor":null}}""");
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
}

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace Corvo.Nfse.Tests.Resources;

public class ImportsTests
{
    private const string Job =
        """{"id":"imp_1","status":"ready","file_name":"notas.csv","company_id":"cmp_1","rows_expected":2,"counters":{"total":2,"valid":1,"invalid":1,"pending":0,"accepted":0,"authorized":0,"failed":0,"valid_amount":100.5,"pending_amount":0,"authorized_amount":0,"failed_amount":0,"awaiting_amount":0,"awaiting_credits":0},"unknown_columns":["extra"],"error_code":null,"created_at":"2026-11-24T00:00:00.000Z","emit_started_at":null,"emit_finished_at":null}""";

    [Fact]
    public async Task Start_uploads_the_file_and_company_as_multipart()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"id":"imp_1","status":"processing","duplicate_of":null}}""", HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        var result = await client.Imports.StartAsync(new byte[] { 0x61, 0x2C, 0x62 }, "notas.csv", companyId: "cmp_1");

        Assert.Null(result.Error);
        Assert.Equal("imp_1", result.Data!.Id);
        Assert.Equal("processing", result.Data.Status);
        Assert.Null(result.Data.DuplicateOf);
        var request = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.test.local/v1/imports", request.Url);
        Assert.StartsWith("multipart/form-data; boundary=", request.Headers["Content-Type"]);
        Assert.Contains("name=\"companyId\"", request.Body);
        Assert.Contains("cmp_1", request.Body);
        Assert.Contains("name=\"file\"; filename=\"notas.csv\"", request.Body);
        Assert.Contains("text/csv", request.Body);
    }

    [Fact]
    public async Task Start_uses_the_spreadsheet_mime_type_for_xlsx_and_reports_duplicates()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"id":"imp_2","status":"ready","duplicate_of":"imp_1"}}""", HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        var result = await client.Imports.StartAsync(new byte[] { 1 }, "notas.xlsx");

        Assert.Equal("imp_1", result.Data!.DuplicateOf);
        Assert.Contains("spreadsheetml.sheet", handler.Requests[0].Body);
        Assert.DoesNotContain("name=\"companyId\"", handler.Requests[0].Body);
    }

    [Fact]
    public async Task List_and_get_parse_snake_case_jobs()
    {
        var handler = TestHandler.Sequence($$$"""{"data":[{{{Job}}}]}""", $$$"""{"data":{{{Job}}}}""");
        using var client = TestClient.Create(handler);

        var list = await client.Imports.ListAsync(limit: 5);
        Assert.Equal("https://api.test.local/v1/imports?limit=5", handler.Requests[0].Url);
        var job = list.Data![0];
        Assert.Equal("notas.csv", job.FileName);
        Assert.Equal(2, job.RowsExpected);
        Assert.Equal(100.5m, job.Counters.ValidAmount);
        Assert.Equal(new[] { "extra" }, job.UnknownColumns);
        Assert.Null(job.EmitStartedAt);

        var got = await client.Imports.GetAsync("imp_1");
        Assert.Equal("ready", got.Data!.Status);
        Assert.EndsWith("/imports/imp_1", handler.Requests[1].Url);
    }

    [Fact]
    public async Task Emit_posts_the_send_email_flag()
    {
        var handler = TestHandler.RespondJson($$$"""{"data":{{{Job}}}}""", HttpStatusCode.Accepted);
        using var client = TestClient.Create(handler);

        await client.Imports.EmitAsync("imp_1", sendEmail: true);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.EndsWith("/imports/imp_1/emit", handler.Requests[0].Url);
        Assert.Equal("""{"sendEmail":true}""", handler.Requests[0].Body);
    }

    [Fact]
    public async Task Rows_list_iterate_and_update()
    {
        const string row =
            """{"line":2,"status":"invalid","data":{"valor":"abc"},"error_code":"ERROR_IMPORT_ROW","error_message":"bad","invoice_id":null}""";
        var handler = TestHandler.Sequence(
            $$$"""{"data":[{{{row}}}],"pagination":{"limit":1,"hasMore":false,"nextCursor":null}}""",
            $$$"""{"data":[{{{row}}}],"pagination":{"limit":1,"hasMore":true,"nextCursor":"1"}}""",
            $$$"""{"data":[],"pagination":{"limit":1,"hasMore":false,"nextCursor":null}}""",
            $$$"""{"data":{{{row}}}}""");
        using var client = TestClient.Create(handler);

        var page = await client.Imports.ListRowsAsync("imp_1", new ImportRowListParams { Status = ImportRowStatuses.Invalid, Limit = 1 });
        Assert.Equal("https://api.test.local/v1/imports/imp_1/rows?status=invalid&limit=1", handler.Requests[0].Url);
        Assert.Equal(2, page.Data!.Data[0].Line);
        Assert.Equal("ERROR_IMPORT_ROW", page.Data.Data[0].ErrorCode);
        Assert.Equal("abc", page.Data.Data[0].Data.GetProperty("valor").GetString());

        var count = 0;
        await foreach (var item in client.Imports.IterateRowsAsync("imp_1", new ImportRowListParams { Limit = 1 }))
        {
            Assert.Null(item.Error);
            count++;
        }

        Assert.Equal(1, count);
        Assert.EndsWith("/imports/imp_1/rows?cursor=1&limit=1", handler.Requests[2].Url);

        await client.Imports.UpdateRowAsync("imp_1", 2, new JsonObject { ["valor"] = "10.00" });
        var last = handler.Requests[^1];
        Assert.Equal(HttpMethod.Patch, last.Method);
        Assert.EndsWith("/imports/imp_1/rows/2", last.Url);
        Assert.Equal("""{"item":{"valor":"10.00"}}""", last.Body);
    }

    [Fact]
    public async Task Discard_accepts_a_204()
    {
        var handler = TestHandler.Respond(TestHandler.Response(null, HttpStatusCode.NoContent));
        using var client = TestClient.Create(handler);

        var result = await client.Imports.DiscardAsync("imp_1");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
        Assert.EndsWith("/imports/imp_1", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Template_and_errors_come_back_as_csv_files()
    {
        var handler = TestHandler.Respond(TestHandler.Response(null, HttpStatusCode.OK, response =>
        {
            response.Content = new StringContent("a,b\n1,2\n");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment") { FileName = "\"modelo.csv\"" };
        }));
        using var client = TestClient.Create(handler);

        var template = await client.Imports.DownloadTemplateAsync();
        Assert.Equal("a,b\n1,2\n", System.Text.Encoding.UTF8.GetString(template.Data!.Buffer));
        Assert.Equal("modelo.csv", template.Data.FileName);
        Assert.StartsWith("text/csv", template.Data.MimeType);
        Assert.EndsWith("/v1/imports/template", handler.Requests[0].Url);

        var errorsHandler = TestHandler.Respond(TestHandler.Response("a\n", HttpStatusCode.OK));
        using var errorsClient = TestClient.Create(errorsHandler);
        var errors = await errorsClient.Imports.DownloadErrorsAsync("imp_1");
        Assert.Null(errors.Error);
        Assert.EndsWith("/v1/imports/imp_1/errors.csv", errorsHandler.Requests[0].Url);
    }
}

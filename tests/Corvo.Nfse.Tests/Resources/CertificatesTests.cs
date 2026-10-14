using System.Net;

namespace Corvo.Nfse.Tests.Resources;

public class CertificatesTests
{
    [Fact]
    public async Task Upload_sends_multipart_with_file_and_password_parts()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"cert_1","companyId":"cmp_1"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.Certificates.UploadAsync(
            "cmp_1",
            new byte[] { 0x30, 0x82, 0x01, 0x02 },
            password: "s3cret");

        Assert.Null(result.Error);
        Assert.Equal("cert_1", result.Data!.Id);

        var request = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.test.local/v1/companies/cmp_1/certificates", request.Url);

        var contentType = request.Headers["Content-Type"];
        Assert.StartsWith("multipart/form-data; boundary=", contentType);
        Assert.Contains("name=\"file\"", request.Body);
        Assert.Contains("filename=\"certificate.p12\"", request.Body);
        Assert.Contains("application/x-pkcs12", request.Body);
        Assert.Contains("name=\"password\"", request.Body);
        Assert.Contains("s3cret", request.Body);
    }

    [Fact]
    public async Task Upload_honours_a_custom_file_name()
    {
        var handler = TestHandler.RespondJson("""{"data":{"id":"cert_1"}}""");
        using var client = TestClient.Create(handler);

        await client.Certificates.UploadAsync("cmp_1", new byte[] { 1, 2 }, "pw", fileName: "acme.p12");

        Assert.Contains("filename=\"acme.p12\"", handler.Requests[0].Body);
    }

    [Fact]
    public async Task List_and_remove_use_the_company_scoped_paths()
    {
        var handler = TestHandler.Sequence(
            """{"data":[{"id":"cert_1"}],"pagination":{"limit":10,"hasMore":false,"nextCursor":null}}""",
            """{"data":null}""");
        using var client = TestClient.Create(handler);

        var page = await client.Certificates.ListAsync("cmp_1", limit: 10);
        Assert.Null(page.Error);
        Assert.Equal("cert_1", page.Data!.Data[0].Id);
        Assert.Equal("https://api.test.local/v1/companies/cmp_1/certificates?limit=10", handler.Requests[0].Url);

        var removed = await client.Certificates.RemoveAsync("cmp_1", "cert_1");
        Assert.True(removed.IsSuccess);
        Assert.True(removed.Data);
        Assert.Equal(
            "https://api.test.local/v1/companies/cmp_1/certificates/cert_1",
            handler.Requests[1].Url);
        Assert.Equal(HttpMethod.Delete, handler.Requests[1].Method);
    }

    [Fact]
    public async Task Upload_errors_come_back_as_error_results()
    {
        var handler = TestHandler.RespondJson(
            """{"code":"ERROR_CERTIFICATE_INVALID","message":"Password does not match"}""",
            HttpStatusCode.BadRequest);
        using var client = TestClient.Create(handler);

        var result = await client.Certificates.UploadAsync("cmp_1", new byte[] { 1 }, "bad");

        Assert.True(result.IsError);
        var error = result.Error!;
        Assert.Equal("ERROR_CERTIFICATE_INVALID", error.Code);
        Assert.Equal(400, error.Status);
    }
}

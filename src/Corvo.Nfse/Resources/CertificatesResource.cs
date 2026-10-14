using System.Net.Http.Headers;
using System.Text.Json;
using Corvo.Nfse.Transport;

namespace Corvo.Nfse;

/// <summary>Certificates: upload (multipart), list, remove.</summary>
public sealed class CertificatesResource
{
    private readonly HttpTransport _http;

    internal CertificatesResource(HttpTransport http) => _http = http;

    /// <summary>
    /// Uploads an A1 certificate (<c>POST /v1/companies/{id}/certificates</c>) as
    /// <c>multipart/form-data</c> with a <c>file</c> part (application/x-pkcs12) and a
    /// <c>password</c> part.
    /// </summary>
    /// <param name="companyId">The company the certificate belongs to.</param>
    /// <param name="file">The raw .p12/.pfx bytes.</param>
    /// <param name="password">The certificate password.</param>
    /// <param name="fileName">File name for the part; defaults to <c>certificate.p12</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Result<Certificate>> UploadAsync(
        string companyId,
        byte[] file,
        string password,
        string? fileName = null,
        CancellationToken ct = default)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-pkcs12");
        form.Add(fileContent, "file", fileName ?? "certificate.p12");
        form.Add(new StringContent(password), "password");

        return await _http
            .SendMultipartAsync<Certificate>($"/companies/{Uri.EscapeDataString(companyId)}/certificates", form, ct)
            .ConfigureAwait(false);
    }

    /// <summary>Lists a company's certificates (<c>GET /v1/companies/{id}/certificates</c>).</summary>
    public Task<Result<Paginated<Certificate>>> ListAsync(
        string companyId,
        int? limit = null,
        string? cursor = null,
        CancellationToken ct = default)
        => _http.ListAsync<Certificate>(
            $"/companies/{Uri.EscapeDataString(companyId)}/certificates",
            HttpTransport.Query(("limit", limit), ("cursor", cursor)),
            HttpTransport.MapPlain<Certificate>(),
            ct);

    /// <summary>Removes a certificate (<c>DELETE /v1/companies/{cid}/certificates/{certId}</c>).</summary>
    public async Task<Result<bool>> RemoveAsync(string companyId, string certificateId, CancellationToken ct = default)
    {
        var result = await _http
            .RequestAsync<JsonElement>(
                HttpMethod.Delete,
                $"/companies/{Uri.EscapeDataString(companyId)}/certificates/{Uri.EscapeDataString(certificateId)}",
                ct: ct)
            .ConfigureAwait(false);
        return result.Error is null
            ? new Result<bool>(true, null)
            : new Result<bool>(false, result.Error);
    }
}

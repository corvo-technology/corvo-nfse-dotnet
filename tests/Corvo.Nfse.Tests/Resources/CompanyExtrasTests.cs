using System.Net;
using System.Net.Http.Headers;

namespace Corvo.Nfse.Tests.Resources;

public class CompanyExtrasTests
{
    [Fact]
    public async Task AccountantPackage_get_update_send_and_list()
    {
        var handler = TestHandler.Sequence(
            """{"data":{"email":"contador@example.com","lastSentMonth":"2026-10"}}""",
            """{"data":{"email":"novo@example.com","lastSentMonth":null,"testEmailSent":true}}""",
            """{"data":{"email":null,"lastSentMonth":null,"testEmailSent":false}}""",
            """{"data":{"status":"sent"}}""",
            """{"data":[{"id":"s1","month":"2026-10","recipient":"contador@example.com","status":"sent","trigger":"manual","invoiceCount":3,"totalCents":45000,"createdAt":"2026-11-01T00:00:00.000Z"}]}""");
        using var client = TestClient.Create(handler);

        var got = await client.AccountantPackage.GetAsync("cmp_1");
        Assert.Equal("contador@example.com", got.Data!.Email);
        Assert.Equal("2026-10", got.Data.LastSentMonth);
        Assert.EndsWith("/companies/cmp_1/accountant-package", handler.Requests[0].Url);

        var updated = await client.AccountantPackage.UpdateAsync("cmp_1", "novo@example.com");
        Assert.True(updated.Data!.TestEmailSent);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal("""{"accountantEmail":"novo@example.com"}""", handler.Requests[1].Body);

        await client.AccountantPackage.UpdateAsync("cmp_1", null);
        Assert.Equal("""{"accountantEmail":null}""", handler.Requests[2].Body);

        var sent = await client.AccountantPackage.SendAsync("cmp_1", "2026-10");
        Assert.Equal("sent", sent.Data!.Status);
        Assert.Equal(HttpMethod.Post, handler.Requests[3].Method);
        Assert.EndsWith("/companies/cmp_1/accountant-package/send", handler.Requests[3].Url);
        Assert.Equal("""{"month":"2026-10"}""", handler.Requests[3].Body);

        var sends = await client.AccountantPackage.ListSendsAsync("cmp_1");
        Assert.Equal(3, sends.Data![0].InvoiceCount);
        Assert.Equal("manual", sends.Data[0].Trigger);
        Assert.EndsWith("/companies/cmp_1/accountant-package/sends", handler.Requests[4].Url);
    }

    [Fact]
    public async Task InvoicePackage_summary_and_zip_download()
    {
        var handler = TestHandler.Sequence("""{"data":{"companyName":"ACME","companyTaxId":"11222333000181","month":"2026-10","count":3,"limit":500,"total":"450.50"}}""");
        using var client = TestClient.Create(handler);

        var summary = await client.InvoicePackage.SummaryAsync("cmp_1", "2026-10");
        Assert.Equal(3, summary.Data!.Count);
        Assert.Equal("450.50", summary.Data.Total);
        Assert.Equal("https://api.test.local/v1/companies/cmp_1/invoice-package/summary?month=2026-10", handler.Requests[0].Url);

        var zipHandler = TestHandler.Respond(TestHandler.Response(null, HttpStatusCode.OK, response =>
        {
            response.Content = new ByteArrayContent(new byte[] { 0x50, 0x4B, 0x03, 0x04 });
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment") { FileName = "\"pacote-2026-10.zip\"" };
        }));
        using var zipClient = TestClient.Create(zipHandler);

        var zip = await zipClient.InvoicePackage.DownloadAsync("cmp_1", "2026-10", include: "xml");

        Assert.Null(zip.Error);
        Assert.Equal(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, zip.Data!.Buffer);
        Assert.Equal("application/zip", zip.Data.MimeType);
        Assert.Equal("pacote-2026-10.zip", zip.Data.FileName);
        Assert.Equal("https://api.test.local/v1/companies/cmp_1/invoice-package?month=2026-10&include=xml", zipHandler.Requests[0].Url);
    }

    [Fact]
    public async Task InvoicePackage_too_large_is_an_error_result()
    {
        var handler = TestHandler.RespondJson(
            """{"statusCode":422,"code":"PACKAGE_TOO_LARGE","message":"too many","requestId":"r"}""",
            HttpStatusCode.UnprocessableEntity);
        using var client = TestClient.Create(handler);

        var result = await client.InvoicePackage.DownloadAsync("cmp_1", "2026-10");

        Assert.Equal("PACKAGE_TOO_LARGE", result.Error!.Code);
        Assert.Equal(422, result.Error.Status);
    }

    [Fact]
    public async Task DpsSequence_get_and_set()
    {
        var handler = TestHandler.Sequence(
            """{"data":{"lastNumber":12,"series":"1"}}""",
            """{"data":{"lastNumber":500,"series":"1"}}""");
        using var client = TestClient.Create(handler);

        var got = await client.DpsSequence.GetAsync("cmp_1");
        Assert.Equal(12, got.Data!.LastNumber);
        Assert.Equal("1", got.Data.Series);
        Assert.EndsWith("/companies/cmp_1/dps-sequence", handler.Requests[0].Url);

        var set = await client.DpsSequence.SetAsync("cmp_1", 500);
        Assert.Equal(500, set.Data!.LastNumber);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal("""{"lastNumber":500}""", handler.Requests[1].Body);
    }

    [Fact]
    public async Task DpsSequence_set_below_current_keeps_the_error_code()
    {
        var handler = TestHandler.RespondJson(
            """{"statusCode":422,"code":"DPS_NUMBER_BELOW_CURRENT","message":"below","requestId":"r"}""",
            HttpStatusCode.UnprocessableEntity);
        using var client = TestClient.Create(handler);

        var result = await client.DpsSequence.SetAsync("cmp_1", 1);

        Assert.Equal(CompanyErrorCodes.DpsNumberBelowCurrent, result.Error!.Code);
    }

    [Fact]
    public async Task FiscalProfiles_list_upsert_approve()
    {
        const string profile =
            """{"id":"fp_1","tenantId":"t","companyId":"cmp_1","taxRegime":"SIMPLES_NACIONAL","simpleNational":true,"simplesApuracao":"SN","simplesIssRate":null,"specialTaxRegime":null,"ibsCbsRegularOption":false,"ibsCbsRegularFrom":null,"validFrom":"2026-01-01","validUntil":null,"source":"api","reviewStatus":"unreviewed","reviewedAt":null,"reviewedBy":null,"createdAt":"2026-11-01T00:00:00.000Z"}""";
        var handler = TestHandler.Sequence($$$"""{"data":[{{{profile}}}]}""", $$$"""{"data":{{{profile}}}}""", $$$"""{"data":{{{profile}}}}""");
        using var client = TestClient.Create(handler);

        var list = await client.FiscalProfiles.ListAsync("cmp_1");
        Assert.Equal("SIMPLES_NACIONAL", list.Data![0].TaxRegime);
        Assert.Null(list.Data[0].ValidUntil);
        Assert.EndsWith("/companies/cmp_1/fiscal-profile", handler.Requests[0].Url);

        var upserted = await client.FiscalProfiles.UpsertAsync("cmp_1", new FiscalProfileUpsertParams
        {
            TaxRegime = "SIMPLES_NACIONAL",
            SimplesApuracao = "SN",
            ValidFrom = "2026-01-01",
        });
        Assert.Null(upserted.Error);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal("""{"taxRegime":"SIMPLES_NACIONAL","simplesApuracao":"SN","validFrom":"2026-01-01"}""", handler.Requests[1].Body);

        var approved = await client.FiscalProfiles.ApproveAsync("cmp_1", "fp_1");
        Assert.Equal(HttpMethod.Post, handler.Requests[2].Method);
        Assert.EndsWith("/companies/cmp_1/fiscal-profile/fp_1/approve", handler.Requests[2].Url);
        Assert.Equal("unreviewed", approved.Data!.ReviewStatus);
    }

    [Fact]
    public async Task ServiceTaxProfiles_crud_and_approve()
    {
        const string profile =
            """{"id":"stp_1","tenantId":"t","companyId":"cmp_1","name":"Consultoria","serviceCode":"010101","municipalServiceCode":null,"nbsCode":null,"classCode":null,"cst":null,"operationIndicator":null,"operationNature":"SERVICE","issTaxation":"NORMAL","issRate":"2.00","issWithholdingDefault":false,"incidenceMunicipalityCode":null,"regimeApplicability":["MEI"],"withholdingPolicy":"UNKNOWN","withholdingRates":{"irrf":"1.5"},"classificationSource":"manual","catalogVersion":null,"effectiveFrom":"2026-01-01","effectiveUntil":null,"reviewStatus":"unreviewed","createdAt":"2026-11-01T00:00:00.000Z"}""";
        var handler = TestHandler.Sequence(
            $$$"""{"data":[{{{profile}}}]}""",
            $$$"""{"data":{{{profile}}}}""",
            $$$"""{"data":{{{profile}}}}""",
            """{"data":null}""",
            $$$"""{"data":{{{profile}}}}""");
        using var client = TestClient.Create(handler);

        var list = await client.ServiceTaxProfiles.ListAsync("cmp_1", date: "2026-11-24");
        Assert.Equal("1.5", list.Data![0].WithholdingRates.Irrf);
        Assert.Equal("MEI", list.Data[0].RegimeApplicability[0]);
        Assert.EndsWith("/companies/cmp_1/service-tax-profiles?date=2026-11-24", handler.Requests[0].Url);

        await client.ServiceTaxProfiles.CreateAsync("cmp_1", new ServiceTaxProfileCreateParams
        {
            Name = "Consultoria",
            ServiceCode = "010101",
            EffectiveFrom = "2026-01-01",
            IssRate = "2.00",
        });
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("""{"name":"Consultoria","serviceCode":"010101","effectiveFrom":"2026-01-01","issRate":"2.00"}""", handler.Requests[1].Body);

        await client.ServiceTaxProfiles.UpdateAsync("cmp_1", "stp_1", new ServiceTaxProfileUpdateParams
        {
            Name = "Novo nome",
            IssRate = null,
            WithholdingPolicy = "CONFIGURED",
        });
        Assert.Equal(HttpMethod.Patch, handler.Requests[2].Method);
        Assert.EndsWith("/companies/cmp_1/service-tax-profiles/stp_1", handler.Requests[2].Url);
        Assert.Equal("""{"name":"Novo nome","issRate":null,"withholdingPolicy":"CONFIGURED"}""", handler.Requests[2].Body);

        var deleted = await client.ServiceTaxProfiles.DeleteAsync("cmp_1", "stp_1");
        Assert.True(deleted.Data);
        Assert.Equal(HttpMethod.Delete, handler.Requests[3].Method);

        await client.ServiceTaxProfiles.ApproveAsync("cmp_1", "stp_1");
        Assert.EndsWith("/companies/cmp_1/service-tax-profiles/stp_1/approve", handler.Requests[4].Url);
    }

    [Fact]
    public async Task FiscalPortal_status_parses()
    {
        var handler = TestHandler.RespondJson(
            """{"data":{"available":false,"environment":"live","retryAt":"2026-11-24T12:00:00.000Z","since":"2026-11-24T11:00:00.000Z"}}""");
        using var client = TestClient.Create(handler);

        var result = await client.FiscalPortal.StatusAsync();

        Assert.False(result.Data!.Available);
        Assert.Equal("live", result.Data.Environment);
        Assert.Equal("2026-11-24T12:00:00.000Z", result.Data.RetryAt);
        Assert.EndsWith("/v1/fiscal-portal/status", handler.Requests[0].Url);
    }

    [Fact]
    public async Task Company_new_fields_parse_and_update_can_send_the_tax_id()
    {
        var handler = TestHandler.Sequence(
            """{"data":{"id":"cmp_1","legalName":"ACME","taxId":"11222333000181","taxRegime":"Simples Nacional","simpleNational":true,"simplesRate":"6.54","simplesIssRate":"2.01","municipalityAgreement":{"status":"active","agreementType":"national","checkedAt":"2026-11-01T00:00:00.000Z"},"warnings":[{"code":"MEI_MUNICIPALITY_MISMATCH","message":"x"}],"setupGaps":[]}}""",
            """{"data":{"id":"cmp_1"}}""");
        using var client = TestClient.Create(handler);

        var got = await client.Companies.GetAsync("cmp_1");
        Assert.Equal("6.54", got.Data!.SimplesRate);
        Assert.Equal("active", got.Data.MunicipalityAgreement!.Status);
        Assert.Equal("MEI_MUNICIPALITY_MISMATCH", got.Data.Warnings![0].Code);

        await client.Companies.UpdateAsync("cmp_1", new CompanyUpdateParams { LegalName = "ACME LTDA" });
        Assert.Equal("""{"legalName":"ACME LTDA"}""", handler.Requests[1].Body);
    }
}

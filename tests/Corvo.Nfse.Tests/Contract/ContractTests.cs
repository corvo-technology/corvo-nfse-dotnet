namespace Corvo.Nfse.Tests.Contract;

/// <summary>
/// Contract test against a live Corvo instance. Runs only when both CORVO_API_KEY and
/// CORVO_BASE_URL are set; otherwise it no-ops (the test tree must pass offline).
/// </summary>
public class ContractTests
{
    private static bool Enabled =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CORVO_API_KEY")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CORVO_BASE_URL"));

    [Fact]
    public async Task Reads_health_and_a_company_page_against_a_live_instance()
    {
        if (!Enabled)
        {
            return; // skipped: set CORVO_API_KEY + CORVO_BASE_URL to run the contract test
        }

        using var client = new CorvoClient();

        var live = await client.Health.LiveAsync();
        Assert.Null(live.Error);
        Assert.Equal("ok", live.Data!.GetProperty("status").GetString());

        var page = await client.Companies.ListAsync(limit: 1);
        Assert.Null(page.Error);
        var companyPage = page.Data!;
        Assert.Equal(1, companyPage.Pagination.Limit);
        Assert.NotNull(companyPage.Data);
    }
}

using System.Reflection;
using System.Text.Json;

namespace Corvo.Nfse.Tests.Contract;

/// <summary>
/// Loads the vendored contract (<c>spec/openapi.json</c>) and asserts that every operationId is implemented.
/// When the API contract changes, copy the new file to <c>spec/openapi.json</c>: a new operation fails
/// this test until it gets a method here.
/// </summary>
public class OpenApiCoverageTests
{
    /// <summary>operationId to (Client.Resource.Method, "VERB /path").</summary>
    private static readonly Dictionary<string, (string Method, string Route)> Implemented = new()
    {
        ["accountantPackage_get"] = ("AccountantPackage.GetAsync", "GET /companies/{companyId}/accountant-package"),
        ["accountantPackage_listSends"] = ("AccountantPackage.ListSendsAsync", "GET /companies/{companyId}/accountant-package/sends"),
        ["accountantPackage_send"] = ("AccountantPackage.SendAsync", "POST /companies/{companyId}/accountant-package/send"),
        ["accountantPackage_update"] = ("AccountantPackage.UpdateAsync", "PUT /companies/{companyId}/accountant-package"),
        ["billing_getPlan"] = ("Billing.PlanAsync", "GET /billing/plan"),
        ["billing_getUsage"] = ("Billing.UsageAsync", "GET /billing/usage"),
        ["certificates_list"] = ("Certificates.ListAsync", "GET /companies/{companyId}/certificates"),
        ["certificates_revoke"] = ("Certificates.RemoveAsync", "DELETE /companies/{companyId}/certificates/{certId}"),
        ["certificates_upload"] = ("Certificates.UploadAsync", "POST /companies/{companyId}/certificates"),
        ["cnpj_lookup"] = ("Cnpj.LookupAsync", "GET /cnpj/{cnpj}"),
        ["companies_create"] = ("Companies.CreateAsync", "POST /companies"),
        ["companies_delete"] = ("Companies.RemoveAsync", "DELETE /companies/{id}"),
        ["companies_get"] = ("Companies.GetAsync", "GET /companies/{id}"),
        ["companies_list"] = ("Companies.ListAsync", "GET /companies"),
        ["companies_update"] = ("Companies.UpdateAsync", "PATCH /companies/{id}"),
        ["credits_getBalance"] = ("Credits.BalanceAsync", "GET /credits"),
        ["credits_listHistory"] = ("Credits.HistoryAsync", "GET /credits/history"),
        ["credits_listPlans"] = ("Credits.PlansAsync", "GET /credits/plans"),
        ["credits_previewPurchase"] = ("Credits.PreviewPurchaseAsync", "GET /credits/purchase/preview"),
        ["customers_delete"] = ("Customers.DeleteAsync", "DELETE /customers/{taxId}"),
        ["customers_get"] = ("Customers.GetAsync", "GET /customers/{taxId}"),
        ["customers_search"] = ("Customers.SearchAsync", "GET /customers"),
        ["customers_update"] = ("Customers.UpdateAsync", "PATCH /customers/{taxId}"),
        ["dpsSequence_get"] = ("DpsSequence.GetAsync", "GET /companies/{id}/dps-sequence"),
        ["dpsSequence_set"] = ("DpsSequence.SetAsync", "PUT /companies/{id}/dps-sequence"),
        ["fiscalPortal_status"] = ("FiscalPortal.StatusAsync", "GET /fiscal-portal/status"),
        ["fiscalProfiles_approve"] = ("FiscalProfiles.ApproveAsync", "POST /companies/{id}/fiscal-profile/{profileId}/approve"),
        ["fiscalProfiles_list"] = ("FiscalProfiles.ListAsync", "GET /companies/{id}/fiscal-profile"),
        ["fiscalProfiles_upsert"] = ("FiscalProfiles.UpsertAsync", "PUT /companies/{id}/fiscal-profile"),
        ["health_live"] = ("Health.LiveAsync", "GET /health/live"),
        ["imports_discard"] = ("Imports.DiscardAsync", "DELETE /imports/{id}"),
        ["imports_downloadErrors"] = ("Imports.DownloadErrorsAsync", "GET /imports/{id}/errors.csv"),
        ["imports_downloadTemplate"] = ("Imports.DownloadTemplateAsync", "GET /imports/template"),
        ["imports_emit"] = ("Imports.EmitAsync", "POST /imports/{id}/emit"),
        ["imports_get"] = ("Imports.GetAsync", "GET /imports/{id}"),
        ["imports_list"] = ("Imports.ListAsync", "GET /imports"),
        ["imports_listRows"] = ("Imports.ListRowsAsync", "GET /imports/{id}/rows"),
        ["imports_start"] = ("Imports.StartAsync", "POST /imports"),
        ["imports_updateRow"] = ("Imports.UpdateRowAsync", "PATCH /imports/{id}/rows/{line}"),
        ["integrationConnections_activate"] = ("IntegrationConnections.ActivateAsync", "POST /integrations/connections/{id}/activate"),
        ["integrationConnections_create"] = ("IntegrationConnections.CreateAsync", "POST /integrations/connections"),
        ["integrationConnections_disconnect"] = ("IntegrationConnections.DisconnectAsync", "POST /integrations/connections/{id}/disconnect"),
        ["integrationConnections_get"] = ("IntegrationConnections.GetAsync", "GET /integrations/connections/{id}"),
        ["integrationConnections_list"] = ("IntegrationConnections.ListAsync", "GET /integrations/connections"),
        ["integrationConnections_listEvents"] = ("IntegrationConnections.ListEventsAsync", "GET /integrations/connections/{id}/events"),
        ["integrationConnections_listOperations"] = ("IntegrationConnections.ListOperationsAsync", "GET /integrations/connections/{id}/operations"),
        ["integrationConnections_listRules"] = ("IntegrationConnections.ListRulesAsync", "GET /integrations/connections/{id}/rules"),
        ["integrationConnections_pause"] = ("IntegrationConnections.PauseAsync", "POST /integrations/connections/{id}/pause"),
        ["integrationConnections_replaceRules"] = ("IntegrationConnections.ReplaceRulesAsync", "PUT /integrations/connections/{id}/rules"),
        ["integrationConnections_updateCredentials"] = ("IntegrationConnections.UpdateCredentialsAsync", "PUT /integrations/connections/{id}/credentials"),
        ["integrationConnections_validate"] = ("IntegrationConnections.ValidateAsync", "POST /integrations/connections/{id}/validate"),
        ["integrationOperations_completeCustomer"] = ("IntegrationOperations.CompleteCustomerAsync", "PATCH /integrations/operations/{id}/customer"),
        ["integrationOperations_retry"] = ("IntegrationOperations.RetryAsync", "POST /integrations/operations/{id}/retry"),
        ["integrations_listProviders"] = ("Integrations.ListProvidersAsync", "GET /integrations/providers"),
        ["invoicePackage_download"] = ("InvoicePackage.DownloadAsync", "GET /companies/{companyId}/invoice-package"),
        ["invoicePackage_summary"] = ("InvoicePackage.SummaryAsync", "GET /companies/{companyId}/invoice-package/summary"),
        ["invoices_calculate"] = ("Invoices.CalculateAsync", "POST /invoices/calculate"),
        ["invoices_cancel"] = ("Invoices.CancelAsync", "POST /invoices/{id}/cancel"),
        ["invoices_create"] = ("Invoices.CreateAsync", "POST /invoices"),
        ["invoices_createBatch"] = ("Invoices.CreateBatchAsync", "POST /invoices/batches"),
        ["invoices_createDownloadUrl"] = ("Invoices.DownloadUrlAsync", "POST /invoices/{id}/download-url"),
        ["invoices_downloadPdf"] = ("Invoices.PdfAsync", "GET /invoices/{id}/pdf"),
        ["invoices_downloadXml"] = ("Invoices.XmlAsync", "GET /invoices/{id}/xml"),
        ["invoices_get"] = ("Invoices.GetAsync", "GET /invoices/{id}"),
        ["invoices_list"] = ("Invoices.ListAsync", "GET /invoices"),
        ["invoices_listEvents"] = ("Invoices.EventsAsync", "GET /invoices/{id}/events"),
        ["invoices_overview"] = ("Invoices.OverviewAsync", "GET /invoices/overview"),
        ["invoices_review"] = ("Invoices.ReviewAsync", "POST /invoices/review"),
        ["invoices_sendEmail"] = ("Invoices.SendEmailAsync", "POST /invoices/{id}/email"),
        ["invoices_substitute"] = ("Invoices.SubstituteAsync", "POST /invoices/{id}/substitute"),
        ["schedules_cancel"] = ("Schedules.CancelAsync", "POST /schedules/{id}/cancel"),
        ["schedules_get"] = ("Schedules.GetAsync", "GET /schedules/{id}"),
        ["schedules_list"] = ("Schedules.ListAsync", "GET /schedules"),
        ["schedules_listRuns"] = ("Schedules.ListRunsAsync", "GET /schedules/{id}/runs"),
        ["schedules_pause"] = ("Schedules.PauseAsync", "POST /schedules/{id}/pause"),
        ["schedules_resume"] = ("Schedules.ResumeAsync", "POST /schedules/{id}/resume"),
        ["schedules_update"] = ("Schedules.UpdateAsync", "PATCH /schedules/{id}"),
        ["serviceFavorites_create"] = ("ServiceFavorites.CreateAsync", "POST /companies/{companyId}/service-favorites"),
        ["serviceFavorites_delete"] = ("ServiceFavorites.DeleteAsync", "DELETE /companies/{companyId}/service-favorites/{favoriteId}"),
        ["serviceFavorites_list"] = ("ServiceFavorites.ListAsync", "GET /companies/{companyId}/service-favorites"),
        ["serviceTaxProfiles_approve"] = ("ServiceTaxProfiles.ApproveAsync", "POST /companies/{id}/service-tax-profiles/{serviceId}/approve"),
        ["serviceTaxProfiles_create"] = ("ServiceTaxProfiles.CreateAsync", "POST /companies/{id}/service-tax-profiles"),
        ["serviceTaxProfiles_delete"] = ("ServiceTaxProfiles.DeleteAsync", "DELETE /companies/{id}/service-tax-profiles/{serviceId}"),
        ["serviceTaxProfiles_list"] = ("ServiceTaxProfiles.ListAsync", "GET /companies/{id}/service-tax-profiles"),
        ["serviceTaxProfiles_update"] = ("ServiceTaxProfiles.UpdateAsync", "PATCH /companies/{id}/service-tax-profiles/{serviceId}"),
        ["webhookDeliveries_list"] = ("Webhooks.Deliveries.ListAsync", "GET /webhooks/deliveries"),
        ["webhookDeliveries_retry"] = ("Webhooks.Deliveries.RetryAsync", "POST /webhooks/deliveries/{id}/retry"),
        ["webhookDeliveries_stats"] = ("Webhooks.Deliveries.StatsAsync", "GET /webhooks/deliveries/stats"),
        ["webhooks_create"] = ("Webhooks.CreateAsync", "POST /webhooks"),
        ["webhooks_delete"] = ("Webhooks.RemoveAsync", "DELETE /webhooks/{id}"),
        ["webhooks_list"] = ("Webhooks.ListAsync", "GET /webhooks"),
        ["webhooks_test"] = ("Webhooks.TestAsync", "POST /webhooks/{id}/test")
    };

    private static Dictionary<string, string> LoadSpec(out string version)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "spec", "openapi.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        version = doc.RootElement.GetProperty("info").GetProperty("version").GetString()!;
        var routes = new Dictionary<string, string>();
        foreach (var pathItem in doc.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in pathItem.Value.EnumerateObject())
            {
                if (operation.Name == "parameters")
                {
                    continue;
                }

                routes[operation.Value.GetProperty("operationId").GetString()!] =
                    $"{operation.Name.ToUpperInvariant()} {pathItem.Name}";
            }
        }

        return routes;
    }

    [Fact]
    public void The_vendored_contract_is_0_3_0()
    {
        LoadSpec(out var version);
        Assert.Equal("0.3.0", version);
        Assert.Equal(CorvoClient.Version, version);
    }

    [Fact]
    public void Every_operation_of_the_contract_has_a_method()
    {
        var spec = LoadSpec(out _);

        Assert.Empty(spec.Keys.Except(Implemented.Keys).OrderBy(x => x));
        Assert.Empty(Implemented.Keys.Except(spec.Keys).OrderBy(x => x));
        Assert.Equal(92, spec.Count);
    }

    [Fact]
    public void Each_mapped_route_matches_the_contract()
    {
        var spec = LoadSpec(out _);

        foreach (var (operationId, entry) in Implemented)
        {
            Assert.True(spec[operationId] == entry.Route, $"{operationId}: contract says {spec[operationId]}");
        }
    }

    [Fact]
    public void Each_mapped_method_exists_on_the_client()
    {
        foreach (var (operationId, entry) in Implemented)
        {
            var parts = entry.Method.Split('.');
            var type = typeof(CorvoClient);
            foreach (var part in parts[..^1])
            {
                var property = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance);
                Assert.True(property is not null, $"{operationId}: no property {part} on {type.Name}");
                type = property!.PropertyType;
            }

            var method = type.GetMethod(parts[^1], BindingFlags.Public | BindingFlags.Instance);
            Assert.True(method is not null, $"{operationId}: no method {entry.Method}");
        }
    }
}

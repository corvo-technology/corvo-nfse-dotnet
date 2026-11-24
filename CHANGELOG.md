# Changelog

## 0.3.0

SDK for the API contract 0.3.0 (92 operations, vendored at `spec/openapi.json`; also at <https://corvo.tech/openapi.json>).

### Breaking

- `Invoices.CreateAsync` now returns `Result<InvoiceCreateResult>`: read `.Invoice` for an immediate invoice or `.Schedule` when `Options.Type` is `SCHEDULE` or `RECURRING` (the API answers a schedule, not an invoice).
- Plan slugs are `free`, `self-hosted`, `solo`, `equipe`, `empresa`, `escala` (`developer`, `growth`, `scale`, `volume` are gone). Treat the slug as an open string.
- `CreditBalance`: removed `TenantId`, `OverageUnits`, `OverageAmountCents`; added `PlanRemaining`, `AvulsoBalance`. `Balance` and `HardLimit` are `int`.
- `BillingUsage`: removed `OverageUnits`, `OverageAmountCents`; added `AvulsoBalance`. `BillingSubscription` gained `StripePortalAvailable`, `StripeSubscription`, `PixSubscription`. `GET /billing/plan` answers `404 NO_PLAN_ASSIGNED` without a plan.
- `CreditTransaction.Amount` is `int`; new `Metadata`.
- `InvoiceCustomerInput.TaxId` is optional (use `Foreign` for a customer outside Brazil).
- `WebhookTestResult` is now `{ Sent, Error, StatusCode }`; `WebhookDelivery.ResponseBody` is a `string`.
- `Invoices.SendEmailAsync(id, idempotencyKey, email, ct)`: the new `email` parameter comes before `ct`.
- A test key can no longer reach live webhooks (`Webhook.Environment`), and webhook URLs that resolve to private networks are refused (API behavior, documented).

### Added

- Customers (`Customers.SearchAsync/IterateAsync/GetAsync/UpdateAsync/DeleteAsync`), `Cnpj.LookupAsync`, `ServiceFavorites`.
- `AccountantPackage`, `InvoicePackage` (summary and ZIP download), `DpsSequence`, `FiscalProfiles`, `ServiceTaxProfiles`, `FiscalPortal.StatusAsync`.
- `Schedules` (list, get, update, pause, resume, cancel, runs).
- `Imports` (multipart upload, list, get, emit, rows, update row, discard, CSV template and errors).
- `Integrations`, `IntegrationConnections`, `IntegrationOperations`.
- `Invoices.SubstituteAsync`, `OverviewAsync`, `ReviewAsync`; `InvoiceListParams.CustomerTaxId`; optional email destination on `SendEmailAsync`.
- `Credits.PreviewPurchaseAsync` (`GET /credits/purchase/preview?units=N`).
- Invoice fields: `SubstitutesInvoiceId`, `SubstitutionDescription`, `PortalFlag`, `PurchaseOrder`, `Notes`, recurrence options, foreign customers; company `MunicipalityAgreement`, `SimplesRate`, `SimplesIssRate`, `Warnings`.
- Webhook event `nfse.invoice.taker_rejected` (12 events).
- `OpenApiCoverageTests`: fails when an operation of `spec/openapi.json` has no method.
- CI checks formatting (`dotnet format --verify-no-changes`).

### Notes

- `Plans` (public price catalog) is not part of the 0.3.0 contract; it is kept unchanged.

## 0.2.0

Companies, certificates, invoices (batch, cancel), webhooks, credits, billing, plans and health.

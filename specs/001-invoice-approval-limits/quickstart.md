# Quickstart validation guide

## Prerequisites

- .NET 8 SDK installed
- Node.js and npm available for Playwright E2E tests
- Repository already checked out at the current workspace root
- Local app runtime roles configured as required by the assignment

## Local validation commands

### Run backend tests

```bash
dotnet test backend/InvoiceApp.sln
```

### Run the API locally

```bash
dotnet run --project backend/src/InvoiceApp.Api
```

Then verify the web UI at `http://localhost:5000` or the configured ASP.NET launch settings.

### Run Playwright E2E tests

```bash
cd e2e
npm install
npx playwright install chromium
npm test
```

### Run mutation testing

```bash
dotnet tool install -g dotnet-stryker --version 4.13.0
```

Then run the repository’s mutation test command for the project under test using the installed tool version. The requirement is to use the already-installed version and record the results rather than reinstalling it.

## Validation scenarios

1. Manager approval at `50,000` succeeds.
2. Manager approval at `50,001` is rejected with a clear reason and no invoice mutation.
3. Director approval at `500,000` succeeds.
4. Director approval at `500,001` without a valid second Director approver is rejected.
5. Same-user duplicate approver is rejected.
6. Non-Director second approver is rejected.
7. Invalid approval order is rejected.
8. Already-finalized invoice attempts are rejected.
9. Blank rejection reason is rejected.
10. Rejected approval attempts do not partially update `DecisionNote`, `Status`, or decision timestamps.

## Expected outcome

The feature is considered ready for implementation review when all backend tests pass, the required E2E rejection scenario passes, and mutation testing identifies no gaps that require weak or misleading tests.

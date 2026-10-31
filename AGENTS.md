# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected). Approval-limit rules and invalid-approval checks must live here, not in controllers or startup code.
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`. Controllers only parse requests, call the domain service, and map domain errors to HTTP responses.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`. Tests must cover boundary values and invalid approval attempts; a passing baseline is not proof the feature is complete.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`). One Playwright test must cover a rejected over-limit approval through the UI.
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only; approvals are subject to the assignment-specific role rules below.
- Business rules go in the Domain project, never in controllers.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.
- A cold-arriving agent must rely on repository files and assignment rules, not on prior chat history.
- Use the repo's available skills and hooks for logging, structured output, and review evidence; create or update missing project skills/hooks when they are required by the assignment.
- Use multiple agents or sub-agents for distinct review and validation passes when required by the assignment.
- Write or update the formal feature specification with a spec-driven tool before broad implementation; commit the generated specification files with the implementation.
- For review and analysis tasks, follow .github/templates/review-analysis-output.md. Adapt the detailed analysis to the task, but preserve the common reporting structure wherever applicable. Do not fabricate evidence or findings.

### Assignment-specific approval rules

- New approval roles are required for this assignment: `Manager` and `Director`.
- These roles are not assumed to already exist in runtime configuration; they must be added to the demo user configuration before testing or sign-in.
- Manager approvals are valid only up to and including 50,000.
- Director approvals are valid only up to and including 500,000.
- Invoices above 500,000 require two distinct approvers.
- The second approver must be a Director.
- The same user cannot count as both approvers on the same invoice.
- Invalid or unauthorized approval attempts must be rejected with a clear reason.
- A rejected attempt must leave the invoice unchanged and must not partially update state.
- Boundary and negative cases are mandatory: 50,000, just above 50,000, 500,000, just above 500,000, duplicate approvers, invalid approval order, wrong role, and already-finalized invoice attempts.
- Review findings and implementation decisions must verify these rules against the assignment; do not accept a plausible-looking implementation that ignores the negative cases.

### Role model note

- Current runtime roles in the repo are `Approver` and `Admin`.
- The assignment requires `Manager` and `Director` approval roles to be added and used consistently in configuration, authorization checks, and tests.
- Do not assume these roles are already present in the runtime; they must be created in the configured demo users before a cold agent can sign in as them.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`
- Run mutation testing with the required version: `dotnet tool install -g dotnet-stryker --version 4.13.0`

## Demo accounts already present (not real credentials)

- `demo.approver` / `ChangeMe123!` (role Approver)
- `demo.admin` / `ChangeMe123!` (role Admin)

## Required demo accounts for this assignment

These users are required for the approval-limit assignment and must be added to the configured demo users before testing or sign-in:

- `demo.manager` / `ChangeMe123!` (role Manager)
- `demo.director` / `ChangeMe123!` (role Director)

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.
- Implement approval-limit logic in controllers, startup code, or UI code.
- Approve work without tests covering boundary values and negative conditions.
- Claim the assignment is complete without verifying it against the approval rules and the required validation commands.
- Ignore the distinct-approver and same-user rules; these are the highest-risk logic failures.
- Accept review findings without a reason, a fix, or a clear deferred/dismissed verdict.
- Do not mark review findings as fixed, dismissed, or deferred without a clear verdict and rationale.

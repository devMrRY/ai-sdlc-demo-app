# Project Context

This file is a snapshot of repository facts verified on 2026-11-01.

## Repository state

- Repository: `devMrRY/ai-sdlc-demo-app`
- Branch: `feature/us3-invoice-rejection-workflow`
- HEAD: `ebc31fa70da07e59cdc4dbdef44a6b2d28ec09b2` (`fix: added docs for us2 and updated context`)
- US1 and US2 implementation milestones have completed. US2 commit `e1bb728` is in history; US3 changes remain uncommitted. Do not claim US3 is committed, pushed, or merged.
- The working tree is dirty with US3 tests, role authorization/UI changes, and related documentation/specification updates. It also contains interaction/session logs and other pre-existing changes; preserve unrelated changes.
- US3 tasks T043–T052 are complete. T053 remains unchecked because the US3 commit has not succeeded.

## Application and repository structure

This is an ASP.NET Core 8 Web API with a static web UI and an in-memory invoice repository.

- `backend/src/InvoiceApp.Domain/`
  - `Invoice.cs`: invoice state, decision fields, and approval history.
  - `ApprovalRecord.cs`: persisted approver identity, role, and timestamp.
  - `InvoiceStatus.cs`: `Draft`, `PendingApproval`, `Approved`, and `Rejected`.
  - `ApprovalService.cs`: invoice submission, approval, rejection, sequencing, and business validation.
  - `IInvoiceRepository.cs`: domain repository interface, including atomic invoice operations.
- `backend/src/InvoiceApp.Api/`
  - `Program.cs`: dependency registration, middleware, and controller mapping.
  - `Controllers/`: invoice, approval, and authentication endpoints; `ApiControllerBase` maps domain errors to HTTP responses.
  - `Auth/`: demo-user configuration, `SessionStore`, bearer-session middleware, and `RequireRoleAttribute`.
  - `InMemoryInvoiceRepository.cs`: in-memory repository implementation with atomic per-invoice operations.
  - `wwwroot/`: static UI (`index.html`, `app.js`, and `styles.css`).
- `backend/tests/InvoiceApp.Tests/`
  - `ApprovalServiceTests.cs`: domain behavior tests.
  - `ApprovalsControllerAuthorizationTests.cs`: API authorization tests using ASP.NET Core TestServer.
  - `FakeInvoiceRepository.cs`: test repository.
- `e2e/pages/` and `e2e/tests/`: Playwright page object and browser tests.
- `specs/001-invoice-approval-limits/`: feature specification, plan, tasks, and supporting design/API artifacts.
- `.github/`: agent instructions, skills, hooks, PR review artifacts, and interaction logs.
- `docs/use-cases/`: feature use-case briefs.

## Verified implementation status

### US1 — implemented

The configured demo users in `backend/src/InvoiceApp.Api/appsettings.json` have the roles `Approver`, `Admin`, `Manager`, and `Director`. All four users may sign in; the seeded Approver/Admin accounts are retained for negative authorization testing. Only Manager and Director may approve or reject invoices.

`ApprovalService.Approve(invoiceId, approver, role)` validates the role and amount before mutation:

- Manager may approve an invoice up to and including 50,000.
- Director may approve an invoice up to and including 500,000, including amounts at or below 50,000.
- An independent single approval above 500,000 is not allowed; the US2 provisional-first-approval flow is described separately below.
- Invalid approval attempts throw before changing invoice state.

Both approval and rejection endpoints are restricted to Manager/Director and use the identity and role from the authenticated session. Approver, Admin, and unsupported roles receive HTTP 403 with `A Manager or Director role is required to perform this operation.` Unauthorized attempts leave invoice state unchanged. The UI omits decision controls for other roles. `ApiControllerBase.Handle(...)` maps missing invoices to 404, invalid state/business-rule errors to 409, and argument errors to 400.

The US1 tests include amount/role boundaries, state preservation for invalid attempts, API authorization through TestServer, and the required Manager over-limit Playwright scenario. `e2e/tests/manager-over-limit-approval.spec.ts` signs in as a Manager, attempts to approve a 50,001 invoice, checks the over-limit error, and verifies the invoice remains pending with no final decision metadata. This tests an invalid approval attempt, not an explicit invoice rejection.

The review report `.github/PRs/us1-review-report.md` records F-01 as dismissed and F-02 as fixed; its overall merge decision remains a human decision.

### US2 — implementation completed

For invoices above 500,000, either a Manager or Director may provide the first provisional approval. The service records the authenticated user's identity, role, and timestamp in `Invoice.ApprovalHistory`, while leaving the invoice `PendingApproval` and keeping `DecidedBy` and `DecidedAt` unset. A different authenticated Director must provide the second approval; only then is the invoice marked `Approved` and final decision metadata set.

The approval endpoint continues to derive identity and role from the session. The domain service owns approval sequencing and validation. Repository operations serialize each invoice's state transition. The client does not supply an approval stage or trusted identity. Invalid second approvals preserve invoice state and history; rejection remains a separate operation.

Backend tests cover Manager→Director and Director→Director success, invalid second-role and duplicate-user attempts, recovery with a different Director, finalized invoices, history integrity, and concurrent first approvals. The Playwright test `e2e/tests/high-value-two-approver-approval.spec.ts` verifies the Manager→Director flow in the UI and checks provisional and final persisted state.

`specs/001-invoice-approval-limits/tasks.md` marks T028–T042 complete. US2 commit `e1bb728` is in history.

### US3 — implementation and validation completed; commit pending

Rejections require a non-empty reason and record the authenticated actor, timestamp, and rejection reason in the API response's `decisionNote` property (`Invoice.DecisionNote` in the domain model). Domain validation rejects attempts to decide invoices that are not pending, including approval of a Rejected invoice and rejection of an Approved invoice. Invalid requests preserve invoice state and approval history. The approval service's US1/US2 business rules remain intact.

API authorization tests verify Manager/Director access and HTTP 403 with the exact shared message for Approver, Admin, and an unsupported role, asserting invoice-state preservation. The UI allows those seeded users to sign in but omits their approve/reject controls. Playwright covers both seeded roles.

The US3 review-analysis report found no confirmed code defects. It noted stale handoff documentation (F-01) and absence of available test evidence at review time (F-02); tests have since been executed successfully as recorded below. T043–T052 are checked; T053 is unchecked pending human approval and a successful US3 commit.

## Verified validation

Validation was run on the current code changes on 2026-11-01:

- API authorization tests: `dotnet test backend\InvoiceApp.sln -c Release --filter "FullyQualifiedName~ApprovalsControllerAuthorizationTests"` — passed, 17 passed, 0 failed, 0 skipped.
- Full backend suite: `dotnet test backend\InvoiceApp.sln -c Release` — passed, 44 passed, 0 failed, 0 skipped.
- Playwright suite: `npm test` from `e2e/` — passed, 4 tests.
- `git diff --check` — passed.
- Mutation testing was not run or verified. T054–T058 remain unchecked.

Commands:

```powershell
dotnet run --project backend/src/InvoiceApp.Api
dotnet test backend/InvoiceApp.sln
Set-Location e2e; npm test
```

The E2E package also documents dependency/browser setup in `e2e/package.json` and `e2e/playwright.config.ts`.

## Logging and agent support

- `.github/hooks/logger.json` registers lifecycle, prompt, tool, and subagent hook events.
- `.github/hooks/log-interaction.mjs` writes interaction records to `.github/logs/ai-interactions.jsonl` and per-session files under `.github/logs/sessions/`.
- `.github/skills/ai-interaction-logger/SKILL.md` documents the logging requirements.
- `.github/skills/refresh-project-context/SKILL.md` defines this explicit refresh workflow.
- Project instructions are in `AGENTS.md`; assignment requirements are in `ASSIGNMENT.md`.

## Known limitations and pending work

- US3 is not committed, pushed, or merged. T053 remains pending because its commit has not succeeded.
- T054–T058 final validation/review milestones, including mutation testing, remain unchecked.
- The TestServer authorization fixture composes its own host and middleware pipeline rather than exercising production startup wiring. The review accepted this narrower startup-wiring coverage; the tests do exercise the real session store, middleware, role filter, controller, and error mapping.
- Mutation testing and remaining assignment deliverables are not verified complete.
- Preserve unrelated uncommitted source/documentation changes and the current interaction/session log paths; do not overwrite or discard them.
